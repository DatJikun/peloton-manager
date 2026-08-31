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

public sealed class CareerShellHostTests
{
    private const long GateSeed = 91234;

    [Fact]
    public void OpenSkeletonStaysInManagementWithHubCalendarAndPeople()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);

        Assert.True(host.OpenSkeleton(GateSeed).Succeeded);
        Assert.Equal(GameState.Management, host.State);
        CareerDayProjection day = Assert.IsType<CareerDayProjection>(host.Day);
        Assert.Equal(0, day.DayNumber);
        Assert.Equal(HubPrimaryActionIds.AdvanceDay, day.PrimaryAction);
        Assert.Equal("Advance Day", day.PrimaryLabel);
        Assert.Equal("Adam Wroński", day.ManagerName);
        Assert.Equal("Beskid–Vetter", day.EmployerName);
        Assert.Contains(host.Calendar, entry => entry.Title == SkeletonCalendar.OpeningClassic);
        Assert.Equal(3, host.Calendar.Count);
        Assert.DoesNotContain(host.People, person => person.Name.Contains("OVR", StringComparison.Ordinal));
        Assert.NotEmpty(host.Organizations);
        Assert.False(host.Settings.WatchFilmEnabled);
    }

    [Fact]
    public void FollowPrimaryAdvancesDayThenEntersPreparationOnRaceDue()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton().Succeeded);

        while (host.Day is { RaceDueToday: false })
        {
            Assert.True(host.FollowPrimary().Succeeded);
            Assert.Equal(GameState.Management, host.State);
        }

        CareerDayProjection due = Assert.IsType<CareerDayProjection>(host.Day);
        Assert.Equal(HubPrimaryActionIds.RaceNext, due.PrimaryAction);
        Assert.Equal("Race next", due.PrimaryLabel);
        Assert.Contains(host.Inbox, item => item.Category == "race-due");
        Assert.Equal(
            "INBOX_SOURCE_CANNOT_BE_DISMISSED",
            host.ArchiveInbox(host.Inbox[0].Identity).ReasonCode);

        Assert.True(host.FollowPrimary().Succeeded);
        Assert.Equal(GameState.RacePreparationFlow, host.State);
        Assert.Null(host.Day);
        RacePreparationProjection prep = Assert.IsType<RacePreparationProjection>(host.Preparation);
        Assert.Equal(SkeletonCalendar.OpeningClassic, prep.Title);
        Assert.Equal(4, prep.Seats.Count);
        Assert.Contains(prep.Seats, seat => seat.Role == SquadRoles.Leader && seat.Name == "Piotr Kowalczyk");
    }

    [Fact]
    public void RaceNextSimulatesToResultsTableByDefault()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton().Succeeded);
        AdvanceToRaceDue(host);

        Assert.True(host.FollowPrimary().Succeeded);
        Assert.False(host.Settings.WatchFilmEnabled);
        Assert.True(host.RunRace().Succeeded);
        Assert.Equal(GameState.RaceResultsFlow, host.State);
        Assert.Null(host.Watch);
        RaceResultProjection result = Assert.IsType<RaceResultProjection>(host.Result);
        Assert.Equal("Opening Classic", result.Title);
        Assert.Equal("Marco Anconi", result.WinnerLabel);
        Assert.Equal(3, result.Teams.Count);
        Assert.Contains(result.FinishOrder, row => row.Label == "Dawid Rutka" && row.TeamName == "Beskid–Vetter");
        Assert.Equal(12, host.VisibleResultTable.Count);

        RaceResultTeam beskid = result.Teams.Single(team => team.Name == "Beskid–Vetter");
        host.SetResultTeamFilter(beskid.Id);
        Assert.Equal(4, host.VisibleResultTable.Count);
        Assert.All(host.VisibleResultTable, row => Assert.Equal("Beskid–Vetter", row.TeamName));
        host.SetResultTeamFilter(null);
        Assert.Equal(12, host.VisibleResultTable.Count);

        Assert.True(host.ContinueOutcome().Succeeded);
        Assert.Equal(GameState.RaceDebriefFlow, host.State);
        Assert.True(host.ContinueOutcome().Succeeded);
        Assert.Equal(GameState.Management, host.State);
        Assert.Contains(host.Calendar, entry => entry.OfficialResult is not null);
    }

    [Fact]
    public void WatchFilmSettingIsOffByDefaultAndOptInOpensWatch()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton().Succeeded);
        Assert.False(host.Settings.WatchFilmEnabled);
        host.SetWatchFilmEnabled(true);
        Assert.True(host.Settings.WatchFilmEnabled);

        CareerShellHost reloaded = CreateHost(temp.Path);
        Assert.True(reloaded.Settings.WatchFilmEnabled);
        Assert.True(reloaded.OpenSkeleton().Succeeded);

        AdvanceToRaceDue(reloaded);
        Assert.True(reloaded.FollowPrimary().Succeeded);
        Assert.True(reloaded.RunRace().Succeeded);
        Assert.Equal(GameState.RaceLive, reloaded.State);
        Assert.NotNull(reloaded.Watch);
        Assert.Contains(reloaded.Watch!.Interpolated!.Riders, rider => rider.Name == "Piotr Kowalczyk");
    }

    [Fact]
    public void SaveAndLoadRoundTripManagementDay()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton().Succeeded);
        Assert.True(host.FollowPrimary().Succeeded);
        int dayNumber = host.Day!.DayNumber;
        Assert.True(host.Save().Succeeded);

        CareerShellHost loaded = CreateHost(temp.Path);
        Assert.True(loaded.Load().Succeeded);
        Assert.Equal(GameState.Management, loaded.State);
        Assert.Equal(dayNumber, loaded.Day!.DayNumber);
        Assert.Equal(host.Day.EmployerName, loaded.Day.EmployerName);
    }

    private static void AdvanceToRaceDue(CareerShellHost host)
    {
        for (int day = 0; day < 32 && host.Day is { RaceDueToday: false }; day++)
        {
            Assert.True(host.FollowPrimary().Succeeded);
        }

        Assert.True(host.Day!.RaceDueToday);
    }

    private static CareerShellHost CreateHost(string directory)
    {
        GameApplication application = new(
            new JsonScenarioCatalog(ContentRoot()),
            new JsonRacePrototypeCatalog(ContentRoot()),
            new SqliteWorldSaveStore(),
            new PrototypeRaceEngine());
        return new CareerShellHost(
            application,
            Path.Combine(directory, "career.peloton"),
            Path.Combine(directory, "pre-race.peloton"));
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
