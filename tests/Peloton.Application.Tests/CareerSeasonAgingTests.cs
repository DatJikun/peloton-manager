using System;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerSeasonAgingTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const long GateSeed = 91234;

    [Fact]
    public void TwentyOneYearOldWithHighPotGainsCriticalPower()
    {
        RiderCareer career = CreateRider(criticalPowerW: 360, potentialOvr: 90);
        double before = career.CriticalPowerW;
        Assert.True(RiderRatingQueries.FromPhysiology(career, career.PotentialOvr).Ovr <= 90);
        SeasonAging.ApplyToCareer(GateSeed, 2027, career, age: 21);
        Assert.True(career.CriticalPowerW > before);
        int ovr = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr).Ovr;
        Assert.True(ovr <= career.PotentialOvr);
    }

    [Fact]
    public void ThirtySixYearOldLosesCriticalPower()
    {
        RiderCareer career = CreateRider(criticalPowerW: 400, potentialOvr: 88);
        double before = career.CriticalPowerW;
        SeasonAging.ApplyToCareer(GateSeed, 2027, career, age: 36);
        Assert.True(career.CriticalPowerW < before);
    }

    [Fact]
    public void WorldRolloverAgesYoungRiderUpAndOldRiderDownAndNeverExceedsPot()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        Assert.All(world.Persons.Where(person => person.OriginDefinitionId is not null),
            person => Assert.True(person.BirthYear is > 0));

        RiderCareer young = FindByBirthYear(world, 2006);
        RiderCareer old = FindByBirthYear(world, 1990);
        double youngBefore = young.CriticalPowerW;
        double oldBefore = old.CriticalPowerW;

        AdvanceToNewYear(application);
        young = world.RiderCareers.Single(career => career.Id == young.Id);
        old = world.RiderCareers.Single(career => career.Id == old.Id);
        Assert.True(young.CriticalPowerW > youngBefore);
        Assert.True(old.CriticalPowerW < oldBefore);
        Assert.All(world.RiderCareers, career =>
        {
            int ovr = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr).Ovr;
            Assert.True(ovr <= career.PotentialOvr);
        });
    }

    private static RiderCareer FindByBirthYear(WorldState world, int birthYear)
    {
        Person person = world.Persons.First(item => item.BirthYear == birthYear && item.OriginDefinitionId is not null);
        return world.RiderCareers.Single(career => career.PersonId == person.Id);
    }

    private static void AdvanceToNewYear(GameApplication application)
    {
        WorldState world = application.World!;
        while (world.CurrentDate.DayNumber < 365)
        {
            world.AdvanceOneDay();
        }
    }

    private static RiderCareer CreateRider(double criticalPowerW, int potentialOvr)
    {
        return new RiderCareer(
            new WorldEntityId(1),
            new WorldEntityId(2),
            new WorldEntityId(3),
            "rider.test.aging",
            criticalPowerW,
            wPrimeCapacityJ: 22_000,
            peakPowerW: 1_100,
            wPrimeRecoveryJPerSecond: 30,
            lowIntensityDurability: 0.80,
            highIntensityDurability: 0.80,
            bodyMassKg: 70,
            systemMassKg: 8,
            cdAM2: 0.28,
            baseCrr: 0.004,
            positioning: 0.70,
            handling: 0.70,
            tacticalAwareness: 0.70,
            potentialOvr: potentialOvr);
    }
}
