using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class WtRosterCalibrationTests
{
    private const long GateSeed = 91234;
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private static readonly string[] WorldTourOrganizationOrigins =
    [
        "organization.wt2026.alpecin",
        "organization.wt2026.bahrain",
        "organization.wt2026.decathlon",
        "organization.wt2026.ef",
        "organization.wt2026.fdj",
        "organization.wt2026.ineos",
        "organization.wt2026.lidl-trek",
        "organization.wt2026.lotto",
        "organization.wt2026.movistar",
        "organization.wt2026.nsn",
        "organization.wt2026.redbull",
        "organization.wt2026.soudal",
        "organization.wt2026.jayco",
        "organization.wt2026.picnic",
        "organization.wt2026.visma",
        "organization.wt2026.uae",
        "organization.wt2026.unox",
        "organization.wt2026.astana",
    ];

    private static readonly string[] WildcardOrganizationOrigins =
    [
        "organization.wt2026.israel",
        "organization.wt2026.tudor",
        "organization.wt2026.q36",
        "organization.wt2026.totalenergies",
        "organization.wt2026.cofidis",
        "organization.wt2026.unibet",
        "organization.wt2026.australia",
    ];

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
        Assert.Equal("rider.wt2026.alpecin.support-3", origins[4]);
        Assert.Equal(22, origins.Length);
        Assert.Equal(
            "Kaden Groves",
            world.Persons.Single(person =>
                person.Id == world.GetRiderCareersForOrganization(alpecin.Id)[4].PersonId).Name);

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
        RiderCareer evenepoel = Find(world, "rider.wt2026.redbull.leader");
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
        Assert.Equal(6_600_000, Wage(world, evenepoel));

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
        Assert.True(wages.Distinct().Count() >= 4);
        Assert.True(wages.Sum(wage => (long)wage) > 5_700_000);
        Assert.Contains(4_000_000, wages);
        Assert.Contains(1_200_000, wages);
        Assert.Contains(180_000, wages);
        Assert.Equal(22, wages.Length);
    }

    [Fact]
    public void CreateWorldLoadsFullWorldTourRosterDepth()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        Assert.InRange(world.RiderCareers.Count, 448, 452);
        Assert.Equal(452, world.RiderCareers.Count);

        foreach (string organizationOrigin in WorldTourOrganizationOrigins)
        {
            Organization organization = world.Organizations.Single(
                org => string.Equals(org.OriginDefinitionId, organizationOrigin, StringComparison.Ordinal));
            Assert.Equal(22, world.GetRiderCareersForOrganization(organization.Id).Count);
        }

        foreach (string organizationOrigin in WildcardOrganizationOrigins)
        {
            Organization organization = world.Organizations.Single(
                org => string.Equals(org.OriginDefinitionId, organizationOrigin, StringComparison.Ordinal));
            Assert.Equal(8, world.GetRiderCareersForOrganization(organization.Id).Count);
        }
    }

    [Fact]
    public void RosterOriginIdsAreUnique()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        string[] originIds = world.RiderCareers.Select(career => career.OriginDefinitionId).ToArray();
        Assert.Equal(originIds.Length, originIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ClubRosterProjectionListsAllTwentyTwoAlpecinRiders()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        ClubRosterProjection roster = Assert.IsType<ClubRosterProjection>(application.ClubRoster);
        Assert.Equal(22, roster.Riders.Count);
        Assert.Equal("Mathieu van der Poel", roster.Riders[0].Name);
        Assert.Equal("Jasper Philipsen", roster.Riders[1].Name);
    }

    [Fact]
    public void ClubWagesStayWithinEstimatedBudget()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        Dictionary<string, long> budgets = new(StringComparer.Ordinal)
        {
            ["organization.wt2026.alpecin"] = 18_000_000,
            ["organization.wt2026.bahrain"] = 18_000_000,
            ["organization.wt2026.decathlon"] = 20_000_000,
            ["organization.wt2026.ef"] = 14_000_000,
            ["organization.wt2026.fdj"] = 15_000_000,
            ["organization.wt2026.ineos"] = 35_000_000,
            ["organization.wt2026.lidl-trek"] = 28_000_000,
            ["organization.wt2026.lotto"] = 16_000_000,
            ["organization.wt2026.movistar"] = 18_000_000,
            ["organization.wt2026.nsn"] = 20_000_000,
            ["organization.wt2026.redbull"] = 30_000_000,
            ["organization.wt2026.soudal"] = 22_000_000,
            ["organization.wt2026.jayco"] = 14_000_000,
            ["organization.wt2026.picnic"] = 12_000_000,
            ["organization.wt2026.visma"] = 32_000_000,
            ["organization.wt2026.uae"] = 50_000_000,
            ["organization.wt2026.unox"] = 14_000_000,
            ["organization.wt2026.astana"] = 15_000_000,
            ["organization.wt2026.israel"] = 8_000_000,
            ["organization.wt2026.tudor"] = 8_000_000,
            ["organization.wt2026.q36"] = 7_000_000,
            ["organization.wt2026.totalenergies"] = 7_000_000,
            ["organization.wt2026.cofidis"] = 8_000_000,
            ["organization.wt2026.unibet"] = 5_000_000,
            ["organization.wt2026.australia"] = 2_000_000,
        };

        foreach (Organization organization in world.Organizations)
        {
            long wageSum = world.GetRiderCareersForOrganization(organization.Id)
                .Sum(career => Wage(world, career));
            Assert.True(
                wageSum <= budgets[organization.OriginDefinitionId],
                $"{organization.OriginDefinitionId} wages={wageSum} budget={budgets[organization.OriginDefinitionId]}");
        }
    }

    [Fact]
    public void ClassicsStarCalibrationMatchesD057Overrides()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        RiderCareer mvdp = Find(world, "rider.wt2026.alpecin.leader");
        RiderCareer vanAert = Find(world, "rider.wt2026.visma.support-2");
        RiderCareer evenepoel = Find(world, "rider.wt2026.redbull.leader");
        RiderCareer pedersen = world.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, "rider.wt2026.lidl-trek.mads-pedersen", StringComparison.Ordinal));

        Assert.Equal(455, mvdp.CriticalPowerW);
        Assert.Equal(0.96, mvdp.LowIntensityDurability, 2);
        Assert.Equal(0.92, mvdp.HighIntensityDurability, 2);
        Assert.Equal(458, vanAert.CriticalPowerW);
        Assert.Equal(0.95, vanAert.LowIntensityDurability, 2);
        Assert.Equal(0.91, vanAert.HighIntensityDurability, 2);
        Assert.Equal(448, pedersen.CriticalPowerW);
        Assert.Equal(0.94, pedersen.LowIntensityDurability, 2);
        Assert.Equal(0.90, pedersen.HighIntensityDurability, 2);
        Assert.Equal(0.90, evenepoel.LowIntensityDurability, 2);
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
