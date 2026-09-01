using System;
using System.IO;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class CareerShellHostTests
{

    [Fact]
    public void OpenSkeletonStaysInManagementWithHubCalendarAndPeople()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);

        Assert.True(host.OpenSkeleton().Succeeded);
        Assert.Equal(GameState.Management, host.State);
        CareerDayProjection day = Assert.IsType<CareerDayProjection>(host.Day);
        Assert.Equal(0, day.DayNumber);
        Assert.Equal(HubPrimaryActionIds.AdvanceDay, day.PrimaryAction);
        Assert.Equal("Advance Day", day.PrimaryLabel);
        Assert.False(string.IsNullOrWhiteSpace(day.ManagerName));
        Assert.False(string.IsNullOrWhiteSpace(day.EmployerName));
        Assert.Contains(host.Calendar, entry => entry.Title == "Skeleton race");
        Assert.NotEmpty(host.People);
        Assert.DoesNotContain(host.People, person => person.Name.Contains("OVR", StringComparison.Ordinal));
        Assert.NotEmpty(host.Organizations);
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
        Assert.NotNull(host.Preparation);
    }

    [Fact]
    public void WatchFromRaceNextMatchesPrototypeGoldenAndReturnsToManagement()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton().Succeeded);
        while (host.Day is { RaceDueToday: false })
        {
            Assert.True(host.FollowPrimary().Succeeded);
        }

        Assert.True(host.FollowPrimary().Succeeded);
        WatchRaceHost watch = host.CreateWatchHost();
        Assert.Equal(GameState.RacePreparationFlow, watch.State);
        Assert.True(watch.CancelPreparation().Succeeded);
        Assert.Equal(GameState.Management, watch.State);
        Assert.Equal(HubPrimaryActionIds.RaceNext, host.Day!.PrimaryAction);

        Assert.True(host.FollowPrimary().Succeeded);
        watch = host.CreateWatchHost();
        Assert.True(watch.ConfirmPreparation().Succeeded);
        Assert.True(watch.StartWatch().Succeeded);
        CompleteWatch(watch);
        Assert.Equal(GameState.RaceResultsFlow, watch.State);
        Assert.Equal("rider.race-prototype.beta-leader", watch.Result!.WinnerLabel);
        Assert.False(string.IsNullOrWhiteSpace(watch.LastChecksum));
        Assert.True(watch.AcknowledgeResults().Succeeded);
        Assert.True(watch.CompleteDebrief().Succeeded);
        Assert.Equal(GameState.Management, host.State);
        Assert.Equal(1, host.Day!.RaceCount);
        Assert.Contains(host.Calendar, entry => entry.OfficialResult is not null);
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

    private static void CompleteWatch(WatchRaceHost watch)
    {
        for (int barrier = 0; barrier < 100_000 && watch.State == GameState.RaceLive; barrier++)
        {
            if (watch.PendingDecision is not null)
            {
                Assert.True(watch.RespondDelegatedDefault().Succeeded);
                continue;
            }

            Assert.True(watch.Tick(1.0).Succeeded);
        }

        Assert.Equal(GameState.RaceResultsFlow, watch.State);
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
