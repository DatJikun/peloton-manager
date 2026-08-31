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
    public void RaceNextEntersPrepWithNamedSeatsThenWatch()
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

        Assert.True(host.OpenWatch().Succeeded);
        Assert.Equal(GameState.RaceLive, host.State);
        Assert.NotNull(host.Watch);
        Assert.Contains(host.Watch!.Interpolated!.Riders, rider => rider.Name == "Dawid Rutka");
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
