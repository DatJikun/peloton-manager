using System;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

internal static class CareerWorldTestSupport
{
    public const string BetaLeaderOriginId = "rider.race-prototype.beta-leader";

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
