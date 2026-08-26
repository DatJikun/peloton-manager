using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.SimRunner;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class SimRunnerContractTests
{
    private const long GateSeed = 91234;

    [Fact]
    public void RaceCommandPrintsRequiredKeysAndIsSpyNeutral()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = Program.Run(
            [
                "race",
                "--scenario",
                RacePrototypeCommand.CanonicalScenarioId,
                "--seed",
                GateSeed.ToString(CultureInfo.InvariantCulture),
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);

        string stdout = output.ToString();
        Assert.Equal(0, exitCode);
        Assert.Contains("winner=", stdout, StringComparison.Ordinal);
        Assert.Contains("checksum=", stdout, StringComparison.Ordinal);
        Assert.Contains("decisionCount=", stdout, StringComparison.Ordinal);
        Assert.Contains("spyNeutral=true", stdout, StringComparison.Ordinal);
        Assert.Contains("crashed=false", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("winner=\n", stdout.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));
    }

    [Fact]
    public void GateAliasResolvesToTheSameOfficialResultAsTheCanonicalId()
    {
        RacePrototypeReport canonical = Execute(RacePrototypeCommand.CanonicalScenarioId, GateSeed);
        RacePrototypeReport alias = Execute(RacePrototypeCommand.GateScenarioAlias, GateSeed);

        Assert.False(canonical.Crashed);
        Assert.Equal(canonical.Winner, alias.Winner);
        Assert.Equal(canonical.Checksum, alias.Checksum);
        Assert.Equal(canonical.DecisionCount, alias.DecisionCount);
        Assert.True(canonical.SpyNeutral);
        Assert.True(alias.SpyNeutral);
        Assert.True(canonical.DecisionCount > 0);
    }

    [Fact]
    public void SameSeedProducesIdenticalWinnerChecksumAndDecisionCount()
    {
        RacePrototypeReport first = Execute(RacePrototypeCommand.CanonicalScenarioId, GateSeed);
        RacePrototypeReport second = Execute(RacePrototypeCommand.CanonicalScenarioId, GateSeed);

        Assert.Equal(first.Winner, second.Winner);
        Assert.Equal(first.Checksum, second.Checksum);
        Assert.Equal(first.DecisionCount, second.DecisionCount);
        Assert.False(first.Crashed);
        Assert.False(second.Crashed);
    }

    [Fact]
    public void TraceFlagsExportJsonAndMarkdownWithoutChangingOfficialResult()
    {
        using TemporaryDirectory temp = new();
        string jsonPath = Path.Combine(temp.Path, "spy.json");
        string markdownPath = Path.Combine(temp.Path, "spy.md");
        RacePrototypeReport baseline = Execute(RacePrototypeCommand.CanonicalScenarioId, GateSeed);

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = Program.Run(
            [
                "race",
                "--scenario",
                RacePrototypeCommand.CanonicalScenarioId,
                "--seed",
                GateSeed.ToString(CultureInfo.InvariantCulture),
                "--content-root",
                TestApplication.ContentRoot,
                "--trace-json",
                jsonPath,
                "--trace-markdown",
                markdownPath,
            ],
            output,
            error);

        string stdout = output.ToString();
        Assert.Equal(0, exitCode);
        Assert.Contains($"checksum={baseline.Checksum}", stdout, StringComparison.Ordinal);
        Assert.Contains($"winner={baseline.Winner}", stdout, StringComparison.Ordinal);
        Assert.Contains($"decisionCount={baseline.DecisionCount}", stdout, StringComparison.Ordinal);
        Assert.True(File.Exists(jsonPath));
        Assert.True(File.Exists(markdownPath));
        Assert.Contains("\"domain\": \"Race\"", File.ReadAllText(jsonPath), StringComparison.Ordinal);
        Assert.Contains("## Race decision", File.ReadAllText(markdownPath), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData]
    [InlineData("walk")]
    [InlineData("race")]
    [InlineData("race", "--scenario")]
    [InlineData("race", "--scenario", RacePrototypeCommand.CanonicalScenarioId)]
    [InlineData("race", "--scenario", RacePrototypeCommand.CanonicalScenarioId, "--seed", "not-a-number")]
    [InlineData("race", "--scenario", RacePrototypeCommand.CanonicalScenarioId, "--seed", "1", "--seed", "2")]
    public void MalformedRaceOptionsReturnUsageExitCode(params string[] args)
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = Program.Run(args, output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage: peloton-sim", error.ToString(), StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(output.ToString()));
    }

    [Fact]
    public void UnknownRaceScenarioReportsCrashWithoutThrowing()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = Program.Run(
            [
                "race",
                "--scenario",
                "race-scenario.does-not-exist",
                "--seed",
                "1",
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);

        string stdout = output.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("crashed=true", stdout, StringComparison.Ordinal);
        Assert.Contains("winner=", stdout, StringComparison.Ordinal);
        Assert.Contains("checksum=", stdout, StringComparison.Ordinal);
        Assert.Contains("decisionCount=0", stdout, StringComparison.Ordinal);
        Assert.Contains("reason=SCENARIO_NOT_FOUND", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WatchCommandCompressesQuietTimeAndMatchesOfficialRaceChecksum()
    {
        RacePrototypeReport official = Execute(RacePrototypeCommand.CanonicalScenarioId, GateSeed);
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = Program.Run(
            [
                "watch",
                "--scenario",
                RacePrototypeCommand.CanonicalScenarioId,
                "--seed",
                GateSeed.ToString(CultureInfo.InvariantCulture),
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);

        string stdout = output.ToString();
        Assert.Equal(0, exitCode);
        Assert.Contains($"checksum={official.Checksum}", stdout, StringComparison.Ordinal);
        Assert.Contains($"winner={official.Winner}", stdout, StringComparison.Ordinal);
        Assert.Contains("kind=start", stdout, StringComparison.Ordinal);
        Assert.Contains("kind=decision", stdout, StringComparison.Ordinal);
        Assert.Contains("kind=finish", stdout, StringComparison.Ordinal);
        Assert.Contains("spyNeutral=true", stdout, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));

        int watchBeats = ParseKey(stdout, "watchBeats");
        int simulationSeconds = ParseKey(stdout, "simulationSeconds");
        Assert.True(watchBeats < simulationSeconds);
        Assert.True(watchBeats >= 3);
    }

    [Fact]
    public void WatchMarkdownExportSkipsQuietPhysics()
    {
        using TemporaryDirectory temp = new();
        string markdownPath = Path.Combine(temp.Path, "watch.md");
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = Program.Run(
            [
                "watch",
                "--scenario",
                RacePrototypeCommand.GateScenarioAlias,
                "--seed",
                GateSeed.ToString(CultureInfo.InvariantCulture),
                "--content-root",
                TestApplication.ContentRoot,
                "--trace-markdown",
                markdownPath,
            ],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(markdownPath));
        string markdown = File.ReadAllText(markdownPath);
        Assert.Contains("Race decision digest", markdown, StringComparison.Ordinal);
        Assert.Contains("index of pauses", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("WPrime", markdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExistingCareerRunCommandStillParsesAndRejectsMalformedYears()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = Program.Run(
            ["run", "--scenario", "scenario.peloton.skeleton", "--years", "0", "--seed", "1"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("--years must be a positive integer.", error.ToString(), StringComparison.Ordinal);
    }

    private static RacePrototypeReport Execute(string scenarioId, long seed)
    {
        return RacePrototypeCommand.Execute(new RacePrototypeOptions(
            scenarioId,
            seed,
            TestApplication.ContentRoot,
            TraceJsonPath: null,
            TraceMarkdownPath: null));
    }

    private static int ParseKey(string stdout, string key)
    {
        string prefix = key + "=";
        string? line = stdout
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .FirstOrDefault(item => item.StartsWith(prefix, StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(line));
        return int.Parse(line.AsSpan(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}
