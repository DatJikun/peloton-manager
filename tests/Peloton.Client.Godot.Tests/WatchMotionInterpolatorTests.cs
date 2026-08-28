using System;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class WatchMotionInterpolatorTests
{
    [Fact]
    public void InterpolationStaysBetweenOfficialFramesWithoutTeleport()
    {
        RaceWatchFrame first = Frame(
            watchSecond: 4,
            raceSecond: 20,
            paused: false,
            new RaceWatchRiderFrame(new WorldEntityId(1006), 100, 0, 10, 0.7, 0.02),
            new RaceWatchRiderFrame(new WorldEntityId(1007), 80, 20, 9, 1.0, 0.02));
        RaceWatchFrame second = Frame(
            watchSecond: 5,
            raceSecond: 25,
            paused: false,
            new RaceWatchRiderFrame(new WorldEntityId(1006), 150, 0, 12, 0.8, 0.04),
            new RaceWatchRiderFrame(new WorldEntityId(1007), 110, 40, 8, 1.0, 0.04));

        InterpolatedWatchView halfway = WatchMotionInterpolator.Project(first, second, 0.5);

        Assert.Equal(5, halfway.WatchSecond);
        Assert.Equal(25, halfway.RaceSecond);
        Assert.Equal(0.5, halfway.InterpolationT);
        InterpolatedRiderView leader = Assert.Single(halfway.Riders, rider => rider.RiderId == 1006);
        Assert.Equal(125, leader.DistanceM, 8);
        Assert.Equal(11, leader.SpeedMps, 8);
        Assert.InRange(leader.Progress, 0.0, 1.0);
        Assert.InRange(leader.DistanceM, 100, 150);
        Assert.DoesNotContain("WPrime", leader.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PausedFramePinsIconsToTheOfficialSnapshot()
    {
        RaceWatchFrame previous = Frame(
            watchSecond: 8,
            raceSecond: 40,
            paused: false,
            new RaceWatchRiderFrame(new WorldEntityId(1006), 200, 0, 10, 0.7, 0));
        RaceWatchFrame paused = Frame(
            watchSecond: 9,
            raceSecond: 41,
            paused: true,
            new RaceWatchRiderFrame(new WorldEntityId(1006), 210, 0, 10, 0.7, 0));

        InterpolatedWatchView frozen = WatchMotionInterpolator.Project(previous, paused, 0.1);

        Assert.True(frozen.Paused);
        Assert.Equal(1.0, frozen.InterpolationT);
        Assert.Equal(210, frozen.Riders[0].DistanceM, 8);
    }

    private static RaceWatchFrame Frame(
        int watchSecond,
        int raceSecond,
        bool paused,
        params RaceWatchRiderFrame[] riders)
    {
        return new RaceWatchFrame(watchSecond, raceSecond, 5, paused, 1_000, riders);
    }
}
