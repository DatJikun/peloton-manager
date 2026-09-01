using System;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class PositionAndSelectionTests
{
    [Fact]
    public void HigherPositioningEndsAheadAfterFlatDrift()
    {
        RaceRiderProfile front = RaceScenarioFactory.Profile(
            501,
            601,
            criticalPowerW: 360.0,
            wPrimeCapacityJ: 24_000.0,
            peakPowerW: 900.0,
            durability: 0.82,
            positioning: 0.95);
        RaceRiderProfile rear = RaceScenarioFactory.Profile(
            502,
            602,
            criticalPowerW: 360.0,
            wPrimeCapacityJ: 24_000.0,
            peakPowerW: 900.0,
            durability: 0.82,
            positioning: 0.80);
        RaceDefinition definition = new(
            "route.positioning.flat",
            1.225,
            new[]
            {
                new RaceRouteSegment(
                    "segment.flat",
                    lengthM: 20_000.0,
                    gradient: 0.0,
                    roadWidthM: 8.0,
                    windSpeedMps: 0.0,
                    windYawDegrees: 0.0),
            });
        RaceStartingPosition[] positions =
        {
            new(rear.RiderId, 0.0),
            new(front.RiderId, RaceTuning.SlotSpacingM),
        };
        RaceScenario scenario = new(
            "race.positioning.flat",
            definition,
            new[] { front, rear },
            positions,
            Array.Empty<RaceCommand>(),
            initialSpeedMps: 11.0,
            maximumDurationSeconds: 1_000,
            classifiedStageType: ClassifiedStageType.Flat);
        PrototypeRaceEngine engine = new();
        RaceSession session = engine.CreateSession(scenario, 77);
        for (int step = 0; step < 600 && !session.IsCompleted; step++)
        {
            RaceStepResult result = session.Step();
            if (result.Status == RaceStepStatus.DecisionRequired)
            {
                throw new Xunit.Sdk.XunitException("Unexpected decision request.");
            }
        }

        RiderRuntimeState frontState = Find(session, front.RiderId);
        RiderRuntimeState rearState = Find(session, rear.RiderId);
        Assert.True(frontState.DistanceM > rearState.DistanceM);
    }

    [Fact]
    public void CobbleShelterAndSurgePenalizeLowHandling()
    {
        const double shelter = 0.62;
        double highHandlingShelter = PositionScoreResolver.CobbleShelterMultiplier(shelter, 0.93);
        double lowHandlingShelter = PositionScoreResolver.CobbleShelterMultiplier(shelter, 0.80);
        Assert.True(highHandlingShelter > shelter);
        Assert.True(lowHandlingShelter > highHandlingShelter);

        double highHandlingSurge = PositionScoreResolver.CobbleSurgeMultiplier(0.93);
        double lowHandlingSurge = PositionScoreResolver.CobbleSurgeMultiplier(0.80);
        Assert.Equal(1.0 + (RaceTuning.CobbleSurgeCost * 0.07), highHandlingSurge, precision: 6);
        Assert.Equal(1.0 + (RaceTuning.CobbleSurgeCost * 0.20), lowHandlingSurge, precision: 6);
        Assert.True(lowHandlingSurge > highHandlingSurge);

        RaceRiderProfile highHandler = RaceScenarioFactory.Profile(
            701,
            801,
            criticalPowerW: 360.0,
            wPrimeCapacityJ: 24_000.0,
            peakPowerW: 900.0,
            durability: 0.82,
            positioning: 0.8,
            massKg: 70.0,
            cdAM2: 0.31);
        RaceRiderProfile lowHandler = RaceScenarioFactory.Profile(
            702,
            802,
            criticalPowerW: 360.0,
            wPrimeCapacityJ: 24_000.0,
            peakPowerW: 900.0,
            durability: 0.82,
            positioning: 0.8,
            massKg: 70.0,
            cdAM2: 0.31);
        highHandler = new RaceRiderProfile(
            highHandler.RiderId,
            highHandler.OrganizationId,
            highHandler.CriticalPowerW,
            highHandler.WPrimeCapacityJ,
            highHandler.PeakPowerW,
            highHandler.WPrimeRecoveryJPerSecond,
            highHandler.LowIntensityDurability,
            highHandler.HighIntensityDurability,
            highHandler.BodyMassKg,
            highHandler.SystemMassKg,
            highHandler.CdAM2,
            highHandler.BaseCrr,
            highHandler.Positioning,
            handling: 0.93,
            highHandler.TacticalAwareness);
        lowHandler = new RaceRiderProfile(
            lowHandler.RiderId,
            lowHandler.OrganizationId,
            lowHandler.CriticalPowerW,
            lowHandler.WPrimeCapacityJ,
            lowHandler.PeakPowerW,
            lowHandler.WPrimeRecoveryJPerSecond,
            lowHandler.LowIntensityDurability,
            lowHandler.HighIntensityDurability,
            lowHandler.BodyMassKg,
            lowHandler.SystemMassKg,
            lowHandler.CdAM2,
            lowHandler.BaseCrr,
            lowHandler.Positioning,
            handling: 0.80,
            lowHandler.TacticalAwareness);

        double speedMps = 10.0;
        RequiredPowerBreakdown highDemand = RequiredPowerAtCobbleSpeed(highHandler, speedMps);
        RequiredPowerBreakdown lowDemand = RequiredPowerAtCobbleSpeed(lowHandler, speedMps);
        Assert.True(lowDemand.TotalPowerW > highDemand.TotalPowerW);
    }

    private static RequiredPowerBreakdown RequiredPowerAtCobbleSpeed(RaceRiderProfile profile, double speedMps)
    {
        double shelter = PositionScoreResolver.CobbleShelterMultiplier(0.62, profile.Handling);
        double surge = PositionScoreResolver.CobbleSurgeMultiplier(profile.Handling);
        double crr = profile.BaseCrr + (0.0085 * (1.35 - (0.50 * profile.Handling)));
        RequiredPowerBreakdown demand = RequiredPowerSolver.Calculate(new RequiredPowerInput(
            speedMps,
            0.0,
            0.0,
            1.225,
            speedMps,
            profile.CdAM2,
            shelter,
            crr,
            profile.TotalMassKg));
        return demand with
        {
            AerodynamicPowerW = demand.AerodynamicPowerW * surge,
            RollingPowerW = demand.RollingPowerW * surge,
            GravityPowerW = demand.GravityPowerW * surge,
            AccelerationPowerW = demand.AccelerationPowerW * surge,
        };
    }

    private static RiderRuntimeState Find(RaceSession session, WorldEntityId riderId)
    {
        RaceMotionSnapshot snapshot = session.GetMotionSnapshot();
        RaceRiderMotion motion = snapshot.Riders.Single(rider => rider.RiderId == riderId);
        return new RiderRuntimeState(motion.DistanceM);
    }

    private sealed class RiderRuntimeState
    {
        public RiderRuntimeState(double distanceM)
        {
            DistanceM = distanceM;
        }

        public double DistanceM { get; }
    }
}
