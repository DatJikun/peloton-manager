using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class LookFormatTests
{
    [Fact]
    public void RaceCountdownPillUsesSingularDayLabel()
    {
        (string text, string? accent) = LookFormat.RaceCountdownPill(1);
        Assert.Equal("WYŚCIG ZA 1 DZIEŃ", text);
        Assert.Equal("1 DZIEŃ", accent);
    }

    [Fact]
    public void RaceCountdownPillUsesPluralDayLabel()
    {
        (string text, string? accent) = LookFormat.RaceCountdownPill(5);
        Assert.Equal("WYŚCIG ZA 5 DNI", text);
        Assert.Equal("5 DNI", accent);
    }

    [Fact]
    public void ManagerInitialsFallsBackWhenNameMissing()
    {
        Assert.Equal("MN", LookFormat.ManagerInitials(null));
        Assert.Equal("PK", LookFormat.ManagerInitials("Piotr Kowalczyk"));
    }
}
