using Peloton.Client.Godot;
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
    public void ClockFormatsFilmBudgetWithoutRateMultiplier()
    {
        Assert.Equal("0:45 / 2:00", WatchFilmDuration.Clock(45, 120));
        Assert.Equal("5 min", WatchFilmDuration.Label(WatchFilmDuration.DefaultSeconds));
        Assert.False(WatchFilmDuration.IsChoice(90));
    }
}
