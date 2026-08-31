using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerWorldTourPhase4Tests
{
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";
    private const string AlphaLeaderOriginId = "rider.race-prototype.alpha-leader";
    private const string RedOrganizationOriginId = "organization.skeleton.red";
    private const long GateSeed = 91234;

    [Fact]
    public void CreateWorldMaterializesTwelveContractsWithExpectedWagesAndLoyalty()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);

        Assert.Equal(12, application.World!.RiderContracts.Count);
        RiderCareer alphaLeader = application.World.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, AlphaLeaderOriginId, StringComparison.Ordinal));
        RiderContract alphaContract = application.World.RiderContracts.Single(
            contract => contract.RiderCareerId == alphaLeader.Id);
        Assert.Equal(280_000, alphaContract.AnnualWage);
        Assert.Equal(0.80, alphaLeader.Loyalty01, precision: 10);

        foreach (RiderCareer career in application.World.RiderCareers
                     .Where(item => !string.Equals(item.OriginDefinitionId, AlphaLeaderOriginId, StringComparison.Ordinal)))
        {
            Assert.Equal(0.5, career.Loyalty01, precision: 10);
        }

        foreach (RiderContract contract in application.World.RiderContracts)
        {
            RiderCareer career = application.World.TryGetRiderCareer(contract.RiderCareerId)!;
            Assert.Equal(contract.OrganizationId, career.OrganizationId);
        }
    }

    [Fact]
    public void ClubRosterProjectionListsRedEmployerRidersWithWages()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);

        ClubRosterProjection roster = Assert.IsType<ClubRosterProjection>(application.ClubRoster);
        Assert.Equal(4, roster.Riders.Count);
        int[] expectedWages = { 280_000, 160_000, 110_000, 90_000 };
        Assert.Equal(
            expectedWages,
            roster.Riders.Select(entry => entry.AnnualWage).OrderByDescending(wage => wage).ToArray());
        Assert.All(roster.Riders, entry => Assert.Equal(10_000, entry.ContractEndDay));
    }

    [Fact]
    public void SchemaVersionFiveRoundTripsContractsWagesLoyaltyAndNullableOrganization()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "career-contracts.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        WorldCheckpoint stored = new SqliteWorldSaveStore().Load(savePath);
        Assert.Equal(12, stored.World.RiderContracts.Count);
        Assert.Equal(12, stored.World.RiderCareers.Count);

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(
            WorldChecksum.Compute(source.World!),
            WorldChecksum.Compute(loaded.World!));
    }

    [Fact]
    public void ContractExpiringOnDayZeroDetachesRiderAfterFirstAdvance()
    {
        WorldState world = CareerWorldTestSupport.CreateContractExpiryWorld(contractEndDay: 0, currentDay: 0);
        WorldEntityId organizationId = world.Organizations[0].Id;
        WorldEntityId riderId = world.RiderCareers[0].Id;

        Assert.NotNull(world.RiderCareers[0].OrganizationId);
        world.AdvanceOneDay();
        world.CaptureDayNotes(new AccessContext(null, organizationId, null, "test"));

        Assert.Equal(1, world.CurrentDate.DayNumber);
        Assert.Null(world.RiderCareers[0].OrganizationId);
        Assert.Empty(world.GetRiderCareersForOrganization(organizationId));
        Assert.Single(world.RiderContracts);
        Assert.Contains($"{world.Persons[0].Name}'s contract expired.", world.LastDayNotes);

        HashSet<WorldEntityId> enteredOrganizations = world.OrganizationRaceEntries
            .Where(entry =>
                entry.Entered &&
                string.Equals(entry.RaceContentId, PrototypeRaceScenarioId, StringComparison.Ordinal))
            .Select(entry => entry.OrganizationId)
            .ToHashSet();
        bool wouldStart = world.RiderCareers.Any(career =>
            career.Id == riderId &&
            career.OrganizationId is WorldEntityId organizationId &&
            enteredOrganizations.Contains(organizationId));
        Assert.False(wouldStart);
    }

    [Fact]
    public void InclusiveLastContractDayKeepsRiderOnRosterUntilNextAdvance()
    {
        WorldState world = CareerWorldTestSupport.CreateContractExpiryWorld(contractEndDay: 5, currentDay: 5);
        WorldEntityId organizationId = world.Organizations[0].Id;

        Assert.NotNull(world.RiderCareers[0].OrganizationId);
        Assert.Single(world.GetRiderCareersForOrganization(organizationId));

        world.AdvanceOneDay();
        Assert.Equal(6, world.CurrentDate.DayNumber);
        Assert.Null(world.RiderCareers[0].OrganizationId);
        Assert.Empty(world.GetRiderCareersForOrganization(organizationId));
    }

    [Fact]
    public void UnattachedRidersStillReceiveRestTick()
    {
        WorldState world = CareerWorldTestSupport.CreateContractExpiryWorld(
            contractEndDay: 0,
            currentDay: 0,
            fatigue01: 0.5);
        world.AdvanceOneDay();

        Assert.Null(world.RiderCareers[0].OrganizationId);
        Assert.True(world.RiderCareers[0].Fatigue01 < 0.5);
    }

    [Fact]
    public void TenSeasonRunnerStillCompletesWithTwelveRidersOnClubs()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        SkeletonCareerRunner runner = new(application);
        SkeletonRunReport report = runner.Run(10, temp.Path);

        Assert.False(report.Crashed);
        Assert.Equal(10, report.RaceCount);
        Assert.Equal(120, report.WorldDay);
        Assert.Equal(12, application.World!.RiderCareers.Count(item => item.OrganizationId is not null));
    }
}
