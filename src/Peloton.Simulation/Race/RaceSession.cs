using System;
using System.Collections.Generic;
using System.Linq;
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
        ResolveGroups();
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
            DecisionCount: 0,
            Checksum: string.Empty);
        return provisional with
        {
            Checksum = RaceResultChecksum.Compute(provisional),
        };
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

        public double MaximumGapDuringPressureM { get; set; }

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
