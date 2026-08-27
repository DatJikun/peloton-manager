using System;
using System.IO;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerWatchTests
{
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";
    private const long GateSeed = 91234;

    [Fact]
    public void CareerWatchClockUsesLiveSessionWithoutRunBatchAndMatchesSimulate()
    {
        using TemporaryDirectory temp = new();
        CountingRaceEngine watchEngine = new();
        GameApplication watched = Create(watchEngine);
        Assert.True(watched.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(watched.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(watched.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(watched.Execute(new StartRaceCommand(
            Path.Combine(temp.Path, "watch-pre-race.peloton"),
            PrototypeRaceScenarioId)).Succeeded);
        Assert.Equal(GameState.RaceLive, watched.State);
        Assert.True(watched.Execute(new BeginRaceWatchCommand(5)).Succeeded);

        CompleteWatch(watched);

        Assert.Equal(GameState.RaceResultsFlow, watched.State);
        Assert.Equal(1, watchEngine.CreateSessionCalls);
        Assert.Equal(0, watchEngine.RunBatchCalls);
        RaceResultProjection watchResult = Assert.IsType<RaceResultProjection>(watched.RaceResult);
        Assert.Equal(1006, watchResult.WinnerId.Value);
        Assert.Null(watched.CareerDay);

        CountingRaceEngine simulateEngine = new();
        GameApplication simulated = Create(simulateEngine);
        Assert.True(simulated.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(simulated.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(simulated.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(simulated.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        Assert.Equal(1, simulateEngine.RunBatchCalls);
        Assert.Equivalent(watchResult, simulated.RaceResult, strict: true);
        Assert.Equal(
            WorldChecksum.Compute(watched.World!),
            WorldChecksum.Compute(simulated.World!));
        Assert.Equal(watched.LastOfficialChecksum, simulated.LastOfficialChecksum);
        Assert.Equal(0, watchEngine.RunBatchCalls);
    }

    [Fact]
    public void CareerWatchRatesShareOfficialResultButNotWatchTime()
    {
        using TemporaryDirectory firstTemp = new();
        using TemporaryDirectory secondTemp = new();
        (GameApplication Application, int WatchSecond) rateOne = RunWatch(firstTemp.Path, rate: 1);
        (GameApplication Application, int WatchSecond) rateTwenty = RunWatch(secondTemp.Path, rate: 20);

        Assert.Equivalent(rateOne.Application.World!.LastRace, rateTwenty.Application.World!.LastRace, strict: true);
        Assert.Equal(rateOne.Application.LastOfficialChecksum, rateTwenty.Application.LastOfficialChecksum);
        Assert.Equal(1006, rateOne.Application.World.LastRace!.WinnerId.Value);
        Assert.True(rateOne.WatchSecond > rateTwenty.WatchSecond);
        Assert.Equal(
            WorldChecksum.Compute(rateOne.Application.World),
            WorldChecksum.Compute(rateTwenty.Application.World));
    }

    private static (GameApplication Application, int WatchSecond) RunWatch(string autosaveDirectory, int rate)
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new StartRaceCommand(
            Path.Combine(autosaveDirectory, "pre-race.peloton"),
            PrototypeRaceScenarioId)).Succeeded);
        Assert.True(application.Execute(new BeginRaceWatchCommand(rate)).Succeeded);
        CompleteWatch(application);
        return (application, application.LastWatchSecond);
    }

    private static void CompleteWatch(GameApplication application)
    {
        for (int barrier = 0; barrier < 100_000 && application.State == GameState.RaceLive; barrier++)
        {
            if (application.PendingRaceDecision is PendingRaceDecision decision)
            {
                Assert.True(application.RaceWatch!.Paused);
                Assert.True(application.Execute(new RespondToRaceDecisionCommand(
                    decision.RequestId,
                    decision.AuthorityId,
                    decision.DelegatedDefaultOption)).Succeeded);
                continue;
            }

            Assert.True(application.Execute(new AdvanceRaceWatchCommand()).Succeeded);
        }

        Assert.Equal(GameState.RaceResultsFlow, application.State);
        Assert.Null(application.RaceWatch);
    }

    private static GameApplication Create(IRaceEngine raceEngine)
    {
        return new GameApplication(
            new JsonScenarioCatalog(TestApplication.ContentRoot),
            new JsonRacePrototypeCatalog(TestApplication.ContentRoot),
            new SqliteWorldSaveStore(),
            raceEngine);
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
