using System;
using System.IO;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class WatchRaceHostTests
{
    private const long GateSeed = 91234;

    [Fact]
    public void HostWatchUsesStartRaceClockAndMatchesSimulateWithoutRunBatch()
    {
        using TemporaryDirectory temp = new();
        CountingRaceEngine engine = new();
        WatchRaceHost host = CreateHost(temp.Path, engine);

        Assert.True(host.OpenPrototype(GateSeed).Succeeded);
        Assert.Equal(GameState.RacePreparationFlow, host.State);
        Assert.True(host.SetDefaultStrategy().Succeeded);
        Assert.True(host.ConfirmPreparation().Succeeded);
        Assert.True(host.SelectRate(5).Succeeded);
        Assert.True(host.StartWatch().Succeeded);
        Assert.Equal(GameState.RaceLive, host.State);
        Assert.NotNull(host.Course);
        Assert.NotNull(host.OfficialFrame);
        Assert.All(
            host.OfficialFrame!.FocalRiders,
            rider =>
            {
                Assert.InRange(rider.ShelterMultiplier, 0.0, 1.0);
                Assert.True(double.IsFinite(rider.Gradient));
            });

        CompleteWatch(host);

        Assert.Equal(GameState.RaceResultsFlow, host.State);
        Assert.Equal(1, engine.CreateSessionCalls);
        Assert.Equal(0, engine.RunBatchCalls);
        RaceResultProjection result = Assert.IsType<RaceResultProjection>(host.Result);
        Assert.Equal("rider.race-prototype.beta-leader", result.WinnerLabel);
        Assert.False(string.IsNullOrWhiteSpace(host.LastChecksum));
        Assert.DoesNotContain("WPrime", result.WinnerLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HostTicksOneWatchSecondPerRealSecondAndPausesOnDecision()
    {
        using TemporaryDirectory temp = new();
        WatchRaceHost host = CreateHost(temp.Path, new PrototypeRaceEngine());
        Assert.True(host.OpenPrototype(GateSeed).Succeeded);
        Assert.True(host.SetDefaultStrategy().Succeeded);
        Assert.True(host.ConfirmPreparation().Succeeded);
        Assert.True(host.SelectRate(1).Succeeded);
        Assert.True(host.StartWatch().Succeeded);
        int startWatch = host.OfficialFrame!.WatchSecond;
        int startSim = host.OfficialFrame.RaceSecond;

        Assert.True(host.Tick(0.49).Succeeded);
        Assert.Equal(startWatch, host.OfficialFrame.WatchSecond);

        Assert.True(host.Tick(0.51).Succeeded);
        Assert.Equal(startWatch + 1, host.OfficialFrame.WatchSecond);
        Assert.InRange(host.OfficialFrame.RaceSecond - startSim, 1, 5);

        bool sawDecision = false;
        for (int step = 0; step < 50_000 && host.State == GameState.RaceLive; step++)
        {
            if (host.PendingDecision is PendingRaceDecision)
            {
                int frozenWatch = host.OfficialFrame!.WatchSecond;
                int frozenSim = host.OfficialFrame.RaceSecond;
                Assert.True(host.OfficialFrame.Paused);
                Assert.True(host.Tick(3.0).Succeeded);
                Assert.Equal(frozenWatch, host.OfficialFrame.WatchSecond);
                Assert.Equal(frozenSim, host.OfficialFrame.RaceSecond);
                Assert.True(host.RespondDelegatedDefault().Succeeded);
                sawDecision = true;
                break;
            }

            Assert.True(host.Tick(1.0).Succeeded);
        }

        Assert.True(sawDecision);
    }

    [Fact]
    public void HostAbandonRollsBackAutosaveAndDoesNotKeepLiveSession()
    {
        using TemporaryDirectory temp = new();
        string autosave = Path.Combine(temp.Path, "pre-race.peloton");
        WatchRaceHost host = CreateHost(temp.Path, new PrototypeRaceEngine());
        Assert.True(host.OpenPrototype(GateSeed).Succeeded);
        Assert.True(host.SetDefaultStrategy().Succeeded);
        Assert.True(host.ConfirmPreparation().Succeeded);
        Assert.True(host.StartWatch().Succeeded);
        Assert.True(File.Exists(autosave));
        Assert.Equal(
            "WATCH_RATE_LOCKED",
            host.SelectRate(20).ReasonCode);

        Assert.True(host.Abandon().Succeeded);
        Assert.Equal(GameState.RacePreparationFlow, host.State);
        Assert.Null(host.OfficialFrame);
        Assert.Null(host.Interpolated);
    }

    private static void CompleteWatch(WatchRaceHost host)
    {
        for (int barrier = 0; barrier < 100_000 && host.State == GameState.RaceLive; barrier++)
        {
            if (host.PendingDecision is not null)
            {
                Assert.True(host.RespondDelegatedDefault().Succeeded);
                continue;
            }

            Assert.True(host.Tick(1.0).Succeeded);
        }

        Assert.Equal(GameState.RaceResultsFlow, host.State);
    }

    private static WatchRaceHost CreateHost(string directory, IRaceEngine engine)
    {
        GameApplication application = new(
            new JsonScenarioCatalog(ContentRoot()),
            new JsonRacePrototypeCatalog(ContentRoot()),
            new SqliteWorldSaveStore(),
            engine);
        return new WatchRaceHost(application, Path.Combine(directory, "pre-race.peloton"));
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

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"peloton-godot-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}
