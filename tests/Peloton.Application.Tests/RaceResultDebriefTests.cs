using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class RaceResultDebriefTests
{
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";
    private const long GateSeed = 91234;
    private static readonly string[] CommittedResultNotes =
    {
        "Oficjalny zwycięzca: Marco Anconi.",
    };
    private static readonly string[] UncertainStaffNotes =
    {
        RaceOutcomeQueries.UncertainStaffNote,
    };

    private static readonly string[] SkeletonTeamNames =
    {
        "Beskid–Vetter",
        "Fala–Karpaty",
        "Ost-Wind",
    };

    [Fact]
    public void RaceResultTableNamesTeamsAndKeepsGlobalPlacesWhenFiltered()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        RaceResultProjection result = Assert.IsType<RaceResultProjection>(application.RaceResult);
        Assert.Equal(12, result.FinishOrder.Count);
        Assert.Equal(3, result.Teams.Count);
        Assert.Equal(SkeletonTeamNames, result.Teams.Select(team => team.Name));
        Assert.Equal(4, result.FinishOrder.Count(row => row.TeamName == "Beskid–Vetter"));
        Assert.Equal(4, result.FinishOrder.Count(row => row.TeamName == "Fala–Karpaty"));
        Assert.Equal(4, result.FinishOrder.Count(row => row.TeamName == "Ost-Wind"));

        RaceResultTeam beskid = result.Teams.Single(team => team.Name == "Beskid–Vetter");
        IReadOnlyList<RaceResultPlacement> filtered = RaceOutcomeQueries.FilterPlacements(result, beskid.Id);
        Assert.Equal(4, filtered.Count);
        Assert.All(filtered, row => Assert.Equal("Beskid–Vetter", row.TeamName));
        Assert.Equal(filtered.Select(row => row.Place), filtered.Select(row => row.Place).Distinct());
        Assert.DoesNotContain(filtered, row => row.Place == 1);
        Assert.Contains(filtered, row => row.Place > 4);
        Assert.Equal(
            RaceOutcomeQueries.FormatTable(result, teamId: null),
            string.Join('\n', result.FinishOrder.Select(row => $"{row.Place}. {row.Label} | {row.TeamName}")));
        Assert.DoesNotContain("WPrime", RaceOutcomeQueries.FormatTable(result, beskid.Id), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Marco Anconi", RaceOutcomeQueries.FormatTable(result, beskid.Id), StringComparison.Ordinal);
        Assert.Contains("Beskid–Vetter", RaceOutcomeQueries.FormatTable(result, beskid.Id), StringComparison.Ordinal);
    }
    {
        CountingRaceEngine engine = new();
        GameApplication application = Create(engine);
        Assert.Null(application.RaceResult);
        Assert.Null(application.RaceDebrief);
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.Null(application.RaceResult);

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        Assert.Equal(1, engine.RunBatchCalls);
        RaceResultProjection result = Assert.IsType<RaceResultProjection>(application.RaceResult);
        Assert.Null(application.RaceDebrief);
        Assert.Equal("Skeleton race", result.Title);
        Assert.Equal("race-route.peloton.synthetic-proof-v0", result.RouteId);
        Assert.Equal(9, result.WinnerId.Value);
        Assert.Equal("Marco Anconi", result.WinnerLabel);
        Assert.Equal(12, result.FinishOrder.Count);
        Assert.Equal("Marco Anconi", result.FinishOrder[0].Label);
        Assert.Equal(1, result.FinishOrder[0].Place);
        Assert.Equal("Fala–Karpaty", result.FinishOrder[0].TeamName);
        Assert.Equal(application.World!.LastRace!.FinishOrder, result.FinishOrder.Select(place => place.RiderId));
        Assert.All(
            result.FinishOrder,
            place => Assert.False(string.IsNullOrWhiteSpace(place.Label)));
        Assert.All(
            result.FinishOrder,
            place => Assert.False(string.IsNullOrWhiteSpace(place.TeamName)));
        Assert.Equal(SkeletonTeamNames, result.Teams.Select(team => team.Name));
        Assert.Contains(result.Headlines, line => line.Contains("Marco Anconi", StringComparison.Ordinal));
        Assert.Contains(result.Headlines, line => line.Contains("Piotr Kowalczyk", StringComparison.Ordinal));
        Assert.Contains(result.Headlines, line => line.Contains("Cel StageWin", StringComparison.Ordinal));
        Assert.Contains(result.Headlines, line => line.Equals(RaceOutcomeQueries.StaffDecisionHeadline, StringComparison.Ordinal));
        Assert.All(result.Headlines, line => Assert.DoesNotContain("WPrime", line, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, engine.RunBatchCalls);
        Assert.Equal(0, engine.CreateSessionCalls);

        _ = application.RaceResult;
        _ = application.RaceResult;
        Assert.Equal(1, engine.RunBatchCalls);
        Assert.Equal(0, engine.CreateSessionCalls);
    }

    [Fact]
    public void WatchAndSimulateShareTheSameResultProjectionWithoutASecondBatch()
    {
        using TemporaryDirectory temp = new();
        CountingRaceEngine watchEngine = new();
        GameApplication watched = Create(watchEngine);
        Assert.True(watched.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(watched.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(watched.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(watched.Execute(new StartRaceCommand(
            Path.Combine(temp.Path, "pre-race.peloton"),
            PrototypeRaceScenarioId)).Succeeded);
        CompleteRace(watched);

        CountingRaceEngine simulateEngine = new();
        GameApplication simulated = Create(simulateEngine);
        Assert.True(simulated.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(simulated.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(simulated.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(simulated.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        Assert.Equal(0, watchEngine.RunBatchCalls);
        Assert.Equal(1, simulateEngine.RunBatchCalls);
        RaceResultProjection watchedResult = Assert.IsType<RaceResultProjection>(watched.RaceResult);
        RaceResultProjection simulatedResult = Assert.IsType<RaceResultProjection>(simulated.RaceResult);
        Assert.Equivalent(watchedResult, simulatedResult, strict: true);
        Assert.Equal(0, watchEngine.RunBatchCalls);
        Assert.Equal(1, simulateEngine.RunBatchCalls);
        Assert.Equal(
            WorldChecksum.Compute(watched.World!),
            WorldChecksum.Compute(simulated.World!));
    }

    [Fact]
    public void RaceDebriefProjectionUsesConfirmedPrepObjectiveAndCommittedResultFacts()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);
        Assert.Null(application.RaceDebrief);

        Assert.True(application.Execute(new AcknowledgeRaceResultsCommand()).Succeeded);

        Assert.Null(application.RaceResult);
        RaceDebriefProjection debrief = Assert.IsType<RaceDebriefProjection>(application.RaceDebrief);
        Assert.Equal("StageWin", debrief.Objective);
        Assert.InRange(debrief.Notes.Count, 1, 3);
        Assert.Equal(CommittedResultNotes, debrief.Notes);
        Assert.Equal(application.World!.LastRace!.WinnerId, application.World.LastRace.FinishOrder[0]);
        Assert.DoesNotContain(debrief.Notes, note => note.Contains("Widoczny rozjazd", StringComparison.Ordinal));
        Assert.DoesNotContain(debrief.Notes, note => note.Contains("LeaderPositionBand", StringComparison.Ordinal));
        Assert.DoesNotContain(debrief.Notes, note => note.Contains("ResourceEstimate", StringComparison.Ordinal));
        Assert.DoesNotContain(debrief.Notes, note => note.Contains("pasmo", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(debrief.Notes, note => note.Contains("zasoby", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(debrief.Notes, note => note.Contains("WPrime", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(debrief.Notes, note => note.Contains("durability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DebriefWithoutCommittedResultReportsStaffUncertainty()
    {
        RaceDebriefProjection debrief = RaceOutcomeQueries.BuildDebrief(
            world: null,
            racePreparation: null,
            raceScenarioCatalog: new JsonRacePrototypeCatalog(TestApplication.ContentRoot));

        Assert.Equal("StageWin", debrief.Objective);
        Assert.Equal(UncertainStaffNotes, debrief.Notes);
    }

    [Fact]
    public void PreparationCheckpointSurvivesResultsAndClearsOnlyOnCompleteDebrief()
    {
        using TemporaryDirectory temp = new();
        string resultsPath = Path.Combine(temp.Path, "results.peloton");
        string debriefPath = Path.Combine(temp.Path, "debrief.peloton");
        string donePath = Path.Combine(temp.Path, "done.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(source.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(source.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        string worldChecksum = WorldChecksum.Compute(source.World!);
        Assert.True(source.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);
        Assert.Null(source.RacePreparation);
        Assert.Equal(GameState.RaceResultsFlow, source.State);
        string racedChecksum = WorldChecksum.Compute(source.World!);
        Assert.NotEqual(worldChecksum, racedChecksum);

        Assert.True(source.Execute(new SaveGameCommand(resultsPath)).Succeeded);
        WorldCheckpoint storedResults = new SqliteWorldSaveStore().Load(resultsPath);
        Assert.Equal(GameState.RaceResultsFlow, storedResults.GameState);
        RacePreparationCheckpoint resultsPlan = Assert.IsType<RacePreparationCheckpoint>(storedResults.RacePreparation);
        Assert.True(resultsPlan.PlanConfirmed);
        Assert.Equal(PrototypeRaceScenarioId, resultsPlan.RaceScenarioId);
        Assert.Equal(racedChecksum, WorldChecksum.Compute(storedResults.World));

        GameApplication loadedResults = TestApplication.Create();
        Assert.True(loadedResults.Execute(new LoadGameCommand(resultsPath)).Succeeded);
        Assert.Equal(GameState.RaceResultsFlow, loadedResults.State);
        Assert.Equal("Marco Anconi", loadedResults.RaceResult!.WinnerLabel);
        Assert.True(loadedResults.Execute(new AcknowledgeRaceResultsCommand()).Succeeded);
        Assert.Equal("StageWin", loadedResults.RaceDebrief!.Objective);
        Assert.True(loadedResults.Execute(new SaveGameCommand(debriefPath)).Succeeded);

        WorldCheckpoint storedDebrief = new SqliteWorldSaveStore().Load(debriefPath);
        Assert.Equal(GameState.RaceDebriefFlow, storedDebrief.GameState);
        Assert.True(storedDebrief.RacePreparation!.PlanConfirmed);
        Assert.Equal(racedChecksum, WorldChecksum.Compute(storedDebrief.World));

        Assert.True(loadedResults.Execute(new CompleteRaceDebriefCommand()).Succeeded);
        Assert.Equal(GameState.Management, loadedResults.State);
        Assert.Null(loadedResults.RaceResult);
        Assert.Null(loadedResults.RaceDebrief);
        Assert.Null(loadedResults.RacePreparation);
        Assert.Equal(racedChecksum, WorldChecksum.Compute(loadedResults.World!));
        Assert.True(loadedResults.Execute(new SaveGameCommand(donePath)).Succeeded);

        WorldCheckpoint storedDone = new SqliteWorldSaveStore().Load(donePath);
        Assert.Equal(GameState.Management, storedDone.GameState);
        Assert.Null(storedDone.RacePreparation);
        Assert.Equal(racedChecksum, WorldChecksum.Compute(storedDone.World));
    }

    [Fact]
    public void CareerDayDoesNotImpersonateHubPrimaryActionDuringResultsOrDebrief()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 42)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        Assert.Null(application.CareerDay);
        Assert.True(application.Execute(new AcknowledgeRaceResultsCommand()).Succeeded);
        Assert.Null(application.CareerDay);
        Assert.True(application.Execute(new CompleteRaceDebriefCommand()).Succeeded);
        CareerDayProjection hub = Assert.IsType<CareerDayProjection>(application.CareerDay);
        Assert.Equal(HubPrimaryActionIds.AdvanceDay, hub.PrimaryAction);
        Assert.Equal(HubPrimaryActionLabels.AdvanceDay, hub.PrimaryLabel);
    }

    private static GameApplication Create(IRaceEngine raceEngine)
    {
        return new GameApplication(
            new JsonScenarioCatalog(TestApplication.ContentRoot),
            new JsonRacePrototypeCatalog(TestApplication.ContentRoot),
            new SqliteWorldSaveStore(),
            raceEngine);
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

    private sealed class CountingRaceEngine : IRaceEngine
    {
        private readonly PrototypeRaceEngine inner = new();

        public int RunBatchCalls { get; private set; }

        public int CreateSessionCalls { get; private set; }

        public RaceSession CreateSession(
            RaceScenario scenario,
            long seed,
            IWorldSpySink? spySink = null)
        {
            CreateSessionCalls++;
            return inner.CreateSession(scenario, seed, spySink);
        }

        public RaceResult RunBatch(
            RaceScenario scenario,
            long seed,
            IWorldSpySink? spySink = null)
        {
            RunBatchCalls++;
            return inner.RunBatch(scenario, seed, spySink);
        }
    }
}
