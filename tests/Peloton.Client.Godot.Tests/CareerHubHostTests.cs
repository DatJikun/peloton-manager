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

    [Fact]
    public void HubShowsCalendarInboxAndAdvanceDayWithoutKpiDashboard()
    {
        using TemporaryDirectory temp = new();
        CareerHubHost host = CreateHost(temp.Path);

        Assert.True(host.Open(GateSeed).Succeeded);
        Assert.Equal(GameState.Management, host.State);
        Assert.Equal("Beskid–Vetter", host.Day!.EmployerName);
        Assert.Equal("Adam Wroński", host.Day.ManagerName);
        Assert.Equal(HubPrimaryActionIds.AdvanceDay, host.Day.PrimaryAction);
        Assert.Equal(3, host.Calendar.Count);
        Assert.Empty(host.Inbox);
        Assert.Null(host.Watch);

        Assert.True(host.AdvanceDay().Succeeded);
        Assert.Equal(1, host.Day!.DayNumber);
        Assert.Equal(3, host.Day.DaysUntilNextRace);
        Assert.DoesNotContain("KPI", host.Day.TodayNotes[0], System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RaceNextEntersPrepWithNamedSeatsThenSimulatesToResults()
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
        Assert.Equal(SkeletonCalendar.OpeningClassic, prep.Title);
        Assert.Equal(4, prep.Seats.Count);
        Assert.Contains(prep.Seats, seat => seat.Role == SquadRoles.Leader && seat.Name == "Piotr Kowalczyk");
        Assert.Contains(prep.Seats, seat => seat.Role == SquadRoles.Card && !string.IsNullOrWhiteSpace(seat.Why));

        WorldEntityId rutka = prep.Seats.Single(seat => seat.Name == "Dawid Rutka").RiderId;
        Assert.True(host.AssignRole(rutka, SquadRoles.Leader).Succeeded);
        Assert.False(host.Preparation!.PlanConfirmed);
        Assert.Equal(SquadRoles.Leader, host.Preparation.Seats.Single(seat => seat.Name == "Dawid Rutka").Role);
        Assert.Equal(SquadRoles.Worker, host.Preparation.Seats.Single(seat => seat.Name == "Piotr Kowalczyk").Role);
        Assert.Equal("PREP_ROLES_INCOMPLETE", host.ConfirmPreparation().ReasonCode);
        Assert.True(host.AssignRole(
            host.Preparation.Seats.Single(seat => seat.Name == "Piotr Kowalczyk").RiderId,
            SquadRoles.Card).Succeeded);

        Assert.False(host.Settings.WatchFilmEnabled);
        Assert.True(host.RunRace().Succeeded);
        Assert.Equal(GameState.RaceResultsFlow, host.State);
        Assert.Null(host.Watch);
        RaceResultProjection result = Assert.IsType<RaceResultProjection>(host.Result);
        Assert.Equal("Opening Classic", result.Title);
        Assert.Equal("Marco Anconi", result.WinnerLabel);
        Assert.Contains(result.Headlines, line => line.Contains("Marco Anconi", StringComparison.Ordinal));
        Assert.Contains(result.Headlines, line => line.Contains("Dawid Rutka", StringComparison.Ordinal));
        Assert.Contains(result.Headlines, line => line.Contains("Cel StageWin: nie tym razem.", StringComparison.Ordinal));
        Assert.All(result.Headlines, line => Assert.DoesNotContain("WPrime", line, StringComparison.OrdinalIgnoreCase));

        Assert.True(host.ContinueOutcome().Succeeded);
        Assert.Equal(GameState.RaceDebriefFlow, host.State);
        Assert.Contains(host.Debrief!.Notes, note => note.Contains("Marco Anconi", StringComparison.Ordinal));
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
        Assert.Contains(reloaded.Watch!.Interpolated!.Riders, rider => rider.Name == "Piotr Kowalczyk");
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
