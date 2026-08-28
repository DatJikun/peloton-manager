using Peloton.Client.Godot;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class WatchFilmDurationTests
{
    [Theory]
    [InlineData(30, 30)]
    [InlineData(60, 15)]
    [InlineData(120, 8)]
    [InlineData(180, 5)]
    [InlineData(300, 3)]
    public void PrototypeLengthMapsToExpectedRates(int filmSeconds, int expectedRate)
    {
        Assert.Equal(expectedRate, WatchFilmDuration.RateFor(5400.0, filmSeconds));
        Assert.Equal(900, WatchFilmDuration.EstimatePhysicsSeconds(5400.0));
    }

    [Fact]
    public void DefaultFiveMinuteFilmIsAboutFiveMinutesNotFifteenSeconds()
    {
        RaceWatchCourse course = new(
            5400.0,
            new[]
            {
                new RaceWatchCourseSegment("race-segment.peloton.flat-open", 1800.0, 0.0, 6.0, 2.0, 0.0),
                new RaceWatchCourseSegment("race-segment.peloton.sustained-climb", 1800.0, 0.05, 5.0, 1.0, 20.0),
                new RaceWatchCourseSegment("race-segment.peloton.exposed-crosswind", 1800.0, 0.0, 3.2, 10.0, 90.0),
            });
        Assert.Equal(3, WatchFilmDuration.RateFor(course, WatchFilmDuration.DefaultSeconds));
        int film = WatchFilmDuration.EstimateFilmSeconds(course, WatchFilmDuration.DefaultSeconds);
        Assert.Equal(300, film);
        Assert.Equal("5:00 / 5:00", WatchFilmDuration.Clock(film, film));
        Assert.Equal("5 min", WatchFilmDuration.Label(WatchFilmDuration.DefaultSeconds));
        Assert.False(WatchFilmDuration.IsChoice(90));
    }
}
