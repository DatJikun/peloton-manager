using System;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class RaceWatchTests
{
    private static readonly WorldEntityId TeamA = new(701);
    private static readonly WorldEntityId TeamAAuthority = new(801);

    [Fact]
    public void WatchCompressesQuietSecondsAndKeepsOfficialChecksum()
    {
        RaceScenario scenario = ChaseScenario();
        PrototypeRaceEngine engine = new();
        RaceResult official = engine.RunBatch(scenario, 505);
        RaceWatchReport watch = RaceWatchProjector.Project(scenario, 505);

        Assert.Equal(official.Checksum, watch.Result.Checksum);
        Assert.Equal(official.FinishOrder, watch.Result.FinishOrder);
        Assert.Equal(official.DecisionCount, watch.Result.DecisionCount);
        Assert.True(watch.Beats.Count < watch.Result.RiderMetrics.Max(rider => (int)rider.FinishTimeSeconds));
        Assert.Equal("start", watch.Beats[0].Kind);
        Assert.Contains(watch.Beats, beat => beat.Kind == "decision");
        Assert.Equal("finish", watch.Beats[^1].Kind);
        Assert.DoesNotContain(
            watch.Beats,
            beat => beat.Headline.Contains("WPrime", StringComparison.OrdinalIgnoreCase)
                || beat.Headline.Contains("Durability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SameSeedProducesIdenticalWatchBeats()
    {
        RaceScenario scenario = ChaseScenario();
        RaceWatchReport first = RaceWatchProjector.Project(scenario, 606);
        RaceWatchReport second = RaceWatchProjector.Project(scenario, 606);

        Assert.Equal(first.Result.Checksum, second.Result.Checksum);
        Assert.Equal(
            first.Beats.Select(beat => (beat.WatchSecond, beat.SimulationSecond, beat.Kind, beat.Headline, beat.Selected)),
            second.Beats.Select(beat => (beat.WatchSecond, beat.SimulationSecond, beat.Kind, beat.Headline, beat.Selected)));
    }

    private static RaceScenario ChaseScenario()
    {
        RaceRiderProfile support = RaceScenarioFactory.Profile(71, TeamA.Value, 390, 28_000, 930, 0.85);
        RaceRiderProfile leader = RaceScenarioFactory.Profile(72, TeamA.Value, 370, 25_000, 900, 0.82);
        RaceRiderProfile rival = RaceScenarioFactory.Profile(73, 702, 375, 26_000, 910, 0.83);
        RaceRiderProfile rivalSupport = RaceScenarioFactory.Profile(74, 702, 360, 23_000, 880, 0.78);
        RaceRiderProfile[] riders = { support, leader, rival, rivalSupport };
        RaceDefinition definition = new(
            "route.proof.watch",
            1.225,
            new[]
            {
                new RaceRouteSegment(
                    "segment.proof.watch",
                    lengthM: 1_800,
                    gradient: 0,
                    roadWidthM: 5,
                    windSpeedMps: 3,
                    windYawDegrees: 25),
            });
        RaceStartingPosition[] positions = riders
            .Select((rider, index) => new RaceStartingPosition(rider.RiderId, (3 - index) * 0.7))
            .ToArray();
        RaceTacticalPlan plan = new(
            TriggerSecond: 5,
            SupportRiderId: support.RiderId,
            new TeamRaceObservation(
                TeamA,
                TeamAAuthority,
                OfficialGapSeconds: 42,
                VisibleSplit: true,
                LeaderPositionBand: RacePositionBand.Front,
                ResourceEstimate: RaceResourceEstimate.Strong,
                ThreatEstimate: RaceThreatEstimate.High,
                Objective: RaceObjective.StageWin,
                Confidence: RaceInformationConfidence.High),
            new RaceBriefing(RaceBriefingKind.Chase, ConsultManager: true));
        return new RaceScenario(
            "race.proof.watch",
            definition,
            riders,
            positions,
            Array.Empty<RaceCommand>(),
            initialSpeedMps: 11,
            maximumDurationSeconds: 600,
            tacticalPlans: new[] { plan });
    }
}
