using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerSeasonInboxTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string UaeOriginId = "organization.wt2026.uae";
    private const long GateSeed = 91234;

    [Fact]
    public void SeasonSummaryAppearsAfterRolloverAndCanBeDismissed()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        AdvanceToNewYear(application);
        InboxItemProjection? summary = application.Inbox
            .FirstOrDefault(item => item.Category == "season-summary");
        Assert.NotNull(summary);
        Assert.Contains("2026", summary.Body);
        Assert.True(application.Execute(new ArchiveInboxItemCommand(summary.Identity)).Succeeded);
        Assert.DoesNotContain(application.Inbox, item => item.Identity == summary.Identity);
    }

    [Fact]
    public void ContractExpiryWarningAppearsSixtyDaysBeforeEnd()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        WorldState world = application.World!;
        WorldEntityId playerOrg = application.GetAccessContext().CurrentOrganizationId!.Value;
        RiderCareer rider = world.GetRiderCareersForOrganization(playerOrg)[0];
        int warningDay = 200;
        int endDay = warningDay + 60;
        world.TryTerminateActiveContract(rider.Id, new WorldDate(0));
        world.AddRiderContract(new RiderContract(
            world.AllocateEntityId(),
            rider.Id,
            playerOrg,
            250_000,
            new WorldDate(0),
            new WorldDate(endDay)));
        while (world.CurrentDate.DayNumber < warningDay)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }

        InboxItemProjection? warning = application.Inbox
            .FirstOrDefault(item => item.Category == "contract-expiry");
        Assert.NotNull(warning);
        Assert.Contains("60", warning.Body);
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

    private static void AdvanceToNewYear(GameApplication application)
    {
        WorldState world = application.World!;
        while (world.CurrentDate.DayNumber < 365)
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
