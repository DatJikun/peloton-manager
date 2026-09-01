using System;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class WtRosterCalibrationTests
{
    private const long GateSeed = 91234;
    private const string WtScenarioId = "scenario.peloton.wt-2026";

    [Fact]
    public void WorldTourSquadOrderIsCaptainThenProtectedCardThenHelpers()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        Organization alpecin = world.Organizations.Single(
            organization => string.Equals(
                organization.OriginDefinitionId,
                "organization.wt2026.alpecin",
                StringComparison.Ordinal));
        string[] origins = world.GetRiderCareersForOrganization(alpecin.Id)
            .Select(career => career.OriginDefinitionId)
            .ToArray();
        Assert.Equal("rider.wt2026.alpecin.leader", origins[0]);
        Assert.Equal("rider.wt2026.alpecin.card", origins[1]);
        Assert.Equal("rider.wt2026.alpecin.support-1", origins[2]);
        Assert.Equal("rider.wt2026.alpecin.support-2", origins[3]);

        Organization bahrain = world.Organizations.Single(
            organization => string.Equals(
                organization.OriginDefinitionId,
                "organization.wt2026.bahrain",
                StringComparison.Ordinal));
        Assert.Equal(
            "rider.wt2026.bahrain.leader",
            world.GetRiderCareersForOrganization(bahrain.Id)[0].OriginDefinitionId);
        Assert.Equal(
            "Phil Bauhaus",
            world.Persons.Single(person =>
                person.Id == world.GetRiderCareersForOrganization(bahrain.Id)[1].PersonId).Name);
    }

    [Fact]
    public void ClubRosterListsCaptainFirstNotAlphabeticalCard()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        ClubRosterProjection roster = Assert.IsType<ClubRosterProjection>(application.ClubRoster);
        Assert.Equal("Mathieu van der Poel", roster.Riders[0].Name);
        Assert.Equal("Jasper Philipsen", roster.Riders[1].Name);
        Assert.Equal("rider.wt2026.alpecin.leader", roster.Riders[0].OriginDefinitionId);
    }

    [Fact]
    public void DefaultPreparationStrategySelectsDesignatedCaptain()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        for (int day = 0; day < 19; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.SetDefaultStrategy(application).Succeeded);
        RacePreparationProjection prep = Assert.IsType<RacePreparationProjection>(application.RacePreparation);
        RiderCareer captain = application.World!.RiderCareers.Single(
            career => string.Equals(
                career.OriginDefinitionId,
                "rider.wt2026.alpecin.leader",
                StringComparison.Ordinal));
        RiderCareer sprinter = application.World.RiderCareers.Single(
            career => string.Equals(
                career.OriginDefinitionId,
                "rider.wt2026.alpecin.card",
                StringComparison.Ordinal));
        Assert.Equal(captain.Id, prep.LeaderId);
        Assert.Equal(sprinter.Id, prep.SupportId);
    }

    [Fact]
    public void NamedRidersMatchArchetypeBandsAndWageRoles()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;

        RiderCareer pogacar = Find(world, "rider.wt2026.uae.leader");
        RiderCareer philipsen = Find(world, "rider.wt2026.alpecin.card");
        RiderCareer mvdp = Find(world, "rider.wt2026.alpecin.leader");
        RiderCareer bauhaus = Find(world, "rider.wt2026.bahrain.card");
        RiderCareer evenepoel = Find(world, "rider.wt2026.soudal.leader");
        RiderCareer vanAert = Find(world, "rider.wt2026.visma.support-2");
        RiderCareer hermans = Find(world, "rider.wt2026.alpecin.support-2");

        Assert.InRange(pogacar.BodyMassKg, 64, 67);
        Assert.InRange(pogacar.CriticalPowerW / pogacar.BodyMassKg, 6.4, 6.8);
        Assert.Equal(8_000_000, Wage(world, pogacar));

        Assert.InRange(philipsen.BodyMassKg, 72, 76);
        Assert.True(philipsen.PeakPowerW >= 1650);
        Assert.Equal(1_200_000, Wage(world, philipsen));

        Assert.InRange(mvdp.BodyMassKg, 73, 78);
        Assert.Equal(4_000_000, Wage(world, mvdp));
        Assert.NotEqual(Wage(world, mvdp), Wage(world, philipsen));

        Assert.True(bauhaus.BodyMassKg >= 70);
        Assert.True(bauhaus.PeakPowerW >= 1500);
        Assert.True(Wage(world, bauhaus) >= 400_000);
        Assert.True(Wage(world, bauhaus) < Wage(world, mvdp));

        Assert.InRange(evenepoel.BodyMassKg, 60, 64);
        Assert.True(evenepoel.CriticalPowerW / evenepoel.BodyMassKg >= 6.4);
        Assert.Equal(6_500_000, Wage(world, evenepoel));

        Assert.InRange(vanAert.BodyMassKg, 76, 80);
        Assert.Equal(4_000_000, Wage(world, vanAert));

        Assert.True(Wage(world, hermans) <= 220_000);
        Assert.True(Wage(world, pogacar) > Wage(world, hermans) * 20);
    }

    [Fact]
    public void AlpecinWagesAreByRoleNotOneSalaryForTheClub()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        int[] wages = application.World!
            .GetRiderCareersForOrganization(application.GetAccessContext().CurrentOrganizationId!.Value)
            .Select(career => Wage(application.World, career))
            .ToArray();
        Assert.Equal(4, wages.Distinct().Count());
        Assert.Equal(5_700_000, wages.Sum(wage => (long)wage));
        Assert.Contains(4_000_000, wages);
        Assert.Contains(1_200_000, wages);
        Assert.Contains(180_000, wages);
    }

    private static RiderCareer Find(WorldState world, string originId) =>
        world.RiderCareers.Single(career =>
            string.Equals(career.OriginDefinitionId, originId, StringComparison.Ordinal));

    private static int Wage(WorldState world, RiderCareer career) =>
        world.RiderContracts.Single(contract =>
            contract.RiderCareerId == career.Id &&
            contract.StartDate.DayNumber <= world.CurrentDate.DayNumber &&
            contract.EndDate.DayNumber >= world.CurrentDate.DayNumber).AnnualWage;
}
