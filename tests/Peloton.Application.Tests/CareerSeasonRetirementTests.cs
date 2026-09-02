using System;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerSeasonRetirementTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string UaeOriginId = "organization.wt2026.uae";
    private const string PrototypeRaceTemplateId = "race-scenario.peloton.prototype-v0";
    private const string TduRaceContentId = "race.wt2026.tour_down_under";
    private const long GateSeed = 91234;

    [Fact]
    public void NamesPackHasSixtyFirstAndLastNamesPerGroup()
    {
        SeasonNeoPros.NameBank names = SeasonNeoPros.LoadNames(TestApplication.ContentRoot);
        Assert.True(names.Nations.Count >= 4);
        Assert.All(names.Nations, nation =>
        {
            Assert.True(nation.First.Count >= 60);
            Assert.True(nation.Last.Count >= 60);
        });
    }

    [Fact]
    public void FortyYearOldWithContractRetiresOnFirstRollover()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        AdvanceToNewYear(application);
        WorldState world = application.World!;

        Person morkov = world.Persons.Single(person =>
            string.Equals(person.Name, "Michael Mørkøv", StringComparison.Ordinal));
        RiderCareer career = world.RiderCareers.Single(item => item.PersonId == morkov.Id);
        Assert.True(career.IsRetired);
        Assert.Null(career.OrganizationId);
        Assert.True(SeasonRolloverExecutor.LastRetiredCount >= 1);
        Assert.Equal(SeasonRolloverExecutor.LastRetiredCount, SeasonRolloverExecutor.LastNeoCount);
    }

    [Fact]
    public void NeoCountMatchesRetirementAndLivingCountDoesNotShrink()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        WorldState world = application.World!;
        int livingBefore = world.LivingRiderCount;
        int expectedRetirements = CountRidersAgedAtLeast(world, 2027, 40);
        Assert.True(expectedRetirements >= 1);
        Assert.True(livingBefore >= expectedRetirements);

        AdvanceToNewYear(application);

        Assert.Equal(livingBefore, world.LivingRiderCount);
        Assert.Equal(expectedRetirements, SeasonRolloverExecutor.LastRetiredCount);
        Assert.Equal(expectedRetirements, SeasonRolloverExecutor.LastNeoCount);
        Assert.Equal(livingBefore + expectedRetirements, world.RiderCareers.Count);
        Assert.Equal(expectedRetirements, world.RiderCareers.Count(career => career.IsRetired));
        Assert.Equal(
            expectedRetirements,
            world.RiderCareers.Count(career =>
                career.OriginDefinitionId.StartsWith("rider.generated.2027.", StringComparison.Ordinal) &&
                career.OrganizationId is null &&
                !career.IsRetired));
    }

    [Fact]
    public void InjectedUnattachedThirtyThreeYearOldWithoutResultsRetires()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        WorldState world = application.World!;
        RiderCareer veteran = InjectRider(
            world,
            originId: "rider.test.unattached-33",
            birthYear: 1994,
            criticalPowerW: 380,
            potentialOvr: 80,
            organizationId: null,
            withContract: false);
        AdvanceToNewYear(application);
        Assert.True(veteran.IsRetired);
    }

    [Fact]
    public void InjectedUnattachedThirtyThreeYearOldWithTopTwentyStays()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        WorldState world = application.World!;
        RiderCareer veteran = InjectRider(
            world,
            originId: "rider.test.unattached-33-result",
            birthYear: 1994,
            criticalPowerW: 380,
            potentialOvr: 80,
            organizationId: null,
            withContract: false);
        veteran.AppendResult(new RiderCareerResult("race.wt2026.tour_down_under", 20, 12, DidNotFinish: false));
        AdvanceToNewYear(application);
        Assert.False(veteran.IsRetired);
    }

    [Fact]
    public void InjectedUnattachedThirtyFiveYearOldWithLowOvrRetires()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        WorldState world = application.World!;
        RiderCareer veteran = InjectRider(
            world,
            originId: "rider.test.unattached-35-low",
            birthYear: 1992,
            criticalPowerW: 250,
            potentialOvr: 70,
            organizationId: null,
            withContract: false,
            bodyMassKg: 84,
            peakPowerW: 880,
            cdAM2: 0.33);
        int ovr = RiderRatingQueries.FromPhysiology(veteran, veteran.PotentialOvr).Ovr;
        Assert.True(ovr < 60);
        AdvanceToNewYear(application);
        Assert.True(veteran.IsRetired);
    }

    [Fact]
    public void RetiredRidersNeverStartAfterRollover()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        AdvanceToNewYear(application);
        WorldState world = application.World!;
        WorldEntityId[] retiredIds = world.RiderCareers
            .Where(career => career.IsRetired)
            .Select(career => career.Id)
            .ToArray();
        Assert.NotEmpty(retiredIds);

        WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot).Resolve(WtScenarioId);
        RaceScenarioTemplate template = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
            .ResolveTemplate(PrototypeRaceTemplateId);
        RaceScenario scenario = WorldRaceScenarioAssembler.Assemble(
            world,
            recipe,
            template,
            TduRaceContentId);

        Assert.All(retiredIds, id => Assert.DoesNotContain(scenario.Riders, rider => rider.RiderId == id));
        Assert.All(scenario.Riders, rider =>
        {
            RiderCareer career = world.RiderCareers.Single(item => item.Id == rider.RiderId);
            Assert.False(career.IsRetired);
        });
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

    private static int CountRidersAgedAtLeast(WorldState world, int seasonYear, int minAge)
    {
        return world.RiderCareers.Count(career =>
        {
            Person person = world.Persons.Single(item => item.Id == career.PersonId);
            return person.BirthYear is int birthYear && seasonYear - birthYear >= minAge;
        });
    }

    private static void AdvanceToNewYear(GameApplication application)
    {
        WorldState world = application.World!;
        while (world.CurrentDate.DayNumber < 365)
        {
            world.AdvanceOneDay();
        }
    }

    private static RiderCareer InjectRider(
        WorldState world,
        string originId,
        int birthYear,
        double criticalPowerW,
        int potentialOvr,
        WorldEntityId? organizationId,
        bool withContract,
        double bodyMassKg = 70,
        double peakPowerW = 1_100,
        double cdAM2 = 0.28)
    {
        WorldEntityId personId = world.AllocateEntityId();
        WorldEntityId careerId = world.AllocateEntityId();
        world.AddPerson(new Person(personId, originId, originId, "BEL", birthYear));
        RiderCareer career = new(
            careerId,
            personId,
            organizationId,
            originId,
            criticalPowerW,
            wPrimeCapacityJ: 22_000,
            peakPowerW,
            wPrimeRecoveryJPerSecond: 30,
            lowIntensityDurability: 0.80,
            highIntensityDurability: 0.80,
            bodyMassKg,
            systemMassKg: 8,
            cdAM2,
            baseCrr: 0.004,
            positioning: 0.70,
            handling: 0.70,
            tacticalAwareness: 0.70,
            potentialOvr: potentialOvr);
        world.AddRiderCareer(career);
        if (withContract && organizationId is WorldEntityId org)
        {
            world.AddRiderContract(new RiderContract(
                world.AllocateEntityId(),
                careerId,
                org,
                100_000,
                new WorldDate(0),
                new WorldDate(2000)));
        }

        return career;
    }
}
