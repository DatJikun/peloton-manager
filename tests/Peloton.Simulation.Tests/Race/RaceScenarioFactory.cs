using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Simulation.Tests.Race;

internal static class RaceScenarioFactory
{
    private static readonly int[] RepeatedAttackSeconds = { 20, 65, 110 };
    private static readonly int[] DurabilityPaceSeconds = { 5, 95, 185, 275, 365 };

    public static readonly WorldEntityId WeakRiderId = new(12);
    public static readonly WorldEntityId ExposedWeakRiderId = new(14);

    public static RaceScenario Basic()
    {
        RaceRiderProfile[] riders =
        {
            Profile(11, 101, 370.0, 24_000.0, 900.0, 0.82),
            Profile(12, 101, 350.0, 21_000.0, 850.0, 0.75),
            Profile(13, 102, 345.0, 20_000.0, 840.0, 0.70),
        };
        RaceDefinition definition = new(
            "route.prototype.basic",
            1.225,
            new[]
            {
                new RaceRouteSegment(
                    "segment.flat",
                    lengthM: 1_500.0,
                    gradient: 0.0,
                    roadWidthM: 6.0,
                    windSpeedMps: 1.0,
                    windYawDegrees: 0.0),
            });
        RaceStartingPosition[] positions = riders
            .Select((rider, index) => new RaceStartingPosition(
                rider.RiderId,
                distanceM: (riders.Length - 1 - index) * 0.7))
            .ToArray();
        return new RaceScenario(
            "race.prototype.basic",
            definition,
            riders,
            positions,
            new[]
            {
                new RaceCommand(
                    simulationSecond: 20,
                    riders[0].OrganizationId,
                    riders[0].RiderId,
                    RaceCommandKind.ForcePace),
            },
            initialSpeedMps: 11.0,
            maximumDurationSeconds: 600);
    }

    public static readonly WorldEntityId BunchSprinterId = new(201);
    public static readonly WorldEntityId BunchClimberId = new(202);

    public static RaceScenario BunchSprintFinish()
    {
        List<RaceRiderProfile> riders = new()
        {
            Profile(
                BunchSprinterId.Value,
                301,
                criticalPowerW: 370.0,
                wPrimeCapacityJ: 35_000.0,
                peakPowerW: 1_250.0,
                durability: 0.84,
                positioning: 0.92,
                massKg: 75.0,
                cdAM2: 0.32),
            Profile(
                BunchClimberId.Value,
                302,
                criticalPowerW: 430.0,
                wPrimeCapacityJ: 25_000.0,
                peakPowerW: 900.0,
                durability: 0.90,
                positioning: 0.70,
                massKg: 66.0,
                cdAM2: 0.265),
        };
        for (int index = 0; index < 10; index++)
        {
            riders.Add(Profile(
                203 + index,
                301 + (index % 2),
                criticalPowerW: 360.0,
                wPrimeCapacityJ: 24_000.0,
                peakPowerW: 980.0,
                durability: 0.80,
                positioning: 0.72,
                massKg: 70.0,
                cdAM2: 0.30));
        }

        RaceDefinition definition = new(
            "route.prototype.bunch-sprint",
            1.225,
            new[]
            {
                new RaceRouteSegment(
                    "segment.flat-finish",
                    lengthM: 2_500.0,
                    gradient: 0.0,
                    roadWidthM: 8.0,
                    windSpeedMps: 0.5,
                    windYawDegrees: 0.0),
            });
        RaceStartingPosition[] positions = riders
            .Select((rider, index) => new RaceStartingPosition(
                rider.RiderId,
                (riders.Count - 1 - index) * 0.7))
            .ToArray();
        return new RaceScenario(
            "race.prototype.bunch-sprint",
            definition,
            riders,
            positions,
            Array.Empty<RaceCommand>(),
            initialSpeedMps: 11.0,
            maximumDurationSeconds: 800,
            classifiedStageType: ClassifiedStageType.Flat);
    }

    public static RaceRiderProfile Profile(
        long riderId,
        long organizationId,
        double criticalPowerW,
        double wPrimeCapacityJ,
        double peakPowerW,
        double durability,
        double positioning = 0.7,
        double massKg = 70.0,
        double cdAM2 = 0.31)
    {
        return new RaceRiderProfile(
            new WorldEntityId(riderId),
            new WorldEntityId(organizationId),
            criticalPowerW,
            wPrimeCapacityJ,
            peakPowerW,
            wPrimeRecoveryJPerSecond: 40.0,
            lowIntensityDurability: durability,
            highIntensityDurability: durability,
            bodyMassKg: massKg,
            systemMassKg: 8.0,
            cdAM2,
            baseCrr: 0.004,
            positioning,
            handling: 0.7,
            tacticalAwareness: 0.7);
    }

    public static RaceScenario DraftingPosition()
    {
        RaceRiderProfile strong = Profile(11, 101, 410.0, 28_000.0, 950.0, 0.85);
        RaceRiderProfile shelteredWeak = Profile(
            WeakRiderId.Value,
            102,
            330.0,
            25_000.0,
            880.0,
            0.70);
        RaceRiderProfile exposedWeak = Profile(
            ExposedWeakRiderId.Value,
            104,
            330.0,
            25_000.0,
            880.0,
            0.70);
        RaceRiderProfile steady = Profile(13, 103, 350.0, 22_000.0, 860.0, 0.78);
        RaceRiderProfile[] riders = { strong, shelteredWeak, steady, exposedWeak };
        return Scenario(
            "race.proof.drafting-position",
            riders,
            new long[] { 11, 13, 12, 14 },
            lengthM: 1_800.0,
            gradient: 0.0,
            roadWidthM: 1.6,
            windSpeedMps: 7.0,
            windYawDegrees: 90.0,
            new[]
            {
                new RaceCommand(5, strong.OrganizationId, strong.RiderId, RaceCommandKind.ForcePace),
            });
    }

