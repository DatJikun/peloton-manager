using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerWorldTourPhase3Tests
{
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";
    private const long GateSeed = 91234;

    [Fact]
    public void ConfirmWithoutStrategyIsRejected()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);

        CommandResult confirm = application.Execute(new ConfirmRacePreparationPlanCommand());
        Assert.False(confirm.Succeeded);
        Assert.Equal("PREP_STRATEGY_INCOMPLETE", confirm.ReasonCode);
    }

    [Fact]
    public void SkippedRaceDoesNotBlockAdvanceDayAndExcludesPlayerRidersFromStartList()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        AccessContext access = application.GetAccessContext();
        WorldEntityId employerId = access.CurrentOrganizationId!.Value;
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(application.Execute(new SetSeasonRaceEntryCommand(PrototypeRaceScenarioId, false)).Succeeded);
        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);

        for (int day = 0; day < 12; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        Assert.Equal(13, application.World!.CurrentDate.DayNumber);
        Assert.Equal(1, application.World.RaceCount);
        Assert.False(application.CareerDay!.RaceDueToday);
        Assert.Equal(HubPrimaryActionIds.AdvanceDay, application.CareerDay.PrimaryAction);

        WorldEntityId[] employerRiderIds = application.World.GetRiderCareersForOrganization(employerId)
            .Select(career => career.Id)
            .ToArray();
        Assert.All(employerRiderIds, riderId =>
            Assert.DoesNotContain(riderId, application.World.LastRace!.FinishOrder));
    }

    [Fact]
    public void CancelPreSeasonPlanningDiscardsDraft()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(application.Execute(new SetSeasonRaceEntryCommand(PrototypeRaceScenarioId, false)).Succeeded);
        Assert.True(application.Execute(new CancelPreSeasonPlanningCommand()).Succeeded);
        Assert.Equal(GameState.Management, application.State);
        Assert.Null(application.PreSeasonPlanning);

        Assert.True(application.World!.IsOrganizationEnteredForRace(
            application.GetAccessContext().CurrentOrganizationId!.Value,
            PrototypeRaceScenarioId));
    }

    [Fact]
    public void StrategyChangesAssembledTacticalPlanForPlayerOrganization()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        AccessContext access = application.GetAccessContext();
        WorldEntityId employerId = access.CurrentOrganizationId!.Value;
        WorldEntityId[] squad = application.World!.GetRiderCareersForOrganization(employerId)
            .OrderBy(career => career.Id.Value)
            .Select(career => career.Id)
            .Take(2)
            .ToArray();

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new SetRacePreparationStrategyCommand(
            squad[0],
            squad[1],
            RaceObjective.StageWin,
            RaceBriefingKind.Chase)).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot)
            .Resolve(application.World.ContentIdentity.ScenarioId);
        RaceScenarioTemplate template = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
            .ResolveTemplate(PrototypeRaceScenarioId);
        RaceScenario scenario = WorldRaceScenarioAssembler.Assemble(
            application.World,
            recipe,
            template,
            PrototypeRaceScenarioId,
            new RacePreparationStrategy(squad[0], squad[1], RaceObjective.StageWin, RaceBriefingKind.Chase),
            employerId);

        RaceTacticalPlan playerPlan = Assert.Single(
            scenario.TacticalPlans,
            plan => plan.Observation.OrganizationId == employerId);
        Assert.Equal(squad[1], playerPlan.SupportRiderId);
        Assert.Equal(RaceObjective.StageWin, playerPlan.Observation.Objective);
        Assert.Equal(RaceBriefingKind.Chase, playerPlan.Briefing.Kind);
    }

    [Fact]
    public void DefaultWorldStillCompletesTenSeasonRunner()
    {
        string checksum = TestApplication.RunTenSeasons(GateSeed);
        Assert.False(string.IsNullOrWhiteSpace(checksum));
    }
}
