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
        Assert.Contains(application.World.RulesModules, module => module.Id == "rules.peloton.race.stub-v1");
    }

    [Fact]
    public void RaceLiveRejectsSaveAndCompletesCanonicalFlow()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 404)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        string autosave = Path.Combine(temp.Path, "pre-race.peloton");
        WorldEntityId[] startList = application.World!.Persons.Select(person => person.Id).ToArray();

        Assert.True(application.Execute(new StartRaceCommand(autosave, "route.skeleton.flat", startList)).Succeeded);
        Assert.Equal(GameState.RaceLive, application.State);

        CommandResult saveResult = application.Execute(new SaveGameCommand(Path.Combine(temp.Path, "forbidden.peloton")));
        Assert.False(saveResult.Succeeded);
        Assert.Equal("SAVE_FORBIDDEN_IN_RACE_LIVE", saveResult.ReasonCode);

        Assert.True(application.Execute(new CompleteStubRaceCommand()).Succeeded);
        Assert.Equal(GameState.RaceResultsFlow, application.State);
        Assert.True(application.Execute(new AcknowledgeRaceResultsCommand()).Succeeded);
        Assert.Equal(GameState.RaceDebriefFlow, application.State);
        Assert.True(application.Execute(new CompleteRaceDebriefCommand()).Succeeded);
        Assert.Equal(GameState.Management, application.State);
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
        WorldEntityId[] startList = live.World!.Persons.Select(person => person.Id).ToArray();
        Assert.True(live.Execute(new StartRaceCommand(autosave, "route.skeleton.flat", startList)).Succeeded);
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
}