    public static RaceScenario RepeatedAttacks()
    {
        RaceRiderProfile highReserve = Profile(21, 201, 355.0, 45_000.0, 920.0, 0.85);
        RaceRiderProfile lowReserve = Profile(22, 202, 355.0, 11_000.0, 920.0, 0.85);
        RaceCommand[] commands = RepeatedAttackSeconds
            .Select(second => new RaceCommand(
                second,
                highReserve.OrganizationId,
                highReserve.RiderId,
                RaceCommandKind.Attack))
            .ToArray();
        return Scenario(
            "race.proof.repeated-attacks",
            new[] { highReserve, lowReserve },
            new long[] { 21, 22 },
            lengthM: 4_000.0,
            gradient: 0.0,
            roadWidthM: 6.0,
            windSpeedMps: 1.0,
            windYawDegrees: 0.0,
            commands);
    }

    public static RaceScenario DurabilitySolo(bool durable)
    {
        RaceRiderProfile rider = Profile(
            durable ? 31 : 32,
            durable ? 301 : 302,
            360.0,
            30_000.0,
            900.0,
            durable ? 0.95 : 0.25);
        RaceCommand[] commands = DurabilityPaceSeconds
            .Select(second => new RaceCommand(
                second,
                rider.OrganizationId,
                rider.RiderId,
                RaceCommandKind.ForcePace))
            .ToArray();
        return Scenario(
            durable ? "race.proof.durability.durable" : "race.proof.durability.fragile",
            new[] { rider },
            new[] { rider.RiderId.Value },
            lengthM: 4_500.0,
            gradient: 0.025,
            roadWidthM: 6.0,
            windSpeedMps: 0.0,
            windYawDegrees: 0.0,
            commands,
            maximumDurationSeconds: 3_000);
    }

    public static RaceScenario NaturalDrop()
    {
        RaceRiderProfile front = Profile(41, 401, 430.0, 30_000.0, 980.0, 0.90);
        RaceRiderProfile middle = Profile(42, 402, 355.0, 20_000.0, 850.0, 0.78);
        RaceRiderProfile rear = Profile(43, 403, 255.0, 8_000.0, 650.0, 0.55);
        return Scenario(
            "race.proof.natural-drop",
            new[] { front, middle, rear },
            new long[] { 41, 42, 43 },
            lengthM: 3_000.0,
            gradient: 0.0,
            roadWidthM: 5.5,
            windSpeedMps: 3.0,
            windYawDegrees: 20.0,
            new[]
            {
                new RaceCommand(10, front.OrganizationId, front.RiderId, RaceCommandKind.ForcePace),
                new RaceCommand(100, front.OrganizationId, front.RiderId, RaceCommandKind.ForcePace),
            });
    }

    public static RaceScenario Crosswind()
    {
        List<RaceRiderProfile> riders = new();
        for (int index = 0; index < 12; index++)
        {
            riders.Add(Profile(
                51 + index,
                501 + (index / 4),
                criticalPowerW: index == 0 ? 430.0 : 310.0,
                wPrimeCapacityJ: index == 0 ? 30_000.0 : 13_000.0,
                peakPowerW: index == 0 ? 980.0 : 760.0,
                durability: 0.75,
                positioning: index < 5 ? 0.8 : 0.45));
        }

        RaceRiderProfile front = riders[0];
        return Scenario(
            "race.proof.crosswind",
            riders,
            riders.Select(rider => rider.RiderId.Value).ToArray(),
            lengthM: 3_500.0,
            gradient: 0.0,
            roadWidthM: 3.2,
            windSpeedMps: 11.0,
            windYawDegrees: 90.0,
            new[]
            {
                new RaceCommand(5, front.OrganizationId, front.RiderId, RaceCommandKind.ForcePace),
                new RaceCommand(95, front.OrganizationId, front.RiderId, RaceCommandKind.ForcePace),
            });
    }

    private static RaceScenario Scenario(
        string id,
        IReadOnlyList<RaceRiderProfile> riders,
        long[] frontToBackRiderIds,
        double lengthM,
        double gradient,
        double roadWidthM,
        double windSpeedMps,
        double windYawDegrees,
        IReadOnlyList<RaceCommand> commands,
        int maximumDurationSeconds = 1_000)
    {
        Dictionary<long, RaceRiderProfile> byId = riders.ToDictionary(rider => rider.RiderId.Value);
        RaceStartingPosition[] positions = frontToBackRiderIds
            .Select((idValue, index) => new RaceStartingPosition(
                byId[idValue].RiderId,
                (frontToBackRiderIds.Length - 1 - index) * 0.7))
            .ToArray();
        RaceDefinition definition = new(
            $"route.{id}",
            1.225,
            new[]
            {
                new RaceRouteSegment(
                    $"segment.{id}",
                    lengthM,
                    gradient,
                    roadWidthM,
                    windSpeedMps,
                    windYawDegrees),
            });
        return new RaceScenario(
            id,
            definition,
            riders,
            positions,
            commands,
            initialSpeedMps: 11.0,
            maximumDurationSeconds);
    }

    public static RaceResult RunEveryStep(IRaceEngine engine, RaceScenario scenario, long seed)
    {
        RaceSession session = engine.CreateSession(scenario, seed);
        while (!session.IsCompleted)
        {
            RaceStepResult step = session.Step();
            if (step.Status == RaceStepStatus.DecisionRequired)
            {
                throw new Xunit.Sdk.XunitException("Physical scenario unexpectedly requested a decision.");
            }
        }

        return session.Result!;
    }
}
