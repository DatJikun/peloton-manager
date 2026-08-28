using Peloton.Client.Godot;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class WatchObservationTextTests
{
    [Fact]
    public void SpeedIsCalculatedInKmhFromPhysicsMetresPerSecond()
    {
        Assert.Equal(39.6, WatchObservationText.SpeedKmh(11.0), 8);
        Assert.Equal("40 km/h", WatchObservationText.Speed(11.0));
        Assert.Equal("27 km/h", WatchObservationText.Speed(7.5));
    }

    [Fact]
    public void RadioAndTerrainDistinguishFlatFromClimbWithoutPhysiology()
    {
        string flat = WatchObservationText.Radio(11.0, 0.7, 0.0, 0.0);
        string climb = WatchObservationText.Radio(7.5, 1.0, 0.05, 8.0);
        Assert.Equal("płasko", WatchObservationText.Terrain(0.0));
        Assert.Equal("podjazd 5%", WatchObservationText.Terrain(0.05));
        Assert.Contains("kole", flat, System.StringComparison.Ordinal);
        Assert.Contains("górze", climb, System.StringComparison.Ordinal);
        Assert.DoesNotContain("WPrime", flat, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WPrime", climb, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal("DS (chce: Pościg)", WatchObservationText.DsAction("CommitSupport"));
        Assert.Equal("0:00", WatchObservationText.GapClock(0.0, 11.0));
        Assert.Equal("+0:04", WatchObservationText.GapClock(44.0, 11.0));
        Assert.Equal("Alpha Leader", WatchObservationText.DisplayName("alpha-leader"));
    }
}
