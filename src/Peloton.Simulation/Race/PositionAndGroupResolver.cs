using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public sealed record GroupResolutionInput(
    double RoadWidthM,
    double WindSpeedMps,
    double WindYawDegrees,
    IReadOnlyList<RaceRiderSnapshot> Riders);

public static class PositionAndGroupResolver
{
    private const double ApproximateRiderLaneWidthM = 0.8;
    private const double GroupSplitGapM = RaceTuning.GroupSplitGapM;
    private const double ShelterLossGapM = 2.5;
    private const double StrongCrosswindMps = 6.0;
    private const double BaseShelterMultiplier = 0.62;
    private const double ShelterDegradationPerSlot = 0.03;
    private const double MaximumUsefulShelterMultiplier = 0.85;

    public static GroupResolution Resolve(GroupResolutionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Riders);
        RequirePositive(input.RoadWidthM, nameof(input));
        RequireNonNegative(input.WindSpeedMps, nameof(input));
        RequireFinite(input.WindYawDegrees, nameof(input));

        RaceRiderSnapshot[] ordered = input.Riders
            .OrderByDescending(rider => rider.DistanceM)
            .ThenBy(rider => rider.RiderId.Value)
            .ToArray();
        if (ordered.Length == 0)
        {
            return new GroupResolution(
                CalculateShelterCapacity(input),
                Array.Empty<ResolvedRaceRiderPosition>(),
                Array.Empty<RaceGroupState>());
        }

        int shelterCapacity = CalculateShelterCapacity(input);
        List<ResolvedRaceRiderPosition> resolved = new(ordered.Length);
        List<List<WorldEntityId>> groupMembers = new();
        int groupId = 1;
        int positionSlot = 0;
        groupMembers.Add(new List<WorldEntityId>());

        for (int index = 0; index < ordered.Length; index++)
        {
            RaceRiderSnapshot rider = ordered[index];
            RequireNonNegative(rider.DistanceM, nameof(input));
            RequireNonNegative(rider.SpeedMps, nameof(input));
            RequireUnitInterval(rider.Positioning, nameof(input));

            double gapAheadM = index == 0
                ? 0.0
                : Math.Max(0.0, ordered[index - 1].DistanceM - rider.DistanceM);
            if (index > 0 && gapAheadM > GroupSplitGapM)
            {
                groupId++;
                positionSlot = 0;
                groupMembers.Add(new List<WorldEntityId>());
            }

            bool hasUsefulShelter = positionSlot > 0 &&
                positionSlot <= shelterCapacity &&
                gapAheadM <= ShelterLossGapM;
            double shelterMultiplier = hasUsefulShelter
                ? Math.Min(
                    MaximumUsefulShelterMultiplier,
                    BaseShelterMultiplier + ((positionSlot - 1) * ShelterDegradationPerSlot))
                : 1.0;
            resolved.Add(new ResolvedRaceRiderPosition(
                rider.RiderId,
                positionSlot,
                groupId,
                gapAheadM,
                shelterMultiplier));
            groupMembers[^1].Add(rider.RiderId);
            positionSlot++;
        }

        RaceGroupState[] groups = groupMembers
            .Select((members, index) => new RaceGroupState(index + 1, members.ToArray()))
            .ToArray();
        return new GroupResolution(shelterCapacity, resolved, groups);
    }

    private static int CalculateShelterCapacity(GroupResolutionInput input)
    {
        int lateralSlots = Math.Max(1, (int)Math.Floor(input.RoadWidthM / ApproximateRiderLaneWidthM));
        double yawRadians = input.WindYawDegrees * (Math.PI / 180.0);
        double crosswindMps = Math.Abs(Math.Sin(yawRadians)) * input.WindSpeedMps;
        int shelteredRows = crosswindMps >= StrongCrosswindMps ? 1 : 3;
        return checked(lateralSlots * shelteredRows);
    }

    private static void RequirePositive(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
