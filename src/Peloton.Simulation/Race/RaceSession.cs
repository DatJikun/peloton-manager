using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public sealed record RaceRiderMotion(
    WorldEntityId RiderId,
    double DistanceM,
    double SpeedMps,
    double ShelterMultiplier,
    double Gradient);

public sealed record RaceMotionSnapshot(
    int RaceSecond,
    double RouteLengthM,
    IReadOnlyList<RaceRiderMotion> Riders);

public sealed class RaceSession
{
    public const int PhysicsContractVersion = 2;

    private const double StepSeconds = 1.0;
    private const int AttackDurationSeconds = 18;
    private const int ForcePaceDurationSeconds = 90;
    private const double ForcePaceSpeedIncreaseMps = 1.2;
    private const double AttackSpeedIncreaseMps = 3.0;
    private const double ConserveSpeedDecreaseMps = 0.8;
    private const double ClassifiedFlatSitInMaxGradient = 0.005;
    private const double ClassifiedFlatSitInMaxWindMps = 1.5;
    private const double ClassifiedFlatSitInShelterMultiplier = 0.62;

    private readonly RaceScenario scenario;
    private readonly RiderRuntime[] riders;
    private readonly IWorldSpySink spySink;
    private readonly List<RaceCommand> resolvedCommands = new();
    private readonly HashSet<int> evaluatedTacticalPlans = new();
    private readonly bool routeHasCobble;
    private readonly Dictionary<WorldEntityId, int> pacingReferenceSurgeUntilSecond = new();
    private int simulationSecond;
    private int maximumGroupCount = 1;
    private int decisionCount;
    private int? lastDecisionSecond;
    private PendingDecisionContext? pendingDecisionContext;

