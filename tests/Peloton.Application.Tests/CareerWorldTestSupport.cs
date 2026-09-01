using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

internal static class CareerWorldTestSupport
{
    public const string BetaLeaderOriginId = "rider.race-prototype.beta-leader";
    public const string RedOrganizationOriginId = "organization.skeleton.red";

    public static WorldState CreateContractExpiryWorld(
        int contractEndDay,
        int currentDay,
        double fatigue01 = 0.0)
    {
        WorldEntityId organizationId = new(10);
        WorldEntityId personId = new(20);
        WorldEntityId riderCareerId = new(30);
        WorldEntityId contractId = new(40);
        WorldEntityId authorityId = new(50);
        Organization organization = new(organizationId, RedOrganizationOriginId, "red");
        Person person = new(personId, "Expiry Rider", BetaLeaderOriginId);
        RiderCareer riderCareer = new(
            riderCareerId,
            personId,
            organizationId,
            BetaLeaderOriginId,
            criticalPowerW: 415.0,
            wPrimeCapacityJ: 29_000.0,
            peakPowerW: 930.0,
            wPrimeRecoveryJPerSecond: 43.0,
            lowIntensityDurability: 0.92,
            highIntensityDurability: 0.90,
            bodyMassKg: 61.0,
            systemMassKg: 8.0,
            cdAM2: 0.27,
            baseCrr: 0.0038,
            positioning: 0.88,
            handling: 0.83,
            tacticalAwareness: 0.89,
            fatigue01: fatigue01);
        RiderContract contract = new(
            contractId,
            riderCareerId,
            organizationId,
            280_000,
            new WorldDate(0),
            new WorldDate(contractEndDay));
        OrganizationRaceEntry raceEntry = new(
            organizationId,
            RacePreparationDefaults.PrototypeScenarioId,
            Entered: true);

        return new WorldState(
            worldId: "contract-expiry-test",
            masterSeed: 1,
            rngContractVersion: 1,
            new WorldDate(currentDay),
            new ContentIdentity(
                "peloton.skeleton",
                "0.1.0",
                1,
                "scenario.peloton.skeleton",
                "Dynamic",
                "Advanced",
                "Guessed",
                "test-hash"),
            rulesIdentity: "test-rules",
            rulesModules: Array.Empty<RulesModuleIdentity>(),
            entityIdHighWaterMark: 50,
            new[] { person },
            Array.Empty<ManagerCareer>(),
            Array.Empty<Employment>(),
            new[] { organization },
            new[] { new DecisionAuthority(authorityId, DecisionAuthorityKind.HumanInput) },
            calendarEntries: new[]
            {
                new CalendarEntry(
                    new WorldEntityId(60),
                    12,
                    CalendarEntryKind.Race,
                    "Skeleton race",
                    RaceContentId: RacePreparationDefaults.PrototypeScenarioId),
            },
            riderCareers: new[] { riderCareer },
            organizationRaceEntries: new[] { raceEntry },
            riderContracts: new[] { contract });
    }

    public static RiderCareer CreateSampleCareer(
        double form01 = 1.0,
        double freshness01 = 1.0,
        double fatigue01 = 0.0)
    {
        return new RiderCareer(
            new WorldEntityId(1),
            new WorldEntityId(2),
            new WorldEntityId(3),
            BetaLeaderOriginId,
            criticalPowerW: 415.0,
            wPrimeCapacityJ: 29_000.0,
            peakPowerW: 930.0,
            wPrimeRecoveryJPerSecond: 43.0,
            lowIntensityDurability: 0.92,
            highIntensityDurability: 0.90,
            bodyMassKg: 61.0,
            systemMassKg: 8.0,
            cdAM2: 0.27,
            baseCrr: 0.0038,
            positioning: 0.88,
            handling: 0.83,
            tacticalAwareness: 0.89,
            form01,
            freshness01,
            fatigue01);
    }

    public static (double Form01, double Freshness01, double Fatigue01)[] DayStateSnapshot(GameApplication application) =>
        application.World!.RiderCareers
            .OrderBy(career => career.Id.Value)
            .Select(career => (career.Form01, career.Freshness01, career.Fatigue01))
            .ToArray();

    public static WorldEntityId BetaLeaderCareerId(GameApplication application) =>
        FindRiderCareer(application, BetaLeaderOriginId).Id;

    public static long[] EmployerSquadCareerIds(GameApplication application)
    {
        AccessContext access = application.GetAccessContext();
        WorldEntityId organizationId = access.CurrentOrganizationId
            ?? throw new InvalidOperationException("Test world has no employer.");
        return application.World!
            .GetRiderCareersForOrganization(organizationId)
            .Select(career => career.Id.Value)
            .ToArray();
    }

    public static void AssertFinishOrderUsesWorldRiderCareers(GameApplication application)
    {
        foreach (WorldEntityId riderId in application.World!.LastRace!.FinishOrder)
        {
            Assert.NotNull(application.World.TryGetRiderCareer(riderId));
        }
    }

    private static RiderCareer FindRiderCareer(GameApplication application, string originDefinitionId)
    {
        return application.World!.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, originDefinitionId, StringComparison.Ordinal));
    }
}
