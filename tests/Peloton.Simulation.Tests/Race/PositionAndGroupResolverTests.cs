using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class PositionAndGroupResolverTests
{
    private static readonly WorldEntityId RearId = new(12);

    [Fact]
    public void CrosswindLimitsShelteredSlotsWithoutScriptedSplit()
    {
        GroupResolution result = Resolve(
            roadWidthM: 3.2,
            windSpeedMps: 11.0,
            windYawDegrees: 82.0,
            TwelveRiders());

        Assert.True(result.ShelterCapacity < 12);
        Assert.True(result.Riders.Count(rider => rider.ShelterMultiplier < 1.0) < 12);
        Assert.Contains(result.Riders, rider => rider.ShelterMultiplier == 1.0);
    }

    [Fact]
    public void GrowingGapRemovesShelterAndCreatesASeparateGroup()
    {
        RaceRiderSnapshot[] riders =
        {
            Snapshot(11, 1_000.0),
            Snapshot(RearId.Value, 992.0),
        };

        GroupResolution result = Resolve(6.0, 0.0, 0.0, riders);
        ResolvedRaceRiderPosition front = result.Riders.Single(rider => rider.RiderId.Value == 11);
        ResolvedRaceRiderPosition rear = result.Riders.Single(rider => rider.RiderId == RearId);

        Assert.Equal(1.0, rear.ShelterMultiplier, 8);
        Assert.NotEqual(front.GroupId, rear.GroupId);
        Assert.Equal(8.0, rear.GapAheadM, 8);
    }

    [Fact]
    public void SmallGapKeepsRearRiderShelteredInTheSameGroup()
    {
        RaceRiderSnapshot[] riders =
        {
            Snapshot(11, 1_000.0),
            Snapshot(12, 998.4),
        };

        GroupResolution result = Resolve(6.0, 0.0, 0.0, riders);

        Assert.Single(result.Groups);
        Assert.True(result.Riders[1].ShelterMultiplier < 1.0);
    }

    [Fact]
    public void InputEnumerationOrderCannotChangeResolvedSlots()
    {
        RaceRiderSnapshot[] canonical =
        {
            Snapshot(14, 1_000.0),
            Snapshot(12, 1_000.0),
            Snapshot(13, 999.0),
        };

        GroupResolution first = Resolve(6.0, 4.0, 30.0, canonical);
        GroupResolution second = Resolve(6.0, 4.0, 30.0, canonical.Reverse().ToArray());

        Assert.Equal(
            first.Riders.Select(ToComparable),
            second.Riders.Select(ToComparable));
        Assert.Equal(12, first.Riders[0].RiderId.Value);
        Assert.Equal(14, first.Riders[1].RiderId.Value);
    }

    private static GroupResolution Resolve(
        double roadWidthM,
        double windSpeedMps,
        double windYawDegrees,
        IReadOnlyList<RaceRiderSnapshot> riders)
    {
        return PositionAndGroupResolver.Resolve(new GroupResolutionInput(
            roadWidthM,
            windSpeedMps,
            windYawDegrees,
            riders));
    }

    private static RaceRiderSnapshot[] TwelveRiders()
    {
        return Enumerable
            .Range(1, 12)
            .Select(index => Snapshot(index, 1_000.0 - ((index - 1) * 0.7)))
            .ToArray();
    }

    private static RaceRiderSnapshot Snapshot(long id, double distanceM)
    {
        return new RaceRiderSnapshot(
            new WorldEntityId(id),
            distanceM,
            speedMps: 12.0,
            positioning: 0.6);
    }

    private static object ToComparable(ResolvedRaceRiderPosition rider)
    {
        return new
        {
            rider.RiderId,
            rider.PositionSlot,
            rider.GroupId,
            rider.GapAheadM,
            rider.ShelterMultiplier,
        };
    }
}
