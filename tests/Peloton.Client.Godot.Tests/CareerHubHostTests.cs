using System;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class CareerHubHostTests
{
    private const long GateSeed = 91234;
    private const string BetaLeaderOriginId = "rider.race-prototype.beta-leader";

    [Fact]
    public void HubShowsCalendarInboxAndAdvanceDayWithoutKpiDashboard()
    {
        using TemporaryDirectory temp = new();
        CareerHubHost host = CreateHost(temp.Path);

        Assert.True(host.Open(GateSeed).Succeeded);
        Assert.Equal(GameState.Management, host.State);
        Assert.Equal("red", host.Day!.EmployerName);
        Assert.Equal("Skeleton Manager", host.Day.ManagerName);
        Assert.Equal(HubPrimaryActionIds.AdvanceDay, host.Day.PrimaryAction);
        Assert.Single(host.Calendar);
        Assert.Empty(host.Inbox);
        Assert.Null(host.Watch);

        Assert.True(host.AdvanceDay().Succeeded);
        Assert.Equal(1, host.Day!.DayNumber);
        Assert.Equal(11, host.Day.DaysUntilNextRace);
        Assert.DoesNotContain("KPI", host.Day.TodayNotes[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RaceNextEntersPrepThenSimulatesToResults()
    {
        using TemporaryDirectory temp = new();
        CareerHubHost host = CreateHost(temp.Path);
        Assert.True(host.Open(GateSeed).Succeeded);
        AdvanceToRaceDue(host);

        Assert.Equal(HubPrimaryActionIds.RaceNext, host.Day!.PrimaryAction);
        Assert.Equal("race-due", Assert.Single(host.Inbox).Category);
        Assert.True(host.FollowPrimary().Succeeded);
        Assert.Equal(GameState.RacePreparationFlow, host.State);
        RacePreparationProjection prep = Assert.IsType<RacePreparationProjection>(host.Preparation);
        Assert.Equal("Skeleton race", prep.Title);
        Assert.Equal(4, prep.Squad.Count);
        Assert.Contains(prep.Squad, rider => host.RiderDisplayName(rider) == "Alpha Leader");

        Assert.False(host.Settings.WatchFilmEnabled);
        Assert.True(host.RunRace().Succeeded);
        Assert.Equal(GameState.RaceResultsFlow, host.State);
        Assert.Null(host.Watch);
        RaceResultProjection result = Assert.IsType<RaceResultProjection>(host.Result);
        Assert.Equal("Skeleton race", result.Title);
        Assert.Equal(BetaLeaderOriginId, result.WinnerLabel);
        Assert.Equal("Beta Leader", host.RiderDisplayName(result.WinnerId));
        Assert.Contains(
            result.FinishOrder,
            row => host.RiderDisplayName(row.RiderId) == "Alpha Card" && row.OrganizationName == "red");
        Assert.Equal("blue", result.FinishOrder[0].OrganizationName);
        Assert.Equal(12, host.VisibleResultTable.Count);

        OrganizationNameProjection red = host.ResultTeams.Single(team => team.Name == "red");
        host.SetResultTeamFilter(red.Id);
        Assert.Equal(red.Id, host.ResultTeamFilter);
        Assert.Equal(4, host.VisibleResultTable.Count);
        Assert.All(host.VisibleResultTable, row => Assert.Equal("red", row.OrganizationName));
        Assert.DoesNotContain(host.VisibleResultTable, row => row.Place == 1);
        Assert.Contains(host.VisibleResultTable, row => host.RiderDisplayName(row.RiderId) == "Alpha Card");
        host.SetResultTeamFilter(null);
        Assert.Null(host.ResultTeamFilter);
        Assert.Equal(12, host.VisibleResultTable.Count);

        Assert.True(host.ContinueOutcome().Succeeded);
        Assert.Equal(GameState.RaceDebriefFlow, host.State);
        Assert.Contains(host.Debrief!.Notes, note => note.Contains(BetaLeaderOriginId, StringComparison.Ordinal));
        Assert.True(host.ContinueOutcome().Succeeded);
        Assert.Equal(GameState.Management, host.State);
        Assert.Null(host.Result);
    }

    [Fact]
    public void WatchFilmSettingIsOffByDefaultAndOptInOpensWatch()
    {
        using TemporaryDirectory temp = new();
        CareerHubHost host = CreateHost(temp.Path);
        Assert.True(host.Open(GateSeed).Succeeded);
        Assert.False(host.Settings.WatchFilmEnabled);
        host.SetWatchFilmEnabled(true);
        Assert.True(host.Settings.WatchFilmEnabled);

        CareerHubHost reloaded = CreateHost(temp.Path);
        Assert.True(reloaded.Settings.WatchFilmEnabled);
        Assert.True(reloaded.Open(GateSeed).Succeeded);

        AdvanceToRaceDue(reloaded);
        Assert.True(reloaded.FollowPrimary().Succeeded);
        Assert.True(reloaded.RunRace().Succeeded);
        Assert.Equal(GameState.RaceLive, reloaded.State);
        Assert.NotNull(reloaded.Watch);
        Assert.Contains(reloaded.Watch!.Interpolated!.Riders, rider => rider.Name == "Alpha Leader");
        Assert.All(reloaded.Watch.Interpolated.Riders, rider => Assert.False(string.IsNullOrWhiteSpace(rider.Name)));
    }

    private static void AdvanceToRaceDue(CareerHubHost host)
    {
        for (int day = 0; day < 32 && host.Day is { RaceDueToday: false }; day++)
        {
            Assert.True(host.AdvanceDay().Succeeded);
        }

        Assert.True(host.Day!.RaceDueToday);
    }

    private static CareerHubHost CreateHost(string directory)
    {
        GameApplication application = new(
            new JsonScenarioCatalog(ContentRoot()),
            new JsonRacePrototypeCatalog(ContentRoot()),
            new SqliteWorldSaveStore(),
            new PrototypeRaceEngine());
        return new CareerHubHost(application, Path.Combine(directory, "pre-race.peloton"));
    }

    private static string ContentRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "PelotonManager.sln")))
            {
                return Path.Combine(current.FullName, "content");
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
