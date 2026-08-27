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
    public void RateOneAndTwentyKeepOfficialResultButUseDifferentWatchTime()
    {
        RaceScenario scenario = ChaseScenario();
        PrototypeRaceEngine engine = new();
        RaceResult official = engine.RunBatch(scenario, 505);
        RaceWatchReport rateOne = RaceWatchProjector.Project(scenario, 505, rate: 1);
        RaceWatchReport rateTwenty = RaceWatchProjector.Project(scenario, 505, rate: 20);

        Assert.Equal(official.Checksum, rateOne.Result.Checksum);
        Assert.Equal(official.FinishOrder, rateOne.Result.FinishOrder);
        Assert.Equal(rateOne.Result.Checksum, rateTwenty.Result.Checksum);
        Assert.Equal(rateOne.Result.FinishOrder, rateTwenty.Result.FinishOrder);
        Assert.NotEqual(rateOne.WatchSeconds, rateTwenty.WatchSeconds);
        Assert.True(rateOne.WatchSeconds > rateTwenty.WatchSeconds);
        Assert.All(
            rateTwenty.Frames.Zip(rateTwenty.Frames.Skip(1)),
            pair => Assert.InRange(pair.Second.RaceSecond - pair.First.RaceSecond, 0, 20));
        Assert.DoesNotContain(
            RaceWatchProjector.ExportMarkdown(rateTwenty),
            "WPrime",
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecisionRequestFreezesBothClocksUntilResponded()
    {
        RaceScenario scenario = ChaseScenario();
        RaceSession session = new PrototypeRaceEngine().CreateSession(scenario, 606);
        RaceWatchClock clock = new(session, rate: 5);
        RaceWatchFrame paused;
        do
        {
            paused = clock.AdvanceOneWatchSecond();
        }
        while (!paused.Paused);

        RaceWatchFrame held = clock.AdvanceOneWatchSecond();

        Assert.True(paused.Paused);
        Assert.Equal(paused.WatchSecond, held.WatchSecond);
        Assert.Equal(paused.RaceSecond, held.RaceSecond);
        RaceDecisionRequest request = Assert.IsType<RaceDecisionRequest>(clock.PendingDecision);
        clock.Respond(new RaceDecisionResolution(
            request.Id,
            request.AuthorityId,
            request.DelegatedDefaultOption));
        Assert.False(clock.Current.Paused);

        RaceWatchFrame resumed = clock.AdvanceOneWatchSecond();

        Assert.True(resumed.WatchSecond > held.WatchSecond);
        Assert.True(resumed.RaceSecond > held.RaceSecond);
    }

    [Fact]
    public void ProjectionContainsOnlySmoothPublicMotionForFocalRiders()
    {
        RaceScenario scenario = ChaseScenario();
        RaceSession session = new PrototypeRaceEngine().CreateSession(scenario, 707);
        RaceWatchClock clock = new(session, rate: 2);

        RaceWatchFrame first = clock.Current;
        RaceWatchFrame second = clock.AdvanceOneWatchSecond();

        Assert.Equal(2, second.RaceSecond);
        Assert.InRange(second.FocalRiders.Count, 2, 3);
        Assert.Equal(0.0, second.FocalRiders[0].GapM, precision: 8);
        Assert.All(second.FocalRiders, rider =>
        {
            Assert.True(rider.DistanceM >= 0.0);
            Assert.True(rider.GapM >= 0.0);
            Assert.True(rider.SpeedMps >= 0.0);
        });
        Assert.All(
            second.FocalRiders.Join(
                first.FocalRiders,
                current => current.RiderId,
                previous => previous.RiderId,
                (current, previous) => current.DistanceM - previous.DistanceM),
            distanceDelta => Assert.InRange(distanceDelta, 0.0, 40.0));
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