    internal RaceSession(RaceScenario scenario, long seed, IWorldSpySink spySink)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(spySink);
        this.scenario = scenario;
        this.spySink = spySink;
        routeHasCobble = scenario.Definition.Segments
            .Any(segment => segment.Surface == RouteSurface.Cobble);
        Seed = seed;
        Dictionary<WorldEntityId, RaceStartingPosition> positions = scenario.StartingPositions
            .ToDictionary(position => position.RiderId);
        riders = scenario.Riders
            .OrderBy(profile => profile.RiderId.Value)
            .Select(profile => new RiderRuntime(
                profile,
                positions[profile.RiderId].DistanceM,
                scenario.InitialSpeedMps))
            .ToArray();
        ResolveGroups();
    }

    public long Seed { get; }

    public RaceScenario Scenario => scenario;

    public bool IsCompleted => Result is not null;

    public RaceResult? Result { get; private set; }

    public int SimulationSecond => simulationSecond;

    public RaceDecisionRequest? PendingDecision => pendingDecisionContext?.Request;

    public RaceWatchCourse Course => new(
        scenario.Definition.TotalLengthM,
        scenario.Definition.Segments
            .Select(segment => new RaceWatchCourseSegment(
                segment.Id,
                segment.LengthM,
                segment.Gradient,
                segment.RoadWidthM,
                segment.WindSpeedMps,
                segment.WindYawDegrees))
            .ToArray());

    public RaceMotionSnapshot GetMotionSnapshot()
    {
        RaceRiderMotion[] motion = riders
            .OrderBy(rider => rider.Profile.RiderId.Value)
            .Select(rider =>
            {
                RaceRouteSegment segment = scenario.Definition.SegmentAt(rider.DistanceM);
                return new RaceRiderMotion(
                    rider.Profile.RiderId,
                    rider.DistanceM,
                    rider.SpeedMps,
                    rider.ShelterMultiplier,
                    segment.Gradient);
            })
            .ToArray();
        return new RaceMotionSnapshot(simulationSecond, scenario.Definition.TotalLengthM, motion);
    }

    public RaceStepResult Step()
    {
        if (Result is not null)
        {
            return new RaceStepResult(RaceStepStatus.Completed, Result);
        }

        if (PendingDecision is not null || DetectDecision())
        {
            return new RaceStepResult(RaceStepStatus.DecisionRequired, null);
        }

        if (simulationSecond >= scenario.MaximumDurationSeconds)
        {
            throw new InvalidOperationException(
                $"Race '{scenario.Id}' exceeded its maximum duration without every rider finishing.");
        }

        ResolveGroups();
        ApplyCommands();
        ApplyBunchSprintIntents();
        Dictionary<int, double> groupTargetSpeedMps = DetermineGroupTargetSpeeds();
        Dictionary<WorldEntityId, StepSolve> solves = new();
        foreach (RiderRuntime rider in riders.OrderBy(rider => rider.Profile.RiderId.Value))
        {
            if (rider.FinishTimeSeconds is not null)
            {
                continue;
            }

            RaceRouteSegment segment = scenario.Definition.SegmentAt(rider.DistanceM);
            double remainingM = scenario.Definition.TotalLengthM - rider.DistanceM;
            AtmosphereSample atmosphere = AtmosphereForPhysics(segment);
            if (rider.Intent == RaceCommandKind.LaunchSprint &&
                remainingM <= BunchSprintResolver.KickDistanceM)
            {
                solves.Add(rider.Profile.RiderId, SolveLaunchSprint(rider, segment));
                continue;
            }

            double baseSpeedMps = BasePaceMps(atmosphere.Gradient);
            double desiredSpeedMps = groupTargetSpeedMps.TryGetValue(rider.GroupId, out double groupTarget)
                ? groupTarget
                : baseSpeedMps;
            if (rider.Intent == RaceCommandKind.Conserve)
            {
                desiredSpeedMps = Math.Max(2.0, baseSpeedMps - ConserveSpeedDecreaseMps);
            }

            double desiredAccelerationMps2 = Math.Clamp(
                desiredSpeedMps - rider.SpeedMps,
                -RaceTuning.MaximumDesiredAccelerationMps2,
                RaceTuning.MaximumDesiredAccelerationMps2);
            double yawRadians = atmosphere.WindYawDegrees * (Math.PI / 180.0);
            double headwindMps = Math.Cos(yawRadians) * atmosphere.WindSpeedMps;
            double crosswindMps = Math.Sin(yawRadians) * atmosphere.WindSpeedMps;
            double relativeAirSpeedMps = Math.Sqrt(
                Math.Pow(Math.Max(0.0, desiredSpeedMps + headwindMps), 2.0) +
                Math.Pow(crosswindMps, 2.0));
            double shelterMultiplier = ShelterForPhysics(
                rider.ShelterMultiplier,
                remainingM,
                segment.Surface,
                rider.Profile.Handling);
            double cobbleSurgeMultiplier = CobbleSurgeMultiplier(
                segment.Surface,
                rider.Profile.Handling,
                scenario.ClassifiedStageType);
            RequiredPowerBreakdown demand = RequiredPowerSolver.Calculate(new RequiredPowerInput(
                desiredSpeedMps,
                desiredAccelerationMps2,
                atmosphere.Gradient,
                scenario.Definition.AirDensityKgPerM3,
                relativeAirSpeedMps,
                rider.Profile.CdAM2,
                shelterMultiplier,
                EffectiveCrr(rider.Profile.BaseCrr, rider.Profile.Handling, segment.Surface),
                rider.Profile.TotalMassKg));
            if (cobbleSurgeMultiplier > 1.0)
            {
                demand = demand with
                {
                    AerodynamicPowerW = demand.AerodynamicPowerW * cobbleSurgeMultiplier,
                    RollingPowerW = demand.RollingPowerW * cobbleSurgeMultiplier,
                    GravityPowerW = demand.GravityPowerW * cobbleSurgeMultiplier,
                    AccelerationPowerW = demand.AccelerationPowerW * cobbleSurgeMultiplier,
                };
            }
            CapabilityResult capability = CapabilitySolver.Evaluate(
                rider.Profile,
                rider.Physiology,
                demand.TotalPowerW,
                StepSeconds);
            double realizedSpeedMps = RealizedSpeed(
                desiredSpeedMps,
                demand.TotalPowerW,
                capability.RealizablePowerW,
                atmosphere.Gradient,
                segment.Surface);
            solves.Add(rider.Profile.RiderId, new StepSolve(
                realizedSpeedMps,
                capability.RealizablePowerW,
                capability.EffectiveCriticalPowerW,
                capability.NextState));
        }

        foreach (RiderRuntime rider in riders.OrderBy(rider => rider.Profile.RiderId.Value))
        {
            if (!solves.TryGetValue(rider.Profile.RiderId, out StepSolve? solve))
            {
                continue;
            }

            double previousDistanceM = rider.DistanceM;
            rider.SpeedMps = solve.RealizedSpeedMps;
            rider.DistanceM += solve.RealizedSpeedMps * StepSeconds;
            rider.EnergySpentJ += solve.RealizablePowerW * StepSeconds;
            if (solve.RealizablePowerW > solve.EffectiveCriticalPowerW)
            {
                rider.TimeAboveCriticalPowerSeconds++;
            }

            rider.Physiology = solve.NextPhysiology;
            if (rider.DistanceM >= scenario.Definition.TotalLengthM)
            {
                double remainingAtStartM = scenario.Definition.TotalLengthM - previousDistanceM;
                double withinStepSeconds = solve.RealizedSpeedMps <= 0.0
                    ? StepSeconds
                    : Math.Clamp(remainingAtStartM / solve.RealizedSpeedMps, 0.0, StepSeconds);
                rider.FinishTimeSeconds = simulationSecond + withinStepSeconds;
            }
        }

        simulationSecond++;
        ApplyPositionDrift();
        ExpireIntents();
        if (riders.All(rider => rider.FinishTimeSeconds is not null))
        {
            Result = BuildResult();
            return new RaceStepResult(RaceStepStatus.Completed, Result);
        }

        if (DetectDecision())
        {
            return new RaceStepResult(RaceStepStatus.DecisionRequired, null);
        }

        return new RaceStepResult(RaceStepStatus.Advanced, null);
    }

    public void ResolveDecision(RaceDecisionResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        PendingDecisionContext context = pendingDecisionContext
            ?? throw new InvalidOperationException("The race has no pending decision.");
        context.Request.Resolve(resolution);
        ApplyStrategicChoice(context.Plan, resolution.SelectedOption);
        decisionCount++;
        lastDecisionSecond = simulationSecond;
        EmitDecisionTrace(context, resolution.SelectedOption);
        pendingDecisionContext = null;
    }

    private StepSolve SolveLaunchSprint(RiderRuntime rider, RaceRouteSegment segment)
    {
        AtmosphereSample atmosphere = AtmosphereForPhysics(segment);
        CapabilityResult capability = CapabilitySolver.Evaluate(
            rider.Profile,
            rider.Physiology,
            rider.Profile.PeakPowerW,
            StepSeconds);
        double targetSpeedMps = BunchSprintResolver.SpeedForPowerW(
            capability.RealizablePowerW,
            atmosphere.Gradient,
            scenario.Definition.AirDensityKgPerM3,
            atmosphere.WindSpeedMps,
            atmosphere.WindYawDegrees,
            rider.Profile.CdAM2,
            shelterMultiplier: 1.0,
            EffectiveCrr(rider.Profile.BaseCrr, rider.Profile.Handling, segment.Surface),
            rider.Profile.TotalMassKg);
        double accelerationMps2 = Math.Clamp(
            targetSpeedMps - rider.SpeedMps,
            -RaceTuning.MaximumDesiredAccelerationMps2,
            RaceTuning.MaximumDesiredAccelerationMps2);
        double realizedSpeedMps = Math.Max(2.0, rider.SpeedMps + (accelerationMps2 * StepSeconds));
        if (accelerationMps2 > 0.0)
        {
            realizedSpeedMps = Math.Min(realizedSpeedMps, targetSpeedMps);
        }

        return new StepSolve(
            realizedSpeedMps,
            capability.RealizablePowerW,
            capability.EffectiveCriticalPowerW,
            capability.NextState);
    }

    private void ApplyCommands()
    {
        foreach (RaceCommand command in scenario.Commands.Concat(resolvedCommands)
                     .Where(command => command.SimulationSecond == simulationSecond)
                     .OrderBy(command => command.OrganizationId.Value)
                     .ThenBy(command => command.RiderId.Value))
        {
            RiderRuntime rider = riders.Single(item => item.Profile.RiderId == command.RiderId);
            if (rider.Profile.OrganizationId != command.OrganizationId)
            {
                throw new InvalidOperationException("Race command organization does not own its rider.");
            }

            rider.Intent = command.Kind;
            rider.IntentUntilSecond = command.Kind switch
            {
                RaceCommandKind.Attack => checked(simulationSecond + AttackDurationSeconds),
                RaceCommandKind.ForcePace => checked(simulationSecond + ForcePaceDurationSeconds),
                RaceCommandKind.LaunchSprint => int.MaxValue,
                _ => int.MaxValue,
            };
        }
    }

    private void ApplyBunchSprintIntents()
    {
        RiderRuntime[] unfinished = riders
            .Where(rider => rider.FinishTimeSeconds is null)
            .ToArray();
        if (unfinished.Length == 0)
        {
            return;
        }

        RiderRuntime leader = unfinished
            .OrderByDescending(rider => rider.DistanceM)
            .ThenBy(rider => rider.Profile.RiderId.Value)
            .First();
        BunchSprintRiderSnapshot[] snapshots = unfinished
            .Select(rider => new BunchSprintRiderSnapshot(
                rider.Profile.RiderId,
                rider.GroupId,
                rider.DistanceM,
                rider.SpeedMps))
            .ToArray();
        if (!BunchSprintResolver.ShouldLaunch(
                scenario.Definition,
                scenario.ClassifiedStageType,
                leader.DistanceM,
                leader.SpeedMps,
                leader.GroupId,
                snapshots))
        {
            return;
        }

        double safeSpeedMps = Math.Max(0.1, leader.SpeedMps);
        foreach (RiderRuntime rider in unfinished)
        {
            if (rider.GroupId != leader.GroupId)
            {
                continue;
            }

            double gapM = leader.DistanceM - rider.DistanceM;
            double gapSeconds = gapM / safeSpeedMps;
            if (gapM > BunchSprintResolver.LeadGroupGapM &&
                gapSeconds > BunchSprintResolver.LeadGroupGapSeconds)
            {
                continue;
            }

            rider.Intent = RaceCommandKind.LaunchSprint;
            rider.IntentUntilSecond = int.MaxValue;
        }
    }

    private bool DetectDecision()
    {
        int planIndex = Enumerable.Range(0, scenario.TacticalPlans.Count)
            .FirstOrDefault(
                index => !evaluatedTacticalPlans.Contains(index) &&
                         scenario.TacticalPlans[index].TriggerSecond <= simulationSecond,
                -1);
        if (planIndex < 0)
        {
            return false;
        }

        evaluatedTacticalPlans.Add(planIndex);
        RaceTacticalPlan plan = scenario.TacticalPlans[planIndex];
        ChaseDecision decision = ChaseDecisionEvaluator.Evaluate(plan.Observation, plan.Briefing);
        bool wasRecentlyAsked = lastDecisionSecond is int previousSecond &&
                                simulationSecond - previousSecond < 60;
        RaceDecisionGateResult gate = RaceDecisionGate.Evaluate(
            plan.Observation,
            plan.Briefing,
            decision,
            wasRecentlyAsked);
        if (!gate.CreateRequest)
        {
            ApplyStrategicChoice(plan, decision.SelectedOption);
            decisionCount++;
            lastDecisionSecond = simulationSecond;
            EmitDecisionTrace(
                new PendingDecisionContext(plan, decision, gate, RequestFor(plan, decision, gate)),
                decision.SelectedOption);
            return false;
        }

        RaceDecisionRequest request = RequestFor(plan, decision, gate);
        pendingDecisionContext = new PendingDecisionContext(plan, decision, gate, request);
        EmitRequestTrace(pendingDecisionContext);
        return true;
    }

    private RaceDecisionRequest RequestFor(
        RaceTacticalPlan plan,
        ChaseDecision decision,
        RaceDecisionGateResult gate)
    {
        return new RaceDecisionRequest(
            new RaceDecisionRequestId(
                $"{scenario.Id}:chase:{plan.Observation.OrganizationId.Value}:{simulationSecond}"),
            plan.Observation.DecisionAuthorityId,
            simulationSecond,
            $"Visible split at official gap {plan.Observation.OfficialGapSeconds}s",
            gate.DefensibleOptions,
            decision.SelectedOption);
    }

    private void ApplyStrategicChoice(RaceTacticalPlan plan, RaceDecisionOption selectedOption)
    {
        RaceCommandKind? commandKind = selectedOption switch
        {
            RaceDecisionOption.CommitSupport => RaceCommandKind.ForcePace,
            RaceDecisionOption.WaitForRivals => RaceCommandKind.Conserve,
            RaceDecisionOption.ProtectSecondLeader => RaceCommandKind.Conserve,
            RaceDecisionOption.TrustDs => null,
            _ => throw new InvalidOperationException("Unsupported race decision option."),
        };
        if (commandKind is RaceCommandKind command)
        {
            resolvedCommands.Add(new RaceCommand(
                simulationSecond,
                plan.Observation.OrganizationId,
                plan.SupportRiderId,
                command));
        }
    }

    private void EmitRequestTrace(PendingDecisionContext context)
    {
        spySink.Emit(BuildTrace(
            context,
            selectedOption: string.Empty,
            selectionReasons: context.Gate.Diagnostics,
            commands: Array.Empty<string>()));
    }

    private void EmitDecisionTrace(PendingDecisionContext context, RaceDecisionOption selectedOption)
    {
        string[] commands = selectedOption switch
        {
            RaceDecisionOption.CommitSupport => new[] { $"ForcePace rider:{context.Plan.SupportRiderId.Value}" },
            RaceDecisionOption.WaitForRivals => new[] { $"Conserve rider:{context.Plan.SupportRiderId.Value}" },
            RaceDecisionOption.ProtectSecondLeader => new[] { $"Conserve rider:{context.Plan.SupportRiderId.Value}" },
            RaceDecisionOption.TrustDs => new[] { "Continue DS policy" },
            _ => throw new InvalidOperationException("Unsupported race decision option."),
        };
        spySink.Emit(BuildTrace(
            context,
            selectedOption.ToString(),
            context.Decision.SelectionReasons,
            commands));
    }

    private DecisionTrace BuildTrace(
        PendingDecisionContext context,
        string selectedOption,
        IReadOnlyList<string> selectionReasons,
        IReadOnlyList<string> commands)
    {
        TeamRaceObservation observation = context.Plan.Observation;
        Dictionary<string, string> knownInputs = new(StringComparer.Ordinal)
        {
            ["OfficialGapSeconds"] = observation.OfficialGapSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            ["VisibleSplit"] = observation.VisibleSplit.ToString(),
            ["LeaderPositionBand"] = observation.LeaderPositionBand.ToString(),
            ["ThreatEstimate"] = observation.ThreatEstimate.ToString(),
            ["Objective"] = observation.Objective.ToString(),
        };
        return new DecisionTrace(
            context.Request.Id.Value,
            simulationSecond,
            "Race",
            "ChaseResponse",
            ActorPersonId: null,
            observation.OrganizationId,
            observation.DecisionAuthorityId,
            context.Request.Trigger,
            new[] { observation.Objective.ToString() },
            context.Gate.Diagnostics,
            knownInputs,
            context.Decision.Interpretations,
            observation.Confidence.ToString(),
            context.Decision.OptionEvaluations,
            selectedOption,
            selectionReasons,
            commands,
            new[] { observation.OrganizationId, context.Plan.SupportRiderId },
            TruthDebugRef: $"{scenario.Id}:truth:t{simulationSecond}");
    }

    private Dictionary<int, double> DetermineGroupTargetSpeeds()
    {
        Dictionary<int, double> targets = new();
        RiderRuntime[] unfinished = riders
            .Where(rider => rider.FinishTimeSeconds is null)
            .ToArray();
        foreach (IGrouping<int, RiderRuntime> group in unfinished
                     .GroupBy(rider => rider.GroupId)
                     .OrderBy(grouping => grouping.Key))
        {
            RiderRuntime pacingReference = ResolvePacingReferenceRider(group);
            RaceRouteSegment segment = scenario.Definition.SegmentAt(pacingReference.DistanceM);
            double remainingM = scenario.Definition.TotalLengthM - pacingReference.DistanceM;
            AtmosphereSample atmosphere = AtmosphereForPhysics(segment);
            double basePaceMps = BasePaceMps(atmosphere.Gradient);
            if (routeHasCobble)
            {
                double projectedToM = pacingReference.DistanceM +
                    (Math.Max(0.0, pacingReference.SpeedMps) * StepSeconds);
                if (CrossedAsphaltCobbleTransition(pacingReference.DistanceM, projectedToM))
                {
                    pacingReferenceSurgeUntilSecond[pacingReference.Profile.RiderId] =
                        simulationSecond + RaceTuning.CobbleSurgeSeconds;
                }
            }

            double selectiveTargetMps = IsSelectiveZone(segment, remainingM)
                ? SelectiveGroupTargetMps(group, segment, atmosphere, remainingM)
                : basePaceMps;
            if (routeHasCobble &&
                pacingReferenceSurgeUntilSecond.TryGetValue(pacingReference.Profile.RiderId, out int surgeUntil) &&
                simulationSecond < surgeUntil)
            {
                selectiveTargetMps += RaceTuning.CobbleSurgeSpeedMps;
            }
            double target = group.Max(rider =>
            {
                double riderRemainingM = scenario.Definition.TotalLengthM - rider.DistanceM;
                bool sitIn = IsClassifiedFlatSitIn(riderRemainingM);
                return rider.Intent switch
                {
                    RaceCommandKind.ForcePace => sitIn
                        ? basePaceMps
                        : selectiveTargetMps + ForcePaceSpeedIncreaseMps,
                    RaceCommandKind.Attack => sitIn
                        ? basePaceMps
                        : selectiveTargetMps + AttackSpeedIncreaseMps,
                    RaceCommandKind.Conserve => basePaceMps - ConserveSpeedDecreaseMps,
                    RaceCommandKind.LaunchSprint => selectiveTargetMps,
                    _ => selectiveTargetMps,
                };
            });
            targets.Add(group.Key, target);
        }

        return targets;
    }

    private double SelectiveGroupTargetMps(
        IGrouping<int, RiderRuntime> group,
        RaceRouteSegment segment,
        AtmosphereSample atmosphere,
        double remainingM)
    {
        double tempoFactor = remainingM <= RaceTuning.FinaleM
            ? RaceTuning.TempoFactorFinale
            : RaceTuning.TempoFactorOutsideFinale;
        double referenceSpeedMps = BasePaceMps(atmosphere.Gradient);
        RiderRuntime? setter = null;
        double bestFrontSpeedMps = double.NegativeInfinity;
        foreach (RiderRuntime rider in group.OrderBy(item => item.Profile.RiderId.Value))
        {
            CapabilityResult capability = CapabilitySolver.Evaluate(
                rider.Profile,
                rider.Physiology,
                rider.Profile.CriticalPowerW,
                StepSeconds);
            double sustainablePowerW = capability.EffectiveCriticalPowerW * tempoFactor;
            double frontSpeedMps = MaxSustainableFrontSpeedMps(
                rider.Profile,
                segment,
                atmosphere,
                sustainablePowerW);
            if (frontSpeedMps > bestFrontSpeedMps ||
                (Math.Abs(frontSpeedMps - bestFrontSpeedMps) < 1e-9 &&
                 (setter is null || rider.Profile.RiderId.Value < setter.Profile.RiderId.Value)))
            {
                bestFrontSpeedMps = frontSpeedMps;
                setter = rider;
            }
        }

        if (setter is null)
        {
            return referenceSpeedMps;
        }

        return Math.Max(referenceSpeedMps, bestFrontSpeedMps);
    }

    private double MaxSustainableFrontSpeedMps(
        RaceRiderProfile profile,
        RaceRouteSegment segment,
        AtmosphereSample atmosphere,
        double sustainablePowerW)
    {
        if (sustainablePowerW <= 0.0)
        {
            return 0.0;
        }

        double low = 2.0;
        double high = 15.0;
        for (int iteration = 0; iteration < 24; iteration++)
        {
            double mid = (low + high) * 0.5;
            RequiredPowerBreakdown demand = FrontRequiredPowerAtSpeed(mid, profile, segment, atmosphere);
            if (demand.TotalPowerW <= sustainablePowerW)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    private RequiredPowerBreakdown FrontRequiredPowerAtSpeed(
        double speedMps,
        RaceRiderProfile profile,
        RaceRouteSegment segment,
        AtmosphereSample atmosphere)
    {
        double yawRadians = atmosphere.WindYawDegrees * (Math.PI / 180.0);
        double headwindMps = Math.Cos(yawRadians) * atmosphere.WindSpeedMps;
        double crosswindMps = Math.Sin(yawRadians) * atmosphere.WindSpeedMps;
        double relativeAirSpeedMps = Math.Sqrt(
            Math.Pow(Math.Max(0.0, speedMps + headwindMps), 2.0) +
            Math.Pow(crosswindMps, 2.0));
        RequiredPowerBreakdown frontDemand = RequiredPowerSolver.Calculate(new RequiredPowerInput(
            speedMps,
            0.0,
            atmosphere.Gradient,
            scenario.Definition.AirDensityKgPerM3,
            relativeAirSpeedMps,
            profile.CdAM2,
            1.0,
            EffectiveCrr(profile.BaseCrr, profile.Handling, segment.Surface),
            profile.TotalMassKg));
        return ApplyCobbleDemandMultiplier(
            frontDemand,
            segment.Surface,
            profile.Handling,
            scenario.ClassifiedStageType);
    }

    private void ApplyPositionDrift()
    {
        RiderRuntime[] unfinished = riders
            .Where(rider => rider.FinishTimeSeconds is null)
            .ToArray();
        foreach (IGrouping<int, RiderRuntime> group in unfinished
                     .GroupBy(rider => rider.GroupId)
                     .Where(grouping => grouping.Count() >= 2)
                     .OrderBy(grouping => grouping.Key))
        {
            RiderRuntime leader = group
                .OrderByDescending(rider => rider.DistanceM)
                .ThenBy(rider => rider.Profile.RiderId.Value)
                .First();
            double leaderDistanceM = leader.DistanceM;
            double remainingM = scenario.Definition.TotalLengthM - leaderDistanceM;
            (WorldEntityId RiderId, double PeakPowerPerKg)[] peakPowerPerKg = group
                .Select(rider => (rider.Profile.RiderId, rider.Profile.PeakPowerW / rider.Profile.BodyMassKg))
                .ToArray();
            RiderRuntime[] orderedByScore = group
                .OrderByDescending(rider => PositionScoreResolver.Score(
                    rider.Profile,
                    rider.Intent,
                    remainingM,
                    scenario.ClassifiedStageType,
                    peakPowerPerKg))
                .ThenBy(rider => rider.Profile.RiderId.Value)
                .ToArray();
            Dictionary<WorldEntityId, int> slotTargets = new();
            for (int index = 0; index < orderedByScore.Length; index++)
            {
                slotTargets[orderedByScore[index].Profile.RiderId] = index;
            }

            double maxDriftM = RaceTuning.DriftMps * StepSeconds;
            double maxGapBehindAheadM = RaceTuning.GroupSplitGapM - 0.1;
            foreach (RiderRuntime rider in orderedByScore.OrderBy(item => item.Profile.RiderId.Value))
            {
                int slotTarget = slotTargets[rider.Profile.RiderId];
                double targetDistanceM = leaderDistanceM - (slotTarget * RaceTuning.SlotSpacingM);
                double delta = Math.Clamp(targetDistanceM - rider.DistanceM, -maxDriftM, maxDriftM);
                double newDistanceM = rider.DistanceM + delta;
                newDistanceM = Math.Min(newDistanceM, leaderDistanceM);
                RiderRuntime? riderAhead = orderedByScore
                    .Where(item => slotTargets[item.Profile.RiderId] < slotTarget)
                    .OrderByDescending(item => slotTargets[item.Profile.RiderId])
                    .FirstOrDefault();
                if (riderAhead is not null)
                {
                    double minimumDistanceM = riderAhead.DistanceM - maxGapBehindAheadM;
                    newDistanceM = Math.Max(newDistanceM, minimumDistanceM);
                }

                rider.DistanceM = newDistanceM;
            }
        }
    }

    private RiderRuntime ResolvePacingReferenceRider(IGrouping<int, RiderRuntime> group)
    {
        RiderRuntime[] ordered = group
            .OrderByDescending(rider => rider.DistanceM)
            .ThenBy(rider => rider.Profile.RiderId.Value)
            .ToArray();
        RiderRuntime? cobbleRider = ordered.FirstOrDefault(rider =>
            scenario.Definition.SegmentAt(rider.DistanceM).Surface == RouteSurface.Cobble);
        if (cobbleRider is not null)
        {
            return cobbleRider;
        }

        RiderRuntime? steepRider = ordered.FirstOrDefault(rider =>
            scenario.Definition.SegmentAt(rider.DistanceM).Gradient >= RaceTuning.SelectiveGradientThreshold);
        if (steepRider is not null)
        {
            return steepRider;
        }

        return ordered[0];
    }

    private bool IsSelectiveZone(RaceRouteSegment segment, double remainingM)
    {
        if (segment.Surface == RouteSurface.Cobble || segment.Gradient >= RaceTuning.SelectiveGradientThreshold)
        {
            return true;
        }

        if (remainingM > RaceTuning.FinaleM)
        {
            return false;
        }

        return scenario.ClassifiedStageType is ClassifiedStageType.CobbleClassic
            or ClassifiedStageType.Hilly
            or ClassifiedStageType.Mixed
            or ClassifiedStageType.Mountain
            or ClassifiedStageType.MountainSummit;
    }

    private void ResolveGroups()
    {
        RiderRuntime? leader = riders
            .Where(rider => rider.FinishTimeSeconds is null)
            .OrderByDescending(rider => rider.DistanceM)
            .ThenBy(rider => rider.Profile.RiderId.Value)
            .FirstOrDefault();
        if (leader is null)
        {
            return;
        }

        RaceRouteSegment segment = scenario.Definition.SegmentAt(leader.DistanceM);
        double remainingM = scenario.Definition.TotalLengthM - leader.DistanceM;
        AtmosphereSample atmosphere = AtmosphereForPhysics(segment);
        GroupResolution resolution = PositionAndGroupResolver.Resolve(new GroupResolutionInput(
            segment.RoadWidthM,
            atmosphere.WindSpeedMps,
            atmosphere.WindYawDegrees,
            riders
                .Where(rider => rider.FinishTimeSeconds is null)
                .Select(rider => new RaceRiderSnapshot(
                    rider.Profile.RiderId,
                    rider.DistanceM,
                    rider.SpeedMps,
                    rider.Profile.Positioning))
                .ToArray()));
        maximumGroupCount = Math.Max(maximumGroupCount, resolution.Groups.Count);
        foreach (ResolvedRaceRiderPosition position in resolution.Riders)
        {
            RiderRuntime rider = riders.Single(item => item.Profile.RiderId == position.RiderId);
            bool previouslySheltered = rider.ShelterMultiplier < 1.0;
            bool nowSheltered = position.ShelterMultiplier < 1.0;
            if (previouslySheltered && !nowSheltered)
            {
                rider.LostShelterTransitions++;
            }

            rider.GroupId = position.GroupId;
            rider.PositionSlot = position.PositionSlot;
            rider.MaximumGapAheadM = Math.Max(rider.MaximumGapAheadM, position.GapAheadM);
            rider.ShelterMultiplier = position.ShelterMultiplier;
            RecordPressureGap(rider, position, resolution);
        }
    }

    private void RecordPressureGap(
        RiderRuntime rider,
        ResolvedRaceRiderPosition position,
        GroupResolution resolution)
    {
        if (rider.FinishTimeSeconds is not null)
        {
            return;
        }

        WorldEntityId[] paceSetterIds = riders
            .Where(item => item.Intent == RaceCommandKind.ForcePace &&
                           item.FinishTimeSeconds is null)
            .Select(item => item.Profile.RiderId)
            .ToArray();
        if (paceSetterIds.Length == 0)
        {
            return;
        }

        HashSet<int> pressureGroupIds = resolution.Riders
            .Where(item => paceSetterIds.Contains(item.RiderId))
            .Select(item => item.GroupId)
            .ToHashSet();
        if (pressureGroupIds.Count == 0)
        {
            return;
        }

        double pressureGapM;
        if (pressureGroupIds.Contains(position.GroupId))
        {
            pressureGapM = position.GapAheadM;
        }
        else
        {
            double paceGroupRearDistanceM = resolution.Riders
                .Where(item => pressureGroupIds.Contains(item.GroupId))
                .Min(item => riders.Single(runtime => runtime.Profile.RiderId == item.RiderId).DistanceM);
            pressureGapM = Math.Max(0.0, paceGroupRearDistanceM - rider.DistanceM);
        }

        rider.MaximumGapDuringPressureM = Math.Max(rider.MaximumGapDuringPressureM, pressureGapM);
    }

    private void ExpireIntents()
    {
        foreach (RiderRuntime rider in riders)
        {
            if (simulationSecond >= rider.IntentUntilSecond)
            {
                rider.Intent = RaceCommandKind.HoldPosition;
                rider.IntentUntilSecond = int.MaxValue;
            }
        }
    }

    private RaceResult BuildResult()
    {
        RaceRiderMetrics[] metrics = riders
            .Select(rider => new RaceRiderMetrics(
                rider.Profile.RiderId,
                rider.Profile.OrganizationId,
                rider.FinishTimeSeconds!.Value,
                rider.EnergySpentJ,
                rider.Physiology.WPrimeRemainingJ,
                rider.TimeAboveCriticalPowerSeconds,
                rider.MaximumGapAheadM,
                rider.MaximumGapDuringPressureM,
                rider.LostShelterTransitions,
                rider.GroupId))
            .OrderBy(rider => rider.FinishTimeSeconds)
            .ThenBy(rider => rider.RiderId.Value)
            .ToArray();
        WorldEntityId[] finishOrder = metrics.Select(metric => metric.RiderId).ToArray();
        Dictionary<WorldEntityId, double> teamEnergyJ = metrics
            .GroupBy(metric => metric.OrganizationId)
            .OrderBy(group => group.Key.Value)
            .ToDictionary(group => group.Key, group => group.Sum(metric => metric.EnergySpentJ));
        RaceResult provisional = new(
            scenario.Id,
            scenario.Definition.Id,
            PhysicsContractVersion,
            finishOrder,
            metrics,
            teamEnergyJ,
            maximumGroupCount,
            decisionCount,
            Checksum: string.Empty);
        return provisional with
        {
            Checksum = RaceResultChecksum.Compute(provisional),
        };
    }

    private bool IsClassifiedFlatSitIn(double remainingM) =>
        scenario.ClassifiedStageType == ClassifiedStageType.Flat &&
        remainingM > BunchSprintResolver.KickDistanceM;

    private double ShelterForPhysics(
        double shelterMultiplier,
        double remainingM,
        RouteSurface surface,
        double handling)
    {
        double shelter = shelterMultiplier;
        if (IsClassifiedFlatSitIn(remainingM))
        {
            shelter = Math.Min(shelter, ClassifiedFlatSitInShelterMultiplier);
        }

        if (surface == RouteSurface.Cobble)
        {
            shelter = PositionScoreResolver.EffectiveCobbleShelter(shelter, handling);
        }

        return shelter;
    }

    private bool CrossedAsphaltCobbleTransition(double fromDistanceM, double toDistanceM)
    {
        if (Math.Abs(toDistanceM - fromDistanceM) < 1e-9)
        {
            return false;
        }

        double minM = Math.Min(fromDistanceM, toDistanceM);
        double maxM = Math.Max(fromDistanceM, toDistanceM);
        List<RouteSurface> surfaces = new()
        {
            scenario.Definition.SegmentAt(minM).Surface,
        };
        double cumulativeM = 0.0;
        foreach (RaceRouteSegment segment in scenario.Definition.Segments)
        {
            cumulativeM += segment.LengthM;
            if (cumulativeM > minM && cumulativeM < maxM)
            {
                surfaces.Add(scenario.Definition.SegmentAt(cumulativeM + 1e-6).Surface);
            }
        }

        RouteSurface endSurface = scenario.Definition.SegmentAt(maxM).Surface;
        if (surfaces[^1] != endSurface)
        {
            surfaces.Add(endSurface);
        }

        for (int index = 1; index < surfaces.Count; index++)
        {
            if (IsAsphaltCobbleTransition(surfaces[index - 1], surfaces[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAsphaltCobbleTransition(RouteSurface from, RouteSurface to) =>
        (from == RouteSurface.Asphalt && to == RouteSurface.Cobble) ||
        (from == RouteSurface.Cobble && to == RouteSurface.Asphalt);

    private static double CobbleSurgeMultiplier(
        RouteSurface surface,
        double handling,
        ClassifiedStageType? classifiedStageType) =>
        surface == RouteSurface.Cobble
            ? PositionScoreResolver.CobbleSurgeMultiplier(handling)
            : 1.0;

    private static RequiredPowerBreakdown ApplyCobbleDemandMultiplier(
        RequiredPowerBreakdown demand,
        RouteSurface surface,
        double handling,
        ClassifiedStageType? classifiedStageType)
    {
        double multiplier = CobbleSurgeMultiplier(surface, handling, classifiedStageType);
        if (multiplier <= 1.0)
        {
            return demand;
        }

        return demand with
        {
            AerodynamicPowerW = demand.AerodynamicPowerW * multiplier,
            RollingPowerW = demand.RollingPowerW * multiplier,
            GravityPowerW = demand.GravityPowerW * multiplier,
            AccelerationPowerW = demand.AccelerationPowerW * multiplier,
        };
    }

    private AtmosphereSample AtmosphereForPhysics(RaceRouteSegment segment)
    {
        if (scenario.ClassifiedStageType != ClassifiedStageType.Flat)
        {
            return new AtmosphereSample(segment.Gradient, segment.WindSpeedMps, segment.WindYawDegrees);
        }

        return new AtmosphereSample(
            Math.Min(segment.Gradient, ClassifiedFlatSitInMaxGradient),
            Math.Min(segment.WindSpeedMps, ClassifiedFlatSitInMaxWindMps),
            WindYawDegrees: 0.0);
    }

    private static double BasePaceMps(double gradient)
    {
        return Math.Max(4.0, 11.0 - (Math.Max(0.0, gradient) * 70.0));
    }

    private static double RealizedSpeed(
        double desiredSpeedMps,
        double requiredPowerW,
        double realizablePowerW,
        double gradient,
        RouteSurface surface)
    {
        if (requiredPowerW <= 0.0 || realizablePowerW >= requiredPowerW)
        {
            return desiredSpeedMps;
        }

        double powerRatio = Math.Clamp(realizablePowerW / requiredPowerW, 0.0, 1.0);
        double exponent = gradient >= RaceTuning.SelectiveGradientThreshold ? 0.85 : 1.0 / 3.0;
        return desiredSpeedMps * Math.Pow(powerRatio, exponent);
    }

    private sealed class RiderRuntime
    {
        public RiderRuntime(RaceRiderProfile profile, double distanceM, double speedMps)
        {
            Profile = profile;
            DistanceM = distanceM;
            SpeedMps = speedMps;
            Physiology = RiderPhysiologyState.Fresh(profile);
        }

        public RaceRiderProfile Profile { get; }

        public double DistanceM { get; set; }

        public double SpeedMps { get; set; }

        public RiderPhysiologyState Physiology { get; set; }

        public RaceCommandKind Intent { get; set; } = RaceCommandKind.HoldPosition;

        public int IntentUntilSecond { get; set; } = int.MaxValue;

        public int GroupId { get; set; } = 1;

        public int PositionSlot { get; set; }

        public double ShelterMultiplier { get; set; } = 1.0;

        public double MaximumGapAheadM { get; set; }

        public double MaximumGapDuringPressureM { get; set; }

        public int LostShelterTransitions { get; set; }

        public double EnergySpentJ { get; set; }

        public int TimeAboveCriticalPowerSeconds { get; set; }

        public double? FinishTimeSeconds { get; set; }
    }

    private readonly record struct AtmosphereSample(
        double Gradient,
        double WindSpeedMps,
        double WindYawDegrees);

    private sealed record StepSolve(
        double RealizedSpeedMps,
        double RealizablePowerW,
        double EffectiveCriticalPowerW,
        RiderPhysiologyState NextPhysiology);

    private sealed record PendingDecisionContext(
        RaceTacticalPlan Plan,
        ChaseDecision Decision,
        RaceDecisionGateResult Gate,
        RaceDecisionRequest Request);

    private static double EffectiveCrr(double baseCrr, double handling, RouteSurface surface)
    {
        double surfaceDelta = surface switch
        {
            RouteSurface.Asphalt => 0.0,
            RouteSurface.WhiteRoad => 0.0025,
            RouteSurface.Gravel => 0.0050,
            RouteSurface.Cobble => RaceTuning.CobbleCrrDelta,
            _ => 0.0,
        };

        if (surface != RouteSurface.Cobble)
        {
            return baseCrr + (surfaceDelta * (1.35 - (0.50 * handling)));
        }

        return baseCrr + (surfaceDelta * (RaceTuning.CobbleCrrHandlingIntercept -
                                          (RaceTuning.CobbleCrrHandlingSlope * handling)));
    }
}
