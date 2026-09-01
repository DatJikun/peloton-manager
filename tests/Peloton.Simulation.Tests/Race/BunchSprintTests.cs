using System;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class BunchSprintTests
{
    [Fact]
    public void HighPeakSprinterBeatsHighCpClimberOnFlatBunchFinish()
    {
        RaceScenario scenario = RaceScenarioFactory.BunchSprintFinish();
        PrototypeRaceEngine engine = new();

        RaceResult result = engine.RunBatch(scenario, 91234);

        int sprinterPlace = PlaceOf(result, RaceScenarioFactory.BunchSprinterId);
        int climberPlace = PlaceOf(result, RaceScenarioFactory.BunchClimberId);
        Assert.True(
            sprinterPlace > 0 && sprinterPlace < climberPlace,
            $"sprinter={sprinterPlace} climber={climberPlace} winner={result.WinnerId.Value}");
    }

    [Fact]
    public void BunchSprintSpyOnAndOffMatchFinishOrder()
    {
        RaceScenario scenario = RaceScenarioFactory.BunchSprintFinish();
        PrototypeRaceEngine engine = new();

        RaceResult off = engine.RunBatch(scenario, 91234, NullWorldSpySink.Instance);
        CollectingWorldSpySink spy = new();
        RaceResult on = engine.RunBatch(scenario, 91234, spy);

        Assert.Equal(off.FinishOrder, on.FinishOrder);
        Assert.Equal(off.Checksum, on.Checksum);
    }

    [Fact]
    public void UnclassifiedShortCourseDoesNotUseBunchSprintGate()
    {
        Assert.False(BunchSprintResolver.IsClassifiedEligible(null, 5_400.0));
        Assert.True(BunchSprintResolver.IsClassifiedEligible(ClassifiedStageType.Flat, 2_500.0));
        Assert.False(BunchSprintResolver.IsClassifiedEligible(ClassifiedStageType.Mountain, 180_000.0));
        Assert.False(BunchSprintResolver.IsClassifiedEligible(ClassifiedStageType.CobbleClassic, 260_000.0));
        Assert.True(BunchSprintResolver.MeanGradientOfLastWindow(
            RaceScenarioFactory.BunchSprintFinish().Definition) < 0.015);
    }

    [Fact]
    public void HighPeakSprinterBeatsHighCpClimberOnNoisyClassifiedFlat()
    {
        RaceScenario scenario = RaceScenarioFactory.BunchSprintFinishNoisyClassifiedFlat();
        RaceResult result = new PrototypeRaceEngine().RunBatch(scenario, 91234);
        int sprinterPlace = PlaceOf(result, RaceScenarioFactory.BunchSprinterId);
        int climberPlace = PlaceOf(result, RaceScenarioFactory.BunchClimberId);
        Assert.True(
            sprinterPlace > 0 && sprinterPlace < climberPlace,
            $"noisy classified Flat still dropped the sprinter: sprinter={sprinterPlace} climber={climberPlace}");
    }

    [Fact]
    public void ClassifiedFlatStillLaunchesWhenLastTwoKmMeanGradientIsNoisy()
    {
        RaceDefinition noisyFinish = new(
            "route.prototype.flat-noisy-finish",
            1.225,
            new[]
            {
                new RaceRouteSegment("run-in", 6_000.0, 0.0, 7.0, 4.0, 90.0),
                new RaceRouteSegment("noisy-last-2km", 2_000.0, 0.03, 7.0, 4.0, 90.0),
            });
        Assert.True(BunchSprintResolver.MeanGradientOfLastWindow(noisyFinish) >= 0.015);

        BunchSprintRiderSnapshot[] pack = Enumerable.Range(0, 12)
            .Select(index => new BunchSprintRiderSnapshot(
                new WorldEntityId(400 + index),
                GroupId: 1,
                DistanceM: 7_600.0 - (index * 0.8),
                SpeedMps: 12.0))
            .ToArray();
        Assert.True(BunchSprintResolver.ShouldLaunch(
            noisyFinish,
            ClassifiedStageType.Flat,
            leaderDistanceM: 7_600.0,
            leaderSpeedMps: 12.0,
            leaderGroupId: 1,
            pack));
        Assert.False(BunchSprintResolver.ShouldLaunch(
            noisyFinish,
            ClassifiedStageType.Hilly,
            leaderDistanceM: 7_600.0,
            leaderSpeedMps: 12.0,
            leaderGroupId: 1,
            pack));
    }

    private static int PlaceOf(RaceResult result, WorldEntityId riderId)
    {
        for (int index = 0; index < result.FinishOrder.Count; index++)
        {
            if (result.FinishOrder[index] == riderId)
            {
                return index + 1;
            }
        }

        return -1;
    }
}
