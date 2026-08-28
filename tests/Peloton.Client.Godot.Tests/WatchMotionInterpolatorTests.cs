using System;
using System.Collections.Generic;
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
            Rider(1006, 1, 100, 0, 10, 0.7, 0.02),
            Rider(1007, 2, 80, 20, 9, 1.0, 0.02));
        RaceWatchFrame second = Frame(
            watchSecond: 5,
            raceSecond: 25,
            paused: false,
            Rider(1006, 1, 150, 0, 12, 0.8, 0.04),
            Rider(1007, 2, 110, 40, 8, 1.0, 0.04));

        InterpolatedWatchView halfway = WatchMotionInterpolator.Project(first, second, 0.5);

        Assert.Equal(5, halfway.WatchSecond);
        Assert.Equal(25, halfway.RaceSecond);
        Assert.Equal(0.5, halfway.InterpolationT);
        InterpolatedRiderView leader = Assert.Single(halfway.Field, rider => rider.RiderId == 1006);
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
            Rider(1006, 1, 200, 0, 10, 0.7, 0));
        RaceWatchFrame paused = Frame(
            watchSecond: 9,
            raceSecond: 41,
            paused: true,
            Rider(1006, 1, 210, 0, 10, 0.7, 0));

        InterpolatedWatchView frozen = WatchMotionInterpolator.Project(previous, paused, 0.1);

        Assert.True(frozen.Paused);
        Assert.Equal(1.0, frozen.InterpolationT);
        Assert.Equal(210, frozen.Riders[0].DistanceM, 8);
    }

    [Fact]
    public void SquadFilterKeepsOurRidersOnTheBoard()
    {
        RaceWatchFrame frame = Frame(
            watchSecond: 1,
            raceSecond: 8,
            paused: false,
            Rider(1006, 1, 400, 0, 11, 0.7, 0),
            Rider(1001, 2, 380, 20, 10, 0.8, 0),
            Rider(1002, 3, 350, 50, 9, 1.0, 0));

        InterpolatedWatchView squad = WatchMotionInterpolator.Project(
            frame,
            frame,
            1.0,
            new List<long> { 1001, 1002 });

        Assert.Equal(2, squad.Riders.Count);
        Assert.Contains(squad.Riders, rider => rider.RiderId == 1001);
        Assert.Contains(squad.Riders, rider => rider.RiderId == 1002);
        Assert.Equal(3, squad.Field.Count);
    }

    private static RaceWatchRiderFrame Rider(
        long riderId,
        int place,
        double distanceM,
        double gapM,
        double speedMps,
        double shelter,
        double gradient)
    {
        return new RaceWatchRiderFrame(
            new WorldEntityId(riderId),
            new WorldEntityId(701),
            $"rider-{riderId}",
            place,
            distanceM,
            gapM,
            speedMps,
            shelter,
            gradient);
    }

    private static RaceWatchFrame Frame(
        int watchSecond,
        int raceSecond,
        bool paused,
        params RaceWatchRiderFrame[] riders)
    {
        return new RaceWatchFrame(watchSecond, raceSecond, 5, paused, 1_000, riders, riders);
    }
}
