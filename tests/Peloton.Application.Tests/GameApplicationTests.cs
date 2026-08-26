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
    public void PendingRaceDecisionPausesInRaceLiveAndRejectsInvalidResponses()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 404)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
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
    }

    private static GameApplication RunOneRace(long seed, string autosaveDirectory)
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", seed)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
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
