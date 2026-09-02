using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.SimRunner;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerSeasonsSoakTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string UaeOriginId = "organization.wt2026.uae";
    private const long GateSeed = 91234;

    [Fact]
    [Trait("Category", "Soak")]
    public void FiveSeasonsAdvanceDeterministicallyWithRetirementsAndNeoPros()
    {
        string output = RunSimRunnerSeasons(yearCount: 5);
        Assert.Contains("season=2026", output);
        Assert.Contains("season=2030", output);
        Assert.Contains("crashed=false", output);
        Assert.Contains("retired=", output);
        Assert.Contains("neo=", output);
        string[] seasonLines = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith("season=", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(5, seasonLines.Length);
        string firstChecksum = ChecksumFromSeasonLine(seasonLines[0]);
        string lastChecksum = ChecksumFromSeasonLine(seasonLines[^1]);
        Assert.NotEqual(firstChecksum, lastChecksum);
        Assert.True(RetiredCountFromSeasonLine(seasonLines[^1]) >= 1);
        Assert.True(NeoCountFromSeasonLine(seasonLines[^1]) >= 1);
    }

    private static string RunSimRunnerSeasons(int yearCount)
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = Program.Run(
            [
                "seasons",
                "--scenario",
                WtScenarioId,
                "--years",
                yearCount.ToString(CultureInfo.InvariantCulture),
                "--seed",
                GateSeed.ToString(CultureInfo.InvariantCulture),
                "--employer",
                UaeOriginId,
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);
        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));
        return output.ToString();
    }

    private static string ChecksumFromSeasonLine(string line)
    {
        const string marker = "checksum=";
        int start = line.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, line);
        int valueStart = start + marker.Length;
        int valueEnd = line.IndexOf(' ', valueStart);
        return valueEnd < 0 ? line[valueStart..] : line[valueStart..valueEnd];
    }

    private static int RetiredCountFromSeasonLine(string line) =>
        FieldIntFromSeasonLine(line, "retired=");

    private static int NeoCountFromSeasonLine(string line) =>
        FieldIntFromSeasonLine(line, "neo=");

    private static int FieldIntFromSeasonLine(string line, string marker)
    {
        int start = line.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, line);
        int valueStart = start + marker.Length;
        int valueEnd = line.IndexOf(' ', valueStart);
        string raw = valueEnd < 0 ? line[valueStart..] : line[valueStart..valueEnd];
        return int.Parse(raw, CultureInfo.InvariantCulture);
    }
}
