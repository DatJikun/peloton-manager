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
    [InlineData("watch", "--scenario", RacePrototypeCommand.CanonicalScenarioId, "--seed", "1", "--rate", "0")]
    [InlineData("watch", "--scenario", RacePrototypeCommand.CanonicalScenarioId, "--seed", "1", "--rate", "100")]
    [InlineData("day", "--scenario", "scenario.peloton.skeleton", "--seed", "1", "--days", "1", "--watch-from-prep", "--rate", "0")]
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
    public void WatchCommandDefaultsToRateFiveAndMatchesOfficialRaceChecksum()
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
        Assert.Contains("rate=5", stdout, StringComparison.Ordinal);
        Assert.Contains("watchSecond=", stdout, StringComparison.Ordinal);
        Assert.Contains("simSecond=", stdout, StringComparison.Ordinal);
        Assert.Contains("paused=true", stdout, StringComparison.Ordinal);
        Assert.Contains("rider=", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("WPrime", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("spyNeutral=true", stdout, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));

        int watchSeconds = ParseKey(stdout, "watchSecond");
        int simulationSeconds = ParseKey(stdout, "simulationSeconds");
        Assert.True(watchSeconds < simulationSeconds);
    }

    [Fact]
    public void WatchRateOneAndTwentyDifferOnlyInSupervisingTime()
    {
        (int ExitCode, string Output, string Error) rateOne = RunWatch(rate: 1);
        (int ExitCode, string Output, string Error) rateTwenty = RunWatch(rate: 20);

        Assert.Equal(0, rateOne.ExitCode);
        Assert.Equal(0, rateTwenty.ExitCode);
        Assert.Equal(ParseTextKey(rateOne.Output, "winner"), ParseTextKey(rateTwenty.Output, "winner"));
        Assert.Equal(ParseTextKey(rateOne.Output, "checksum"), ParseTextKey(rateTwenty.Output, "checksum"));
        Assert.Equal("1006", ParseTextKey(rateOne.Output, "winner"));
        Assert.Equal(
            "D9F2FB98498D89E0595ACF89BA31C5A3CB87500C92CC3C5871088968BDE2ABD4", // D-054 PhysicsContractVersion 2 pace-setter
            ParseTextKey(rateOne.Output, "checksum"));
        Assert.True(ParseKey(rateOne.Output, "watchSecond") > ParseKey(rateTwenty.Output, "watchSecond"));
        Assert.Contains("paused=true", rateOne.Output, StringComparison.Ordinal);
        Assert.Contains("paused=true", rateTwenty.Output, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(rateOne.Error));
        Assert.True(string.IsNullOrWhiteSpace(rateTwenty.Error));
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
        Assert.Contains("Headless Watch clock", markdown, StringComparison.Ordinal);
        Assert.Contains("supervising clock", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("WPrime", markdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DayCommandPrintsHubAndStopsOnRaceDay()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = Program.Run(
            [
                "day",
                "--scenario",
                "scenario.peloton.skeleton",
                "--seed",
                "91234",
                "--days",
                "13",
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);

        string stdout = output.ToString();
        Assert.Equal(0, exitCode);
        Assert.Contains("stopped=RACE_DAY_PENDING", stdout, StringComparison.Ordinal);
        Assert.Contains("primaryAction=race-next", stdout, StringComparison.Ordinal);
        Assert.Contains("primaryLabel=Race next", stdout, StringComparison.Ordinal);
        Assert.Contains("day=12", stdout, StringComparison.Ordinal);
        Assert.Contains("raceDue=true", stdout, StringComparison.Ordinal);
        Assert.Contains("note=A race is due today.", stdout, StringComparison.Ordinal);
        Assert.Contains("employer=red", stdout, StringComparison.Ordinal);
        Assert.Contains("calendar=day=12 kind=race status=due title=Skeleton race", stdout, StringComparison.Ordinal);
        Assert.Contains("inboxCount=1", stdout, StringComparison.Ordinal);
        Assert.Contains("inbox=identity=calendar:", stdout, StringComparison.Ordinal);
        Assert.Contains("category=race-due", stdout, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));
    }

    [Fact]
    public void DayCommandFollowHubEntersRacePreparationOnRaceDay()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = Program.Run(
            [
                "day",
                "--scenario",
                "scenario.peloton.skeleton",
                "--seed",
                "91234",
                "--days",
                "13",
                "--follow-hub",
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);

        string stdout = output.ToString();
        Assert.Equal(0, exitCode);
        Assert.Contains("stopped=RACE_DAY_PENDING", stdout, StringComparison.Ordinal);
        Assert.Contains("state=RacePreparationFlow", stdout, StringComparison.Ordinal);
        Assert.Contains(
            "prep=title=Skeleton race objective=StageWin squad=5,8,11,14 planConfirmed=false canStart=false canSimulate=false",
            stdout,
            StringComparison.Ordinal);
        Assert.Contains("primaryAction=race-next", stdout, StringComparison.Ordinal);
        Assert.Contains("primaryLabel=Race next", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("winner=", stdout, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));
    }

    [Fact]
    public void DayCommandThroughResultsPrintsResultAndDebriefWithoutHubPrimaryAction()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = Program.Run(
            [
                "day",
                "--scenario",
                "scenario.peloton.skeleton",
                "--seed",
                "91234",
                "--days",
                "13",
                "--simulate-from-prep",
                "--through-results",
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);

        string stdout = output.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Equal(0, exitCode);
        Assert.Contains("state=RaceResultsFlow", stdout, StringComparison.Ordinal);
        Assert.Contains("winner=20", stdout, StringComparison.Ordinal); // D-054 positioning start grid
        Assert.Contains("result=title=Skeleton race", stdout, StringComparison.Ordinal);
        Assert.Contains("winnerLabel=rider.race-prototype.beta-leader", stdout, StringComparison.Ordinal);
        Assert.Contains("routeId=race-route.peloton.synthetic-proof-v0", stdout, StringComparison.Ordinal);
        Assert.Contains("finishOrder=rider.race-prototype.beta-leader", stdout, StringComparison.Ordinal);
        Assert.Contains("state=RaceDebriefFlow", stdout, StringComparison.Ordinal);
        Assert.Contains("debrief=objective=StageWin notes=Oficjalny zwycięzca: rider.race-prototype.beta-leader.", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("Widoczny rozjazd", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("LeaderPositionBand", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("ResourceEstimate", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("pasmo", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("zasoby", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("state=RaceLive", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("WPrime", stdout, StringComparison.OrdinalIgnoreCase);
        string afterResults = stdout[
            stdout.IndexOf("state=RaceResultsFlow", StringComparison.Ordinal)..];
        Assert.DoesNotContain("primaryAction=", afterResults, StringComparison.Ordinal);
        Assert.DoesNotContain("primaryLabel=", afterResults, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));
    }

    [Fact]
    public void DayCommandWatchFromPrepUsesSupervisingClockAndCommittedResult()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = Program.Run(
            [
                "day",
                "--scenario",
                "scenario.peloton.skeleton",
                "--seed",
                "91234",
                "--days",
                "13",
                "--watch-from-prep",
                "--rate",
                "5",
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);

        string stdout = output.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Equal(0, exitCode);
        Assert.Contains("state=RaceLive", stdout, StringComparison.Ordinal);
        Assert.Contains("rate=5", stdout, StringComparison.Ordinal);
        Assert.Contains("watchSecond=", stdout, StringComparison.Ordinal);
        Assert.Contains("simSecond=", stdout, StringComparison.Ordinal);
        Assert.Contains("paused=true", stdout, StringComparison.Ordinal);
        Assert.Contains("state=RaceResultsFlow", stdout, StringComparison.Ordinal);
        Assert.Contains("result=title=Skeleton race", stdout, StringComparison.Ordinal);
        Assert.Contains("winner=20", stdout, StringComparison.Ordinal); // D-054 positioning start grid
        Assert.Contains("winnerLabel=rider.race-prototype.beta-leader", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("state=RaceDebriefFlow", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("Widoczny rozjazd", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("WPrime", stdout, StringComparison.OrdinalIgnoreCase);
        string afterLive = stdout[stdout.IndexOf("state=RaceLive", StringComparison.Ordinal)..];
        Assert.DoesNotContain("primaryAction=", afterLive, StringComparison.Ordinal);
        Assert.DoesNotContain("primaryLabel=", afterLive, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));
    }

    [Fact]
    public void DayCommandWatchFromPrepRateOneAndTwentyShareOfficialResult()
    {
        (int ExitCode, string Output, string Error) rateOne = RunCareerWatch(rate: 1);
        (int ExitCode, string Output, string Error) rateTwenty = RunCareerWatch(rate: 20);

        Assert.Equal(0, rateOne.ExitCode);
        Assert.Equal(0, rateTwenty.ExitCode);
        Assert.Equal(ParseTextKey(rateOne.Output, "winner"), ParseTextKey(rateTwenty.Output, "winner"));
        Assert.Equal(ParseTextKey(rateOne.Output, "checksum"), ParseTextKey(rateTwenty.Output, "checksum"));
        Assert.Equal("20", ParseTextKey(rateOne.Output, "winner")); // D-054 positioning start grid
        Assert.Equal(
            ParseTextKey(rateOne.Output, "result"),
            ParseTextKey(rateTwenty.Output, "result"));
        Assert.True(ParseKey(rateOne.Output, "watchSecond") > ParseKey(rateTwenty.Output, "watchSecond"));
        Assert.Contains("paused=true", rateOne.Output, StringComparison.Ordinal);
        Assert.Contains("paused=true", rateTwenty.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("Widoczny rozjazd", rateOne.Output, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(rateOne.Error));
        Assert.True(string.IsNullOrWhiteSpace(rateTwenty.Error));
    }

    [Fact]
    public void DayCommandCanConfirmAndSimulateDirectlyFromPreparation()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = Program.Run(
            [
                "day",
                "--scenario",
                "scenario.peloton.skeleton",
                "--seed",
                "91234",
                "--days",
                "13",
                "--simulate-from-prep",
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);

        string stdout = output.ToString();
        Assert.Equal(0, exitCode);
        Assert.Contains("prep=title=Skeleton race", stdout, StringComparison.Ordinal);
        Assert.Contains("state=RaceResultsFlow", stdout, StringComparison.Ordinal);
        Assert.Contains("winner=20", stdout, StringComparison.Ordinal); // D-054 positioning start grid
        Assert.DoesNotContain("state=RaceLive", stdout, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));
    }

    [Fact]
    public void DayCommandThroughRacesContinuesPastFirstRaceDay()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = Program.Run(
            [
                "day",
                "--scenario",
                "scenario.peloton.skeleton",
                "--seed",
                "91234",
                "--days",
                "13",
                "--through-races",
                "true",
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);

        string stdout = output.ToString();
        Assert.Equal(0, exitCode);
        Assert.Contains("crashed=false", stdout, StringComparison.Ordinal);
        Assert.Contains("calendar=day=12 kind=race status=completed title=Skeleton race result=Winner 20", stdout, StringComparison.Ordinal);
        Assert.Contains("category=race-result", stdout, StringComparison.Ordinal);
        Assert.Contains("day=13", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("stopped=RACE_DAY_PENDING", stdout, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(error.ToString()));
    }

    [Fact]
    public void RaceGateSeedProducesExpectedWinnerAndChecksum()
    {
        RacePrototypeReport report = Execute(RacePrototypeCommand.CanonicalScenarioId, GateSeed);

        Assert.False(report.Crashed);
        Assert.Equal("1006", report.Winner); // D-054 PhysicsContractVersion 2 pace-setter
        Assert.Equal("D9F2FB98498D89E0595ACF89BA31C5A3CB87500C92CC3C5871088968BDE2ABD4", report.Checksum);
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

    private static string ParseTextKey(string stdout, string key)
    {
        string prefix = key + "=";
        string? line = stdout
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .FirstOrDefault(item => item.StartsWith(prefix, StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(line));
        return line[prefix.Length..];
    }

    private static (int ExitCode, string Output, string Error) RunWatch(int rate)
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = Program.Run(
            [
                "watch",
                "--scenario",
                RacePrototypeCommand.CanonicalScenarioId,
                "--seed",
                GateSeed.ToString(CultureInfo.InvariantCulture),
                "--rate",
                rate.ToString(CultureInfo.InvariantCulture),
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);
        return (exitCode, output.ToString(), error.ToString());
    }

    private static (int ExitCode, string Output, string Error) RunCareerWatch(int rate)
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = Program.Run(
            [
                "day",
                "--scenario",
                "scenario.peloton.skeleton",
                "--seed",
                "91234",
                "--days",
                "13",
                "--watch-from-prep",
                "--rate",
                rate.ToString(CultureInfo.InvariantCulture),
                "--content-root",
                TestApplication.ContentRoot,
            ],
            output,
            error);
        return (exitCode, output.ToString(), error.ToString());
    }
}
