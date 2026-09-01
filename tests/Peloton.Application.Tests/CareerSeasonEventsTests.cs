using System;
using System.Linq;
using Peloton.Application;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerSeasonEventsTests
{
    [Fact]
    public void WorldTourGroupedEventsAreThirtySixAndUpcomingShowsFiveWholeRaces()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(
            new CreateWorldCommand("scenario.peloton.wt-2026", 91234, "organization.wt2026.ineos")).Succeeded);
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);

        Assert.Equal(36, application.SeasonEvents.Count);
        Assert.Equal(5, application.UpcomingEvents.Count);
        Assert.Contains("Tour Down Under", application.UpcomingEvents[0].Name, StringComparison.Ordinal);
        Assert.All(application.UpcomingEvents, item => Assert.DoesNotContain("Stage", item.Name, StringComparison.Ordinal));
        Assert.All(application.SeasonEvents, item => Assert.DoesNotContain("Stage", item.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void ManagementInboxOnDayZeroIsEmpty()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(
            new CreateWorldCommand("scenario.peloton.wt-2026", 91234, "organization.wt2026.ineos")).Succeeded);
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);
        Assert.Equal(GameState.Management, application.State);
        Assert.Equal(0, application.CareerDay!.DayNumber);
        Assert.Empty(application.Inbox);
    }
}
