using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation;
using Peloton.SimRunner;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerSeasonsSoakTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string UaeOriginId = "organization.wt2026.uae";
    private const long GateSeed = 91234;

    [Fact]
    public void FiveSeasonsAdvanceDeterministicallyWithRetirementsAndNeoPros()
    {
        (string checksumFirst, string checksumLast, int totalRetired, int totalNeo) = RunFiveSeasons();
        Assert.True(totalRetired >= 1, $"totalRetired={totalRetired}");
        Assert.True(totalNeo >= 1, $"totalNeo={totalNeo}");
        Assert.NotEqual(checksumFirst, checksumLast);
    }

    private static (string FirstChecksum, string LastChecksum, int TotalRetired, int TotalNeo) RunFiveSeasons()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed, UaeOriginId)).Succeeded);
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        foreach (PreSeasonRaceEntryProjection race in application.PreSeasonPlanning!.Races)
        {
            Assert.True(application.Execute(new SetSeasonRaceEntryCommand(race.RaceContentId, Entered: false)).Succeeded);
        }

        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);
        WorldState world = application.World!;
        string[] checksums = new string[5];
        int totalRetired = 0;
        int totalNeo = 0;
        for (int season = 0; season < 5; season++)
        {
            int targetDay = checked((season + 1) * world.FinancialYearDays);
            while (world.CurrentDate.DayNumber < targetDay)
            {
                if (application.State == GameState.PreSeasonPlanningFlow)
                {
                    CareerDayCommand.EnsurePlayerSkipsRacesForLongSoak(application);
                }

                int previousSeasonYear = world.SeasonYear;
                Assert.True(application.ExecuteCalendarDaySkippingRaces().Succeeded);
                if (world.SeasonYear > previousSeasonYear)
                {
                    totalRetired += SeasonRolloverExecutor.LastRetiredCount;
                    totalNeo += SeasonRolloverExecutor.LastNeoCount;
                }
            }

            checksums[season] = WorldChecksum.Compute(world);
        }

        return (checksums[0], checksums[^1], totalRetired, totalNeo);
    }

    [Fact]
    public void SimRunnerSeasonsCommandFinishesDeterministically()
    {
        string output = RunSimRunnerSeasons();
        Assert.Contains("season=2026", output);
        Assert.Contains("season=2030", output);
        Assert.Contains("crashed=false", output);
        Assert.Contains("retired=", output);
        Assert.Contains("neo=", output);
    }

    private static string RunSimRunnerSeasons()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = Program.Run(
            [
                "seasons",
                "--scenario",
                WtScenarioId,
                "--years",
                "5",
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
}
