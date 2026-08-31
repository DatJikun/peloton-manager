using System;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

internal static class CareerWorldTestSupport
{
    public const string BetaLeaderOriginId = "rider.race-prototype.beta-leader";

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
