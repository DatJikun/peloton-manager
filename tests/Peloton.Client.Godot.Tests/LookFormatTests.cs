using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class LookFormatTests
{
    [Fact]
    public void RaceCountdownPillUsesSingularDayLabel()
    {
        (string prefix, string? accent) = LookFormat.RaceCountdownPill(1);
        Assert.Equal("WYŚCIG ZA ", prefix);
        Assert.Equal("1 DZIEŃ", accent);
    }

    [Fact]
    public void RaceCountdownPillUsesPluralDayLabel()
    {
        (string prefix, string? accent) = LookFormat.RaceCountdownPill(5);
        Assert.Equal("WYŚCIG ZA ", prefix);
        Assert.Equal("5 DNI", accent);
    }

    [Fact]
    public void YearPillSplitsPrefixAndAccent()
    {
        (string prefix, string accent) = LookFormat.YearPillParts(2026);
        Assert.Equal("ROK ", prefix);
        Assert.Equal("2026", accent);
    }

    [Fact]
    public void DateChipLabelUsesWeekdayAndDdMm()
    {
        Assert.Equal("CZW 01.01", LookFormat.DateChipLabel(0));
    }

    [Fact]
    public void ManagerInitialsFallsBackWhenNameMissing()
    {
        Assert.Equal("MN", LookFormat.ManagerInitials(null));
        Assert.Equal("PK", LookFormat.ManagerInitials("Piotr Kowalczyk"));
    }
}
