using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public sealed class RaceSession
{
    public const int PhysicsContractVersion = 1;

    private const double StepSeconds = 1.0;
    private const int AttackDurationSeconds = 18;
    private const int ForcePaceDurationSeconds = 90;
    private const double ForcePaceSpeedIncreaseMps = 1.2;
    private const double AttackSpeedIncreaseMps = 3.0;
    private const double ConserveSpeedDecreaseMps = 0.8;

    private readonly RaceScenario scenario;
    private readonly RiderRuntime[] riders;
    private int simulationSecond;
    private int maximumGroupCount = 1;
    private long[] justFinishedRiderIds = Array.Empty<long>();

    internal RaceSession(RaceScenario scenario, long seed)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        this.scenario = scenario;
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
        // #region agent log
        if (IsDraftingPositionProof)
        {
            RiderRuntime? r12 = FindRider(12);
            RiderRuntime? r14 = FindRider(14);
            AgentDbg(
                "D",
                "RaceSession.cs:ctor",
                "initial ResolveGroups after start positions",
                new
                {
                    scenario.Id,
                    simulationSecond,
                    unfinished = FormatUnfinishedRiders(),
                    r12Max = r12?.MaximumGapAheadM,
                    r12Dist = r12?.DistanceM,
                    r12Shelter = r12?.ShelterMultiplier,
                    r14Max = r14?.MaximumGapAheadM,
                    r14Dist = r14?.DistanceM,
                    r14Shelter = r14?.ShelterMultiplier,
                    totalLengthM = scenario.Definition.TotalLengthM,
                });
        }
        // #endregion
    }

    public long Seed { get; }

    public bool IsCompleted => Result is not null;

    public RaceResult? Result { get; private set; }

    public RaceStepResult Step()
    {
        if (Result is not null)
        {
            return new RaceStepResult(RaceStepStatus.Completed, Result);
        }

        if (simulationSecond >= scenario.MaximumDurationSeconds)
        {
            throw new InvalidOperationException(
                $"Race '{scenario.Id}' exceeded its maximum duration without every rider finishing.");
        }

        ApplyCommands();
        Dictionary<int, double> groupTargetSpeedMps = DetermineGroupTargetSpeeds();
        Dictionary<WorldEntityId, StepSolve> solves = new();
        foreach (RiderRuntime rider in riders.OrderBy(rider => rider.Profile.RiderId.Value))
        {
            if (rider.FinishTimeSeconds is not null)
            {
                continue;
            }

            RaceRouteSegment segment = scenario.Definition.SegmentAt(rider.DistanceM);
            double baseSpeedMps = BasePaceMps(segment);
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
            double yawRadians = segment.WindYawDegrees * (Math.PI / 180.0);
            double headwindMps = Math.Cos(yawRadians) * segment.WindSpeedMps;
            double crosswindMps = Math.Sin(yawRadians) * segment.WindSpeedMps;
            double relativeAirSpeedMps = Math.Sqrt(
                Math.Pow(Math.Max(0.0, desiredSpeedMps + headwindMps), 2.0) +
                Math.Pow(crosswindMps, 2.0));
            RequiredPowerBreakdown demand = RequiredPowerSolver.Calculate(new RequiredPowerInput(
                desiredSpeedMps,
                desiredAccelerationMps2,
                segment.Gradient,
                scenario.Definition.AirDensityKgPerM3,
                relativeAirSpeedMps,
                rider.Profile.CdAM2,
                rider.ShelterMultiplier,
                rider.Profile.BaseCrr,
                rider.Profile.TotalMassKg));
            CapabilityResult capability = CapabilitySolver.Evaluate(
                rider.Profile,
                rider.Physiology,
                demand.TotalPowerW,
                StepSeconds);
            double realizedSpeedMps = RealizedSpeed(
                desiredSpeedMps,
                demand.TotalPowerW,
                capability.RealizablePowerW,
                segment.Gradient);
            solves.Add(rider.Profile.RiderId, new StepSolve(
                realizedSpeedMps,
                capability.RealizablePowerW,
                capability.EffectiveCriticalPowerW,
                capability.NextState));
        }

        List<long> newlyFinished = new();
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
                newlyFinished.Add(rider.Profile.RiderId.Value);
            }
        }

        justFinishedRiderIds = newlyFinished.ToArray();
        // #region agent log
        if (IsDraftingPositionProof && justFinishedRiderIds.Length > 0)
        {
            RiderRuntime? r12 = FindRider(12);
            RiderRuntime? r14 = FindRider(14);
            AgentDbg(
                "A",
                "RaceSession.cs:Step:finish",
                "riders finished this step before ResolveGroups",
                new
                {
                    simulationSecond,
                    justFinished = justFinishedRiderIds,
                    unfinishedBeforeRegroup = FormatUnfinishedRiders(),
                    r12Dist = r12?.DistanceM,
                    r12Speed = r12?.SpeedMps,
                    r12Max = r12?.MaximumGapAheadM,
                    r12Finished = r12?.FinishTimeSeconds,
                    r14Dist = r14?.DistanceM,
                    r14Speed = r14?.SpeedMps,
                    r14Max = r14?.MaximumGapAheadM,
                    r14Finished = r14?.FinishTimeSeconds,
                    forcePaceIds = ForcePaceRiderIds(),
                    inPaceWindow = simulationSecond >= 5 && simulationSecond < 95,
                });
        }
        // #endregion
        simulationSecond++;
        ResolveGroups();
        justFinishedRiderIds = Array.Empty<long>();
        ExpireIntents();
        if (riders.All(rider => rider.FinishTimeSeconds is not null))
        {
            Result = BuildResult();
            return new RaceStepResult(RaceStepStatus.Completed, Result);
        }

        return new RaceStepResult(RaceStepStatus.Advanced, null);
    }

    private void ApplyCommands()
    {
        foreach (RaceCommand command in scenario.Commands
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
                _ => int.MaxValue,
            };
        }
    }

    private Dictionary<int, double> DetermineGroupTargetSpeeds()
    {
        Dictionary<int, double> targets = new();
        foreach (IGrouping<int, RiderRuntime> group in riders
                     .Where(rider => rider.FinishTimeSeconds is null)
                     .GroupBy(rider => rider.GroupId)
                     .OrderBy(group => group.Key))
        {
            double target = group.Max(rider =>
            {
                RaceRouteSegment segment = scenario.Definition.SegmentAt(rider.DistanceM);
                double basePaceMps = BasePaceMps(segment);
                return rider.Intent switch
                {
                    RaceCommandKind.ForcePace => basePaceMps + ForcePaceSpeedIncreaseMps,
                    RaceCommandKind.Attack => basePaceMps + AttackSpeedIncreaseMps,
                    RaceCommandKind.Conserve => basePaceMps - ConserveSpeedDecreaseMps,
                    _ => basePaceMps,
                };
            });
            targets.Add(group.Key, target);
        }

        return targets;
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
            // #region agent log
            if (IsDraftingPositionProof)
            {
                AgentDbg(
                    "A",
                    "RaceSession.cs:ResolveGroups:allFinished",
                    "no unfinished riders; skip regroup",
                    new
                    {
                        simulationSecond,
                        justFinished = justFinishedRiderIds,
                        finished = FormatFinishedRiders(),
                    });
            }
            // #endregion
            return;
        }

        RaceRouteSegment segment = scenario.Definition.SegmentAt(leader.DistanceM);
        GroupResolution resolution = PositionAndGroupResolver.Resolve(new GroupResolutionInput(
            segment.RoadWidthM,
            segment.WindSpeedMps,
            segment.WindYawDegrees,
            riders
                .Where(rider => rider.FinishTimeSeconds is null)
                .Select(rider => new RaceRiderSnapshot(
                    rider.Profile.RiderId,
                    rider.DistanceM,
                    rider.SpeedMps,
                    rider.Profile.Positioning))
                .ToArray()));
        maximumGroupCount = Math.Max(maximumGroupCount, resolution.Groups.Count);
        // #region agent log
        if (IsDraftingPositionProof)
        {
            int idx12 = IndexOfRider(resolution.Riders, 12);
            long? aheadOf12 = idx12 > 0 ? resolution.Riders[idx12 - 1].RiderId.Value : null;
            ResolvedRaceRiderPosition? pos12 = idx12 >= 0 ? resolution.Riders[idx12] : null;
            AgentDbg(
                "C",
                "RaceSession.cs:ResolveGroups:entry",
                "unfinished set before applying positions",
                new
                {
                    simulationSecond,
                    justFinished = justFinishedRiderIds,
                    unfinished = FormatUnfinishedRiders(),
                    finished = FormatFinishedRiders(),
                    gap12 = pos12?.GapAheadM,
                    slot12 = pos12?.PositionSlot,
                    group12 = pos12?.GroupId,
                    shelter12 = pos12?.ShelterMultiplier,
                    aheadOf12,
                    inPaceWindow = simulationSecond >= 5 && simulationSecond < 95,
                    forcePaceIds = ForcePaceRiderIds(),
                });
        }
        // #endregion
        int resolveIndex = 0;
        foreach (ResolvedRaceRiderPosition position in resolution.Riders)
        {
            RiderRuntime rider = riders.Single(item => item.Profile.RiderId == position.RiderId);
            bool previouslySheltered = rider.ShelterMultiplier < 1.0;
            bool nowSheltered = position.ShelterMultiplier < 1.0;
            if (previouslySheltered && !nowSheltered)
            {
                rider.LostShelterTransitions++;
                // #region agent log
                if (IsDraftingPositionProof && rider.Profile.RiderId.Value == 12)
                {
                    long? shelterAheadId = resolveIndex == 0
                        ? null
                        : resolution.Riders[resolveIndex - 1].RiderId.Value;
                    AgentDbg(
                        "E",
                        "RaceSession.cs:ResolveGroups:lostShelter",
                        "rider 12 lost shelter",
                        new
                        {
                            simulationSecond,
                            justFinished = justFinishedRiderIds,
                            gapAheadM = position.GapAheadM,
                            aheadId = shelterAheadId,
                            unfinished = FormatUnfinishedRiders(),
                            previousMax = rider.MaximumGapAheadM,
                            lostShelterTransitions = rider.LostShelterTransitions,
                            inPaceWindow = simulationSecond >= 5 && simulationSecond < 95,
                        });
                }
                // #endregion
            }

            double previousMax = rider.MaximumGapAheadM;
            long? aheadRiderId = resolveIndex == 0
                ? null
                : resolution.Riders[resolveIndex - 1].RiderId.Value;
            double aheadDistanceM = aheadRiderId is null
                ? 0.0
                : riders.Single(item => item.Profile.RiderId.Value == aheadRiderId.Value).DistanceM;

            rider.GroupId = position.GroupId;
            rider.PositionSlot = position.PositionSlot;
            rider.MaximumGapAheadM = Math.Max(rider.MaximumGapAheadM, position.GapAheadM);
            rider.ShelterMultiplier = position.ShelterMultiplier;

            // #region agent log
            if (IsDraftingPositionProof && rider.Profile.RiderId.Value == 12)
            {
                double maxDelta = rider.MaximumGapAheadM - previousMax;
                if (maxDelta > 0.01 || position.GapAheadM >= 4.9)
                {
                    AgentDbg(
                        maxDelta > 5.0 || justFinishedRiderIds.Length > 0 ? "A" : "B",
                        "RaceSession.cs:ResolveGroups:maxGap12",
                        "rider 12 gap/max update",
                        new
                        {
                            simulationSecond,
                            justFinished = justFinishedRiderIds,
                            gapAheadM = position.GapAheadM,
                            previousMax,
                            newMax = rider.MaximumGapAheadM,
                            maxDelta,
                            aheadRiderId,
                            aheadDistanceM,
                            riderDistanceM = rider.DistanceM,
                            riderSpeedMps = rider.SpeedMps,
                            shelter = rider.ShelterMultiplier,
                            lostShelterTransitions = rider.LostShelterTransitions,
                            groupId = rider.GroupId,
                            slot = rider.PositionSlot,
                            unfinished = FormatUnfinishedRiders(),
                            finished = FormatFinishedRiders(),
                            inPaceWindow = simulationSecond >= 5 && simulationSecond < 95,
                            forcePaceIds = ForcePaceRiderIds(),
                            finishRegroup = justFinishedRiderIds.Length > 0,
                            crossedSurvivalThreshold = previousMax < 5.0 && rider.MaximumGapAheadM >= 5.0,
                        });
                }
            }
            // #endregion
            resolveIndex++;
        }
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
            DecisionCount: 0,
            Checksum: string.Empty);
        RaceResult result = provisional with
        {
            Checksum = RaceResultChecksum.Compute(provisional),
        };
        // #region agent log
        if (IsDraftingPositionProof)
        {
            RaceRiderMetrics? m12 = metrics.FirstOrDefault(m => m.RiderId.Value == 12);
            RaceRiderMetrics? m14 = metrics.FirstOrDefault(m => m.RiderId.Value == 14);
            AgentDbg(
                "B",
                "RaceSession.cs:BuildResult",
                "final drafting metrics",
                new
                {
                    simulationSecond,
                    finishOrder = string.Join(",", finishOrder.Select(id => id.Value)),
                    r12Finish = m12?.FinishTimeSeconds,
                    r12Energy = m12?.EnergySpentJ,
                    r12W = m12?.WPrimeRemainingJ,
                    r12Max = m12?.MaximumGapAheadM,
                    r12LostShelter = m12?.LostShelterTransitions,
                    r14Finish = m14?.FinishTimeSeconds,
                    r14Energy = m14?.EnergySpentJ,
                    r14W = m14?.WPrimeRemainingJ,
                    r14Max = m14?.MaximumGapAheadM,
                    r14LostShelter = m14?.LostShelterTransitions,
                });
        }
        // #endregion
        return result;
    }

    private bool IsDraftingPositionProof =>
        string.Equals(scenario.Id, "race.proof.drafting-position", StringComparison.Ordinal);

    private RiderRuntime? FindRider(long riderId)
    {
        return riders.FirstOrDefault(rider => rider.Profile.RiderId.Value == riderId);
    }

    private static int IndexOfRider(IReadOnlyList<ResolvedRaceRiderPosition> positions, long riderId)
    {
        for (int index = 0; index < positions.Count; index++)
        {
            if (positions[index].RiderId.Value == riderId)
            {
                return index;
            }
        }

        return -1;
    }

    private long[] ForcePaceRiderIds()
    {
        return riders
            .Where(rider => rider.Intent == RaceCommandKind.ForcePace)
            .Select(rider => rider.Profile.RiderId.Value)
            .ToArray();
    }

    private string FormatUnfinishedRiders()
    {
        return string.Join(
            ",",
            riders
                .Where(rider => rider.FinishTimeSeconds is null)
                .OrderByDescending(rider => rider.DistanceM)
                .ThenBy(rider => rider.Profile.RiderId.Value)
                .Select(rider =>
                    rider.Profile.RiderId.Value + "@" +
                    rider.DistanceM.ToString("G7", CultureInfo.InvariantCulture) + "v" +
                    rider.SpeedMps.ToString("G4", CultureInfo.InvariantCulture) + "g" +
                    rider.GroupId + "sh" +
                    rider.ShelterMultiplier.ToString("G3", CultureInfo.InvariantCulture)));
    }

    private string FormatFinishedRiders()
    {
        return string.Join(
            ",",
            riders
                .Where(rider => rider.FinishTimeSeconds is not null)
                .OrderBy(rider => rider.FinishTimeSeconds)
                .ThenBy(rider => rider.Profile.RiderId.Value)
                .Select(rider =>
                    rider.Profile.RiderId.Value + "@t" +
                    rider.FinishTimeSeconds!.Value.ToString("G6", CultureInfo.InvariantCulture) + "d" +
                    rider.DistanceM.ToString("G7", CultureInfo.InvariantCulture)));
    }

    private static void AgentDbg(string hypothesisId, string location, string message, object data)
    {
        // #region agent log
        try
        {
            File.AppendAllText(
                "/opt/cursor/logs/debug.log",
                JsonSerializer.Serialize(new
                {
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                }) + "\n");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        // #endregion
    }

    private static double BasePaceMps(RaceRouteSegment segment)
    {
        return Math.Max(4.0, 11.0 - (Math.Max(0.0, segment.Gradient) * 70.0));
    }

    private static double RealizedSpeed(
        double desiredSpeedMps,
        double requiredPowerW,
        double realizablePowerW,
        double gradient)
    {
        if (requiredPowerW <= 0.0 || realizablePowerW >= requiredPowerW)
        {
            return desiredSpeedMps;
        }

        double powerRatio = Math.Clamp(realizablePowerW / requiredPowerW, 0.0, 1.0);
        double exponent = gradient >= 0.03 ? 0.85 : 1.0 / 3.0;
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

        public int LostShelterTransitions { get; set; }

        public double EnergySpentJ { get; set; }

        public int TimeAboveCriticalPowerSeconds { get; set; }

        public double? FinishTimeSeconds { get; set; }
    }

    private sealed record StepSolve(
        double RealizedSpeedMps,
        double RealizablePowerW,
        double EffectiveCriticalPowerW,
        RiderPhysiologyState NextPhysiology);
}
