using System;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerSeasonAiContractTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string UaeOriginId = "organization.wt2026.uae";
    private const long GateSeed = 91234;

    [Fact]
    public void EveryAiClubHasAtLeastEightRidersOnSecondJanuary2027()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        AdvanceToDay(application, 366);
        WorldState world = application.World!;
        Assert.Equal(2027, world.SeasonYear);
        WorldEntityId? playerOrg = application.GetAccessContext().CurrentOrganizationId;
        foreach (Organization organization in world.Organizations)
        {
            if (playerOrg is WorldEntityId playerId && organization.Id == playerId)
            {
                continue;
            }

            int squadSize = world.GetRiderCareersForOrganization(organization.Id).Count;
            Assert.True(
                squadSize >= SeasonAiContracts.MinimumSquadSize,
                $"{organization.OriginDefinitionId} has {squadSize} riders");
        }
    }

    [Fact]
    public void PlayerClubContractsUntouchedByAiRolloverRule()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        WorldState world = application.World!;
        WorldEntityId playerOrg = application.GetAccessContext().CurrentOrganizationId!.Value;
        RiderContract[] before = world.RiderContracts
            .Where(contract => contract.OrganizationId == playerOrg)
            .OrderBy(contract => contract.Id.Value)
            .ToArray();
        int[] beforeWages = before.Select(contract => contract.AnnualWage).ToArray();
        int[] beforeEnds = before.Select(contract => contract.EndDate.DayNumber).ToArray();
        AdvanceToDay(application, 366);
        RiderContract[] after = world.RiderContracts
            .Where(contract => contract.OrganizationId == playerOrg)
            .OrderBy(contract => contract.Id.Value)
            .ToArray();
        Assert.Equal(before.Length, after.Length);
        Assert.Equal(beforeWages, after.Select(contract => contract.AnnualWage).ToArray());
        Assert.Equal(beforeEnds, after.Select(contract => contract.EndDate.DayNumber).ToArray());
    }

    private static GameApplication CreateWorldSkippingPlayerRaces()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed, UaeOriginId)).Succeeded);
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        foreach (PreSeasonRaceEntryProjection race in application.PreSeasonPlanning!.Races)
        {
            Assert.True(application.Execute(new SetSeasonRaceEntryCommand(race.RaceContentId, Entered: false)).Succeeded);
        }

        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);
        return application;
    }

    private static void AdvanceToDay(GameApplication application, int dayNumber)
    {
        WorldState world = application.World!;
        while (world.CurrentDate.DayNumber < dayNumber)
        {
            if (application.State == GameState.PreSeasonPlanningFlow)
            {
                Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
                foreach (PreSeasonRaceEntryProjection race in application.PreSeasonPlanning!.Races)
                {
                    Assert.True(application.Execute(new SetSeasonRaceEntryCommand(race.RaceContentId, Entered: false)).Succeeded);
                }

                Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);
            }

            world.AdvanceOneDay();
        }
    }
}
