using System;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class GameApplicationTests
{
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";

    private static readonly string[] CanonicalGameStates =
    {
        "MainMenu",
        "NewGameFlow",
        "LoadingWorld",
        "Management",
        "PreSeasonPlanningFlow",
        "RacePreparationFlow",
        "RaceLive",
        "RaceResultsFlow",
        "RaceDebriefFlow",
    };

    [Fact]
    public void CanonicalGameStateContainsExactlyNineLockedValues()
    {
        Assert.Equal(CanonicalGameStates, Enum.GetNames<GameState>());
    }

    [Fact]
    public void AdvanceDayOutsideManagementIsRejectedWithoutMutation()
    {
        GameApplication application = TestApplication.Create();

        CommandResult result = application.Execute(new AdvanceDayCommand());

        Assert.False(result.Succeeded);
        Assert.Equal("GAME_STATE_INVALID", result.ReasonCode);
        Assert.Equal(GameState.MainMenu, application.State);
        Assert.Null(application.World);
    }

    [Fact]
    public void JsonScenarioRecordsCanonicalRecipeAndModuleIdentity()
    {
        GameApplication application = TestApplication.Create();

        CommandResult result = application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 404));

        Assert.True(result.Succeeded, result.ReasonCode);
        Assert.Equal(GameState.Management, application.State);
        Assert.Equal("Dynamic", application.World!.ContentIdentity.HistoryMode);
        Assert.Equal("Advanced", application.World.ContentIdentity.Difficulty);
        Assert.Equal("Guessed", application.World.ContentIdentity.AttributeVisibility);
        Assert.Contains(application.World.RulesModules, module => module.Id == "rules.peloton.calendar.skeleton");
        Assert.Contains(application.World.RulesModules, module => module.Id == "rules.peloton.race.prototype-v0");
    }

    [Fact]
    public void RaceLiveRejectsSaveAndLoadAndCompletesCanonicalFlow()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 404)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        string autosave = Path.Combine(temp.Path, "pre-race.peloton");
        Assert.True(application.Execute(new StartRaceCommand(autosave, PrototypeRaceScenarioId)).Succeeded);
        Assert.Equal(GameState.RaceLive, application.State);

        CommandResult saveResult = application.Execute(new SaveGameCommand(Path.Combine(temp.Path, "forbidden.peloton")));
        Assert.False(saveResult.Succeeded);
        Assert.Equal("SAVE_FORBIDDEN_IN_RACE_LIVE", saveResult.ReasonCode);

        CommandResult loadResult = application.Execute(new LoadGameCommand(autosave));
        Assert.False(loadResult.Succeeded);
        Assert.Equal("LOAD_FORBIDDEN_IN_RACE_LIVE", loadResult.ReasonCode);

        CompleteRace(application);
        Assert.Equal(GameState.RaceResultsFlow, application.State);
        Assert.Equal("race-route.peloton.synthetic-proof-v0", application.World!.LastRace!.RouteId);
        Assert.Equal(12, application.World.LastRace.FinishOrder.Count);
        Assert.True(application.Execute(new AcknowledgeRaceResultsCommand()).Succeeded);
        Assert.Equal(GameState.RaceDebriefFlow, application.State);
        Assert.True(application.Execute(new CompleteRaceDebriefCommand()).Succeeded);
        Assert.Equal(GameState.Management, application.State);
    }

    [Fact]
    public void RacePreparationProjectionUsesFixtureSquadAndConfirmationGatesBothPaths()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 404)).Succeeded);
        Assert.Null(application.RacePreparation);

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);

        RacePreparationProjection prep = Assert.IsType<RacePreparationProjection>(application.RacePreparation);
        Assert.Equal("Skeleton race", prep.Title);
        Assert.Equal("StageWin", prep.Objective);
        Assert.Equal(new long[] { 1001, 1002, 1003, 1004 }, prep.Squad.Select(id => id.Value));
        Assert.False(prep.PlanConfirmed);
        Assert.False(prep.CanStart);
        Assert.False(prep.CanSimulate);
        Assert.Equal(
            "PREP_PLAN_INCOMPLETE",
            application.Execute(new StartRaceCommand(
                Path.Combine(temp.Path, "blocked.peloton"),
                PrototypeRaceScenarioId)).ReasonCode);
        Assert.Equal(
            "PREP_PLAN_INCOMPLETE",
            application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).ReasonCode);

        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);

        prep = Assert.IsType<RacePreparationProjection>(application.RacePreparation);
        Assert.True(prep.PlanConfirmed);
        Assert.True(prep.CanStart);
        Assert.True(prep.CanSimulate);
    }

    [Fact]
    public void CancelRacePreparationReturnsToManagementWithoutCompletingDueRace()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        for (int day = 0; day < 12; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }

        Assert.True(application.Execute(new FollowHubPrimaryActionCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);

        Assert.True(application.Execute(new CancelRacePreparationCommand()).Succeeded);

        Assert.Equal(GameState.Management, application.State);
        Assert.Null(application.RacePreparation);
        Assert.True(application.World!.IsRaceDue);
        Assert.Equal(0, application.World.RaceCount);
        Assert.Equal(HubPrimaryActionIds.RaceNext, application.CareerDay!.PrimaryAction);
    }

    [Fact]
    public void SimulateFromConfirmedPreparationMatchesWatchResultWithoutReturningToRaceLive()
    {
        using TemporaryDirectory temp = new();
        GameApplication watched = TestApplication.Create();
        Assert.True(watched.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 99117)).Succeeded);
        Assert.True(watched.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(watched.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(watched.Execute(new StartRaceCommand(
            Path.Combine(temp.Path, "watched-pre-race.peloton"),
            PrototypeRaceScenarioId)).Succeeded);
        CompleteRace(watched);

        GameApplication simulated = TestApplication.Create();
        Assert.True(simulated.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 99117)).Succeeded);
        Assert.True(simulated.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(simulated.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);

        Assert.True(simulated.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        Assert.Equal(GameState.RaceResultsFlow, simulated.State);
        Assert.Null(simulated.RacePreparation);
        Assert.Null(simulated.PendingRaceDecision);
        Assert.Equivalent(watched.World!.LastRace, simulated.World!.LastRace, strict: true);
        Assert.Equal(
            Peloton.Simulation.WorldChecksum.Compute(watched.World),
            Peloton.Simulation.WorldChecksum.Compute(simulated.World));
    }

    [Fact]
    public void PendingRaceDecisionPausesInRaceLiveAndRejectsInvalidResponses()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 404)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new StartRaceCommand(
            Path.Combine(temp.Path, "pre-race.peloton"),
            PrototypeRaceScenarioId)).Succeeded);

        PendingRaceDecision decision = AdvanceUntilDecision(application);

        Assert.Equal(GameState.RaceLive, application.State);
        Assert.Contains(decision.DelegatedDefaultOption, decision.LegalOptions);
        CommandResult wrongAuthority = application.Execute(new RespondToRaceDecisionCommand(
            decision.RequestId,
            new WorldEntityId(decision.AuthorityId.Value + 1),
            decision.DelegatedDefaultOption));
        Assert.False(wrongAuthority.Succeeded);
        Assert.Equal("RACE_DECISION_AUTHORITY_INVALID", wrongAuthority.ReasonCode);

        CommandResult wrongRequest = application.Execute(new RespondToRaceDecisionCommand(
            new RaceDecisionRequestId(decision.RequestId.Value + ":wrong"),
            decision.AuthorityId,
            decision.DelegatedDefaultOption));
        Assert.False(wrongRequest.Succeeded);
        Assert.Equal("RACE_DECISION_REQUEST_INVALID", wrongRequest.ReasonCode);

        CommandResult illegalOption = application.Execute(new RespondToRaceDecisionCommand(
            decision.RequestId,
            decision.AuthorityId,
            (RaceDecisionOption)int.MaxValue));
        Assert.False(illegalOption.Succeeded);
        Assert.Equal("RACE_DECISION_OPTION_INVALID", illegalOption.ReasonCode);
        Assert.Equal(GameState.RaceLive, application.State);
        Assert.NotNull(application.PendingRaceDecision);

        Assert.True(application.Execute(new RespondToRaceDecisionCommand(
            decision.RequestId,
            decision.AuthorityId,
            decision.DelegatedDefaultOption)).Succeeded);
        Assert.Null(application.PendingRaceDecision);
        Assert.Equal(GameState.RaceLive, application.State);
    }

    [Fact]
    public void FailedPreRaceAutosaveDoesNotCreateLiveRaceSession()
    {
        using TemporaryDirectory temp = new();
        string fileBlockingDirectory = Path.Combine(temp.Path, "not-a-directory");
        File.WriteAllText(fileBlockingDirectory, "blocks Directory.CreateDirectory");
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 818)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);

        CommandResult result = application.Execute(new StartRaceCommand(
            Path.Combine(fileBlockingDirectory, "pre-race.peloton"),
            PrototypeRaceScenarioId));

        Assert.False(result.Succeeded);
        Assert.Equal("PRE_RACE_AUTOSAVE_FAILED", result.ReasonCode);
        Assert.Equal(GameState.RacePreparationFlow, application.State);
        Assert.Null(application.PendingRaceDecision);
        Assert.Equal("GAME_STATE_INVALID", application.Execute(new AdvanceRaceCommand()).ReasonCode);
    }

    [Fact]
    public void SameSeedRaceThroughApplicationIsDeterministic()
    {
        using TemporaryDirectory firstTemp = new();
        using TemporaryDirectory secondTemp = new();
        GameApplication first = RunOneRace(99117, firstTemp.Path);
        GameApplication second = RunOneRace(99117, secondTemp.Path);

        Assert.Equivalent(first.World!.LastRace, second.World!.LastRace, strict: true);
        Assert.Equal(
            Peloton.Simulation.WorldChecksum.Compute(first.World),
            Peloton.Simulation.WorldChecksum.Compute(second.World));
    }

    [Fact]
    public void TenSkeletonSeasonsRunHeadlessAndRepeatChecksum()
    {
        string first = TestApplication.RunTenSeasons(91234);
        string second = TestApplication.RunTenSeasons(91234);

        Assert.Equal(first, second);
    }

    [Fact]
    public void LoadingPreRaceAutosaveAfterLiveRaceRestoresPreparationState()
    {
        using TemporaryDirectory temp = new();
        string autosave = Path.Combine(temp.Path, "pre-race.peloton");
        GameApplication live = TestApplication.Create();
        Assert.True(live.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 505)).Succeeded);
        Assert.True(live.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(live.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        string preRaceChecksum = Peloton.Simulation.WorldChecksum.Compute(live.World!);
        Assert.True(live.Execute(new StartRaceCommand(autosave, PrototypeRaceScenarioId)).Succeeded);
        Assert.Equal(GameState.RaceLive, live.State);

        GameApplication recovered = TestApplication.Create();
        Assert.True(recovered.Execute(new LoadGameCommand(autosave)).Succeeded);

        Assert.Equal(GameState.RacePreparationFlow, recovered.State);
        Assert.Equal(0, recovered.World!.RaceCount);
        Assert.Equal(preRaceChecksum, Peloton.Simulation.WorldChecksum.Compute(recovered.World));
    }

    [Fact]
    public void AdvanceDayAdvancesEveryOrganization()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);

        Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);

        Assert.All(application.World!.Organizations, organization => Assert.Equal(1, organization.DaysSimulated));
        CareerDayProjection hub = Assert.IsType<CareerDayProjection>(application.CareerDay);
        Assert.Equal(1, hub.DayNumber);
        Assert.Equal("red", hub.EmployerName);
        Assert.Equal(11, hub.DaysUntilNextRace);
        Assert.False(hub.RaceDueToday);
        Assert.Contains(hub.TodayNotes, note => note.Contains("worked the day", StringComparison.Ordinal));
        Assert.Contains(hub.TodayNotes, note => note.Contains("rest of the world", StringComparison.Ordinal));
        Assert.Equal(HubPrimaryActionIds.AdvanceDay, hub.PrimaryAction);
        Assert.Equal(HubPrimaryActionLabels.AdvanceDay, hub.PrimaryLabel);
    }

    [Fact]
    public void FollowHubPrimaryActionAdvancesDayWhenRaceIsNotDue()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);

        CareerDayProjection hub = Assert.IsType<CareerDayProjection>(application.CareerDay);
        Assert.Equal(HubPrimaryActionIds.AdvanceDay, hub.PrimaryAction);
        Assert.Equal(1, hub.DayNumber);

        Assert.True(application.Execute(new FollowHubPrimaryActionCommand()).Succeeded);
        Assert.Equal(2, application.World!.CurrentDate.DayNumber);
        Assert.Equal(GameState.Management, application.State);
    }

    [Fact]
    public void RaceDueDayShowsRaceNextPrimaryActionAndFollowHubEntersPreparation()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        for (int day = 0; day < 12; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        CareerDayProjection hub = Assert.IsType<CareerDayProjection>(application.CareerDay);
        Assert.Equal(12, hub.DayNumber);
        Assert.True(hub.RaceDueToday);
        Assert.Equal(HubPrimaryActionIds.RaceNext, hub.PrimaryAction);
        Assert.Equal(HubPrimaryActionLabels.RaceNext, hub.PrimaryLabel);

        CommandResult blocked = application.Execute(new AdvanceDayCommand());
        Assert.False(blocked.Succeeded);
        Assert.Equal("RACE_DAY_PENDING", blocked.ReasonCode);
        Assert.Equal(12, application.World!.CurrentDate.DayNumber);

        Assert.True(application.Execute(new FollowHubPrimaryActionCommand()).Succeeded);
        Assert.Equal(GameState.RacePreparationFlow, application.State);
        Assert.Equal(12, application.World.CurrentDate.DayNumber);
    }

    [Fact]
    public void TwelfthDayBlocksAdvanceUntilTheRaceIsCompleted()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        for (int day = 0; day < 12; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        CareerDayProjection hub = Assert.IsType<CareerDayProjection>(application.CareerDay);
        Assert.Equal(12, hub.DayNumber);
        Assert.True(hub.RaceDueToday);
        Assert.Equal(0, hub.DaysUntilNextRace);
        Assert.Contains(hub.TodayNotes, note => note.Contains("race is due", StringComparison.OrdinalIgnoreCase));

        CommandResult blocked = application.Execute(new AdvanceDayCommand());
        Assert.False(blocked.Succeeded);
        Assert.Equal("RACE_DAY_PENDING", blocked.ReasonCode);
        Assert.Equal(12, application.World!.CurrentDate.DayNumber);

        using TemporaryDirectory temp = new();
        string raceDueSave = Path.Combine(temp.Path, "race-due.peloton");
        Assert.True(application.Execute(new SaveGameCommand(raceDueSave)).Succeeded);
        GameApplication reloaded = TestApplication.Create();
        Assert.True(reloaded.Execute(new LoadGameCommand(raceDueSave)).Succeeded);
        Assert.True(reloaded.World!.IsRaceDue);
        Assert.Equal("RACE_DAY_PENDING", reloaded.Execute(new AdvanceDayCommand()).ReasonCode);
        application = reloaded;
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new StartRaceCommand(
            Path.Combine(temp.Path, "pre-race.peloton"),
            PrototypeRaceScenarioId)).Succeeded);
        CompleteRace(application);
        Assert.True(application.Execute(new AcknowledgeRaceResultsCommand()).Succeeded);
        Assert.True(application.Execute(new CompleteRaceDebriefCommand()).Succeeded);

        Assert.False(application.World.IsRaceDue);
        Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        Assert.Equal(13, application.World.CurrentDate.DayNumber);
        Assert.Equal(11, application.CareerDay!.DaysUntilNextRace);
    }

    private static GameApplication RunOneRace(long seed, string autosaveDirectory)
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", seed)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new StartRaceCommand(
            Path.Combine(autosaveDirectory, "pre-race.peloton"),
            PrototypeRaceScenarioId)).Succeeded);
        CompleteRace(application);
        return application;
    }

    private static PendingRaceDecision AdvanceUntilDecision(GameApplication application)
    {
        Assert.True(application.Execute(new AdvanceRaceCommand()).Succeeded);

        return Assert.IsType<PendingRaceDecision>(application.PendingRaceDecision);
    }

    private static void CompleteRace(GameApplication application)
    {
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

        Assert.Equal(GameState.RaceResultsFlow, application.State);
    }

}
