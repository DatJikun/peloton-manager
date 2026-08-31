using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerWorldTourPhase2Tests
{
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";
    private const long GateSeed = 91234;

    [Fact]
    public void RestTickReducesFatigueForTiredRider()
    {
        RiderCareer career = CareerWorldTestSupport.CreateSampleCareer(fatigue01: 0.5);

        career.ApplyRestTick();

        Assert.Equal(0.41, career.Fatigue01, precision: 10);
        Assert.True(career.Fatigue01 < 0.5);
    }

    [Fact]
    public void SimulateRaceRaisesFatigueAndLowersFreshnessOnStarters()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        (double Form01, double Freshness01, double Fatigue01)[] before =
            CareerWorldTestSupport.DayStateSnapshot(application);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        (double Form01, double Freshness01, double Fatigue01)[] after =
            CareerWorldTestSupport.DayStateSnapshot(application);
        Assert.Equal(before.Length, after.Length);
        for (int index = 0; index < before.Length; index++)
        {
            Assert.True(after[index].Fatigue01 > before[index].Fatigue01);
            Assert.True(after[index].Freshness01 < before[index].Freshness01);
            Assert.True(after[index].Form01 < before[index].Form01);
        }
    }

    [Fact]
    public void SameSeedProducesSameFormTrajectory()
    {
        (double Form01, double Freshness01, double Fatigue01)[] first = RunFormTrajectory(GateSeed);
        (double Form01, double Freshness01, double Fatigue01)[] second = RunFormTrajectory(GateSeed);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ReadinessScalesCriticalPowerInRaceProfile()
    {
        RiderCareer lowForm = CareerWorldTestSupport.CreateSampleCareer(form01: 0.2);
        RiderCareer highForm = CareerWorldTestSupport.CreateSampleCareer(form01: 1.0);

        RaceRiderProfile lowProfile = WorldRaceScenarioAssembler.ToRaceProfile(lowForm);
        RaceRiderProfile highProfile = WorldRaceScenarioAssembler.ToRaceProfile(highForm);

        Assert.NotEqual(lowProfile.CriticalPowerW, highProfile.CriticalPowerW);
        Assert.True(lowProfile.CriticalPowerW < highProfile.CriticalPowerW);
        Assert.True(lowProfile.PeakPowerW <= highProfile.PeakPowerW);
        Assert.True(lowProfile.PeakPowerW >= lowProfile.CriticalPowerW);
    }

    [Fact]
    public void AssembledScenarioUsesRealHumanDecisionAuthorityId()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        WorldEntityId humanAuthorityId = application.World!.DecisionAuthorities
            .Single(authority => authority.Kind == DecisionAuthorityKind.HumanInput)
            .Id;

        WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot)
            .Resolve(application.World.ContentIdentity.ScenarioId);
        RaceScenarioTemplate template = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
            .ResolveTemplate(PrototypeRaceScenarioId);
        RaceScenario scenario = WorldRaceScenarioAssembler.Assemble(
            application.World,
            recipe,
            template,
            PrototypeRaceScenarioId);

        Assert.All(
            scenario.TacticalPlans,
            plan => Assert.Equal(humanAuthorityId, plan.Observation.DecisionAuthorityId));
        Assert.DoesNotContain(
            scenario.TacticalPlans,
            plan => plan.Observation.DecisionAuthorityId.Value
                == checked(plan.Observation.OrganizationId.Value + 100));
    }

    private static (double Form01, double Freshness01, double Fatigue01)[] RunFormTrajectory(long seed)
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", seed)).Succeeded);
        for (int day = 0; day < 5; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);
        return CareerWorldTestSupport.DayStateSnapshot(application);
    }
}
