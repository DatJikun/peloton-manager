using System;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class TimeTrialSessionTests
{
    [Fact]
    public void IndividualTimeTrialUsesTtCdANoShelterAndIncreasingTimes()
    {
        RaceRiderProfile ttSpecialist = RaceScenarioFactory.Profile(
            11,
            101,
            criticalPowerW: 420.0,
            wPrimeCapacityJ: 24_000.0,
            peakPowerW: 1100.0,
            durability: 0.90,
            cdAM2: 0.30,
            cdATtM2: 0.20,
            timeTrialStage: true);
        RaceRiderProfile gcRider = RaceScenarioFactory.Profile(
            12,
            102,
            criticalPowerW: 410.0,
            wPrimeCapacityJ: 28_000.0,
            peakPowerW: 1050.0,
            durability: 0.90,
            cdAM2: 0.27,
            cdATtM2: 0.22,
            timeTrialStage: true);
        RaceRiderProfile sprinter = RaceScenarioFactory.Profile(
            13,
            103,
            criticalPowerW: 370.0,
            wPrimeCapacityJ: 32_000.0,
            peakPowerW: 1600.0,
            durability: 0.82,
            cdAM2: 0.32,
            cdATtM2: 0.28,
            timeTrialStage: true);
        RaceRiderProfile[] riders = { ttSpecialist, gcRider, sprinter };
        RaceStartingPosition[] positions = riders
            .OrderByDescending(rider => rider.RiderId.Value)
            .Select((rider, index) => new RaceStartingPosition(rider.RiderId, 0.0, index * 60))
            .ToArray();
        RaceDefinition definition = new(
            "route.itt.short",
            1.225,
            new[]
            {
                new RaceRouteSegment(
                    "segment.itt",
                    lengthM: 4_000.0,
                    gradient: 0.0,
                    roadWidthM: 6.0,
                    windSpeedMps: 0.0,
                    windYawDegrees: 0.0),
            });
        RaceScenario scenario = new(
            "race.itt.short",
            definition,
            riders,
            positions,
            Array.Empty<RaceCommand>(),
            initialSpeedMps: 11.0,
            maximumDurationSeconds: 3_600,
            classifiedStageType: ClassifiedStageType.IndividualTimeTrial);

        RaceResult result = new PrototypeRaceEngine().RunBatch(scenario, 91234);

        Assert.Equal(ttSpecialist.RiderId, result.WinnerId);
        double[] finishTimes = result.RiderMetrics
            .OrderBy(metric => metric.FinishTimeSeconds)
            .Select(metric => metric.FinishTimeSeconds)
            .ToArray();
        Assert.Equal(3, finishTimes.Length);
        Assert.True(finishTimes[0] < finishTimes[1] - 0.01);
        Assert.True(finishTimes[1] < finishTimes[2] - 0.01);
        Assert.Equal(0, result.RiderMetrics.Sum(metric => metric.LostShelterTransitions));
        Assert.Equal(ttSpecialist.CdATtM2, ttSpecialist.CdAM2);
        Assert.Equal(0.20, ttSpecialist.CdAM2);
    }

    [Fact]
    public void TeamTimeTrialUsesFourthRiderTime()
    {
        RaceRiderProfile[] teamA =
        {
            RaceScenarioFactory.Profile(21, 201, 430, 24_000, 1100, 0.9, cdAM2: 0.24, cdATtM2: 0.18, timeTrialStage: true),
            RaceScenarioFactory.Profile(22, 201, 420, 24_000, 1100, 0.9, cdAM2: 0.24, cdATtM2: 0.18, timeTrialStage: true),
            RaceScenarioFactory.Profile(23, 201, 410, 24_000, 1100, 0.9, cdAM2: 0.24, cdATtM2: 0.18, timeTrialStage: true),
            RaceScenarioFactory.Profile(24, 201, 400, 24_000, 1100, 0.9, cdAM2: 0.24, cdATtM2: 0.18, timeTrialStage: true),
        };
        RaceRiderProfile[] teamB =
        {
            RaceScenarioFactory.Profile(31, 202, 390, 24_000, 1000, 0.85, cdAM2: 0.28, cdATtM2: 0.24, timeTrialStage: true),
            RaceScenarioFactory.Profile(32, 202, 380, 24_000, 1000, 0.85, cdAM2: 0.28, cdATtM2: 0.24, timeTrialStage: true),
            RaceScenarioFactory.Profile(33, 202, 370, 24_000, 1000, 0.85, cdAM2: 0.28, cdATtM2: 0.24, timeTrialStage: true),
            RaceScenarioFactory.Profile(34, 202, 360, 24_000, 1000, 0.85, cdAM2: 0.28, cdATtM2: 0.24, timeTrialStage: true),
        };
        RaceRiderProfile[] riders = teamA.Concat(teamB).ToArray();
        RaceStartingPosition[] positions = riders
            .Select(rider => new RaceStartingPosition(rider.RiderId, 0.0))
            .ToArray();
        RaceDefinition definition = new(
            "route.ttt.short",
            1.225,
            new[]
            {
                new RaceRouteSegment(
                    "segment.ttt",
                    lengthM: 3_000.0,
                    gradient: 0.0,
                    roadWidthM: 8.0,
                    windSpeedMps: 0.0,
                    windYawDegrees: 0.0),
            });
        RaceScenario scenario = new(
            "race.ttt.short",
            definition,
            riders,
            positions,
            Array.Empty<RaceCommand>(),
            initialSpeedMps: 11.0,
            maximumDurationSeconds: 2_400,
            classifiedStageType: ClassifiedStageType.TeamTimeTrial);

        RaceResult result = new PrototypeRaceEngine().RunBatch(scenario, 91234);
        Assert.Equal(new WorldEntityId(201), result.RiderMetrics.First(metric => metric.RiderId == result.WinnerId).OrganizationId);
    }
}
