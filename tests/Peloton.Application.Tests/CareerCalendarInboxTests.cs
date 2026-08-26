using System;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerCalendarInboxTests
{
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";

    [Fact]
    public void NewWorldHasScheduledSkeletonRaceOnDayTwelveAndEmptyInbox()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);

        Assert.Single(application.Calendar);
        CalendarEntryProjection entry = application.Calendar[0];
        Assert.Equal(12, entry.DayNumber);
        Assert.Equal("race", entry.Kind);
        Assert.Equal("scheduled", entry.Status);
        Assert.Equal("Skeleton race", entry.Title);
        Assert.Null(entry.OfficialResult);
        Assert.Empty(application.Inbox);
    }

    [Fact]
    public void TwelfthDayMarksRaceDueInboxAndRejectsArchiveWhileBlockingAdvance()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        for (int day = 0; day < 12; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        CalendarEntryProjection entry = Assert.Single(application.Calendar);
        Assert.Equal("due", entry.Status);
        InboxItemProjection inboxItem = Assert.Single(application.Inbox);
        Assert.Equal("race-due", inboxItem.Category);
        Assert.Equal("A race is due today.", inboxItem.Body);
        Assert.Equal(12, inboxItem.DayNumber);
        Assert.Equal($"calendar:{entry.Id.Value}:due", inboxItem.Identity);
        Assert.Equal(entry.Id, inboxItem.RelatedEntryId);

        CommandResult archive = application.Execute(new ArchiveInboxItemCommand(inboxItem.Identity));
        Assert.False(archive.Succeeded);
        Assert.Equal("INBOX_SOURCE_CANNOT_BE_DISMISSED", archive.ReasonCode);
        Assert.True(application.World!.IsRaceDue);
        Assert.Equal(GameState.Management, application.State);
        Assert.Equal("RACE_DAY_PENDING", application.Execute(new AdvanceDayCommand()).ReasonCode);
    }

    [Fact]
    public void CompletingRaceMarksEntryCompletedWithOfficialResultAndRaceResultInbox()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        for (int day = 0; day < 12; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }

        WorldEntityId firstRaceEntryId = application.Calendar[0].Id;
        using TemporaryDirectory temp = new();
        CompleteRaceFlow(application, temp.Path);

        Assert.Equal(2, application.Calendar.Count);
        CalendarEntryProjection completed = application.Calendar.Single(entry => entry.Id == firstRaceEntryId);
        Assert.Equal("completed", completed.Status);
        Assert.NotNull(completed.OfficialResult);
        Assert.StartsWith("Winner ", completed.OfficialResult, StringComparison.Ordinal);
        CalendarEntryProjection upcoming = application.Calendar.Single(entry => entry.DayNumber == 24);
        Assert.Equal("scheduled", upcoming.Status);
        Assert.Equal("Skeleton race", upcoming.Title);
        Assert.Null(upcoming.OfficialResult);

        InboxItemProjection resultItem = Assert.Single(application.Inbox);
        Assert.Equal("race-result", resultItem.Category);
        Assert.Equal($"calendar:{firstRaceEntryId.Value}:result", resultItem.Identity);
        Assert.Contains(completed.OfficialResult, resultItem.Body, StringComparison.Ordinal);
        Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        Assert.Equal(13, application.World!.CurrentDate.DayNumber);
    }

    [Fact]
    public void ArchivingRaceResultClearsInboxButKeepsCalendarOfficialResult()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        for (int day = 0; day < 12; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }

        using TemporaryDirectory temp = new();
        CompleteRaceFlow(application, temp.Path);
        WorldEntityId firstRaceEntryId = application.Calendar[0].Id;
        string? officialResult = application.Calendar[0].OfficialResult;
        string resultIdentity = Assert.Single(application.Inbox).Identity;
        string checksumBefore = WorldChecksum.Compute(application.World!);

        Assert.True(application.Execute(new ArchiveInboxItemCommand(resultIdentity)).Succeeded);
        Assert.Empty(application.Inbox);
        CalendarEntryProjection completed = application.Calendar.Single(entry => entry.Id == firstRaceEntryId);
        Assert.Equal("completed", completed.Status);
        Assert.Equal(officialResult, completed.OfficialResult);
        Assert.NotEqual(checksumBefore, WorldChecksum.Compute(application.World!));
    }

    [Fact]
    public void SaveLoadPreservesOfficialResultAndResultAcknowledged()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "race-result.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        for (int day = 0; day < 12; day++)
        {
            Assert.True(source.Execute(new AdvanceDayCommand()).Succeeded);
        }

        CompleteRaceFlow(source, temp.Path);
        WorldEntityId calendarEntryId = source.Calendar[0].Id;
        string? officialResult = source.Calendar[0].OfficialResult;
        string resultIdentity = Assert.Single(source.Inbox).Identity;
        Assert.True(source.Execute(new ArchiveInboxItemCommand(resultIdentity)).Succeeded);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        CalendarEntryProjection loadedEntry = loaded.Calendar.Single(entry => entry.Id == calendarEntryId);
        Assert.Equal("completed", loadedEntry.Status);
        Assert.Equal(officialResult, loadedEntry.OfficialResult);
        Assert.Empty(loaded.Inbox);
    }

    [Fact]
    public void SaveLoadPreservesCalendarEntryIdsAndDueInbox()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "race-due.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        for (int day = 0; day < 12; day++)
        {
            Assert.True(source.Execute(new AdvanceDayCommand()).Succeeded);
        }

        WorldEntityId calendarEntryId = source.Calendar[0].Id;
        string dueIdentity = Assert.Single(source.Inbox).Identity;
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(calendarEntryId, loaded.Calendar[0].Id);
        Assert.Equal("due", loaded.Calendar[0].Status);
        Assert.Equal(dueIdentity, Assert.Single(loaded.Inbox).Identity);
        Assert.True(loaded.World!.IsRaceDue);
    }

    [Fact]
    public void QueryingCalendarAndInboxDoesNotChangeChecksum()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        for (int day = 0; day < 12; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }

        string before = WorldChecksum.Compute(application.World!);
        _ = application.Calendar.ToArray();
        _ = application.Inbox.ToArray();
        Assert.Equal(before, WorldChecksum.Compute(application.World!));
    }

    [Fact]
    public void ArchiveUnknownInboxItemIsRejected()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);

        CommandResult result = application.Execute(new ArchiveInboxItemCommand("missing:identity"));
        Assert.False(result.Succeeded);
        Assert.Equal("INBOX_ITEM_NOT_FOUND", result.ReasonCode);
    }

    private static void CompleteRaceFlow(GameApplication application, string autosaveDirectory)
    {
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new StartRaceCommand(
            Path.Combine(autosaveDirectory, "pre-race.peloton"),
            PrototypeRaceScenarioId)).Succeeded);

        for (int barrier = 0; barrier < 32 && application.State == GameState.RaceLive; barrier++)
        {
            Assert.True(application.Execute(new AdvanceRaceCommand()).Succeeded);
            if (application.PendingRaceDecision is PendingRaceDecision decision)
            {
                Assert.True(application.Execute(new RespondToRaceDecisionCommand(
                    decision.RequestId,
                    decision.AuthorityId,
                    decision.DelegatedDefaultOption)).Succeeded);
            }
        }

        Assert.True(application.Execute(new AcknowledgeRaceResultsCommand()).Succeeded);
        Assert.True(application.Execute(new CompleteRaceDebriefCommand()).Succeeded);
    }
}
