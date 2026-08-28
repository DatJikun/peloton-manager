using System;
using System.IO;
using Peloton.Application;
using Peloton.Client.Godot;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class WatchRaceHostTests
{
    private const long GateSeed = 91234;
    private const string ExpectedChecksum =
        "5A35E88103E2FBB40325EA8BEF15AAAC2F2E1AB70F4E6DE2BBCE584EC7EE6721";

    [Fact]
    public void HostWatchUsesStartRaceClockAndMatchesSimulateWithoutRunBatch()
    {
        using TemporaryDirectory temp = new();
        CountingRaceEngine engine = new();
        WatchRaceHost host = CreateHost(temp.Path, engine);

        Assert.True(host.OpenPrototype(GateSeed).Succeeded);
        Assert.Equal(GameState.RacePreparationFlow, host.State);
        Assert.True(host.ConfirmPreparation().Succeeded);
        Assert.Equal(WatchFilmDuration.DefaultSeconds, host.SelectedFilmSeconds);
        Assert.Equal(300, host.SelectedFilmSeconds);
        Assert.Equal("WATCH_FILM_INVALID", host.SelectFilmDuration(90).ReasonCode);
        Assert.True(host.SelectFilmDuration(180).Succeeded);
        Assert.True(host.StartWatch().Succeeded);
        Assert.Equal(GameState.RaceLive, host.State);
        Assert.Equal(5, host.SelectedRate);
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
        Assert.Equal(1006, result.WinnerId.Value);
        Assert.Equal(ExpectedChecksum, host.LastChecksum);
        Assert.DoesNotContain("WPrime", result.WinnerLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HostTicksOneWatchSecondPerRealSecondAndPausesOnDecision()
    {
        using TemporaryDirectory temp = new();
        WatchRaceHost host = CreateHost(temp.Path, new PrototypeRaceEngine());
        Assert.True(host.OpenPrototype(GateSeed).Succeeded);
        Assert.True(host.ConfirmPreparation().Succeeded);
        Assert.True(host.StartWatch().Succeeded);
        Assert.Equal(3, host.SelectedRate);
        int startWatch = host.OfficialFrame!.WatchSecond;
        int startSim = host.OfficialFrame.RaceSecond;

        Assert.True(host.Tick(0.49).Succeeded);
        Assert.Equal(startWatch, host.OfficialFrame.WatchSecond);

        Assert.True(host.Tick(0.51).Succeeded);
        Assert.Equal(startWatch + 1, host.OfficialFrame.WatchSecond);
        Assert.Equal(3, host.OfficialFrame.RaceSecond - startSim);

        Assert.True(host.SetPresentationPaused(true).Succeeded);
        int pausedWatch = host.OfficialFrame.WatchSecond;
        int pausedSim = host.OfficialFrame.RaceSecond;
        Assert.True(host.Tick(20.0).Succeeded);
        Assert.Equal(pausedWatch, host.OfficialFrame.WatchSecond);
        Assert.Equal(pausedSim, host.OfficialFrame.RaceSecond);
        Assert.True(host.SetPresentationPaused(false).Succeeded);

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
        Assert.True(host.ConfirmPreparation().Succeeded);
        Assert.True(host.StartWatch().Succeeded);
        Assert.True(File.Exists(autosave));
        Assert.Equal(
            "WATCH_FILM_LOCKED",
            host.SelectFilmDuration(30).ReasonCode);
        Assert.Equal("WATCH_AUTONOMY_LOCKED", host.SelectDsAutonomy(true).ReasonCode);

        Assert.True(host.Abandon().Succeeded);
        Assert.Equal(GameState.RacePreparationFlow, host.State);
        Assert.Null(host.OfficialFrame);
        Assert.Null(host.Interpolated);
    }

    [Fact]
    public void HostBoardShowsSquadPlaceSpeedAndRadio()
    {
        using TemporaryDirectory temp = new();
        WatchRaceHost host = CreateHost(temp.Path, new PrototypeRaceEngine());
        Assert.True(host.OpenPrototype(GateSeed).Succeeded);
        Assert.Collection(
            host.SquadIds,
            id => Assert.Equal(1001L, id),
            id => Assert.Equal(1002L, id),
            id => Assert.Equal(1003L, id),
            id => Assert.Equal(1004L, id));
        Assert.True(host.ConfirmPreparation().Succeeded);
        Assert.True(host.StartWatch().Succeeded);
        InterpolatedWatchView view = Assert.IsType<InterpolatedWatchView>(host.Interpolated);
        Assert.Equal(4, view.Riders.Count);
        Assert.All(view.Riders, rider => Assert.Contains(rider.RiderId, host.SquadIds));
        Assert.True(view.Field.Count >= 4);
        Assert.Contains(view.Field, rider => rider.Place == 1);
        Assert.All(
            view.Riders,
            rider =>
            {
                Assert.True(WatchObservationText.SpeedKmh(rider.SpeedMps) > 0.0);
                Assert.DoesNotContain("WPrime", WatchObservationText.Radio(
                    rider.SpeedMps,
                    rider.ShelterMultiplier,
                    rider.Gradient,
                    rider.GapM), StringComparison.OrdinalIgnoreCase);
            });

        double? flatSpeed = null;
        double? climbSpeed = null;
        for (int step = 0; step < 5_000 && host.State == GameState.RaceLive; step++)
        {
            if (host.PendingDecision is not null)
            {
                Assert.True(host.RespondDelegatedDefault().Succeeded);
                continue;
            }

            InterpolatedWatchView live = Assert.IsType<InterpolatedWatchView>(host.Interpolated);
            foreach (InterpolatedRiderView rider in live.Field)
            {
                if (rider.Gradient < 0.01 && rider.SpeedMps >= 10.0)
                {
                    flatSpeed = rider.SpeedMps;
                }

                if (rider.Gradient >= 0.03 && rider.SpeedMps <= 8.5)
                {
                    climbSpeed = rider.SpeedMps;
                }
            }

            if (flatSpeed is not null && climbSpeed is not null)
            {
                break;
            }

            Assert.True(host.Tick(1.0).Succeeded);
        }

        Assert.True(flatSpeed.HasValue);
        Assert.True(climbSpeed.HasValue);
        Assert.True(climbSpeed.Value < flatSpeed.Value);
    }

    [Fact]
    public void HostDsAutonomyResolvesDecisionsWithoutPausingTheFilm()
    {
        using TemporaryDirectory temp = new();
        WatchRaceHost host = CreateHost(temp.Path, new PrototypeRaceEngine());
        Assert.True(host.OpenPrototype(GateSeed).Succeeded);
        Assert.True(host.ConfirmPreparation().Succeeded);
        Assert.False(host.DsAutonomy);
        Assert.True(host.SelectDsAutonomy(true).Succeeded);
        Assert.True(host.StartWatch().Succeeded);

        CompleteWatch(host);

        Assert.Equal(GameState.RaceResultsFlow, host.State);
        Assert.Equal(ExpectedChecksum, host.LastChecksum);
        Assert.Null(host.PendingDecision);
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
