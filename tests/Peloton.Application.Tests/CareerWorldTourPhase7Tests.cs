using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerWorldTourPhase7Tests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string SkeletonScenarioId = "scenario.peloton.skeleton";
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";
    private const string AlphaLeaderOriginId = "rider.race-prototype.alpha-leader";
    private const string BetaLeaderOriginId = "rider.race-prototype.beta-leader";
    private const string RedOrganizationOriginId = "organization.skeleton.red";
    private const string BlueOrganizationOriginId = "organization.skeleton.blue";
    private const long GateSeed = 91234;

    [Fact]
    public void SkeletonRaceResultFilterReturnsOnlyOrganizationFinishersWithOfficialPlaces()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        Organization red = world.Organizations.Single(
            organization => string.Equals(organization.OriginDefinitionId, RedOrganizationOriginId, StringComparison.Ordinal));
        Organization blue = world.Organizations.Single(
            organization => string.Equals(organization.OriginDefinitionId, BlueOrganizationOriginId, StringComparison.Ordinal));

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        RaceResultProjection full = Assert.IsType<RaceResultProjection>(application.RaceResult);
        Assert.Equal(12, full.FinishOrder.Count);
        for (int index = 0; index < full.FinishOrder.Count; index++)
        {
            Assert.Equal(index + 1, full.FinishOrder[index].Place);
        }

        IReadOnlyList<RaceResultPlacement> redFiltered =
            Assert.IsType<RaceResultPlacement[]>(application.RaceResultForOrganization(red.Id));
        IReadOnlyList<RaceResultPlacement> blueFiltered =
            Assert.IsType<RaceResultPlacement[]>(application.RaceResultForOrganization(blue.Id));

        Assert.Equal(4, redFiltered.Count);
        Assert.Equal(4, blueFiltered.Count);
        Assert.All(redFiltered, place => Assert.Equal(red.Id, place.OrganizationId));
        Assert.All(blueFiltered, place => Assert.Equal(blue.Id, place.OrganizationId));
        Assert.All(redFiltered, place => Assert.False(string.IsNullOrWhiteSpace(place.OrganizationName)));

        foreach (RaceResultPlacement filtered in redFiltered)
        {
            RaceResultPlacement official = full.FinishOrder.Single(place => place.RiderId == filtered.RiderId);
            Assert.Equal(official.Place, filtered.Place);
            Assert.Equal(official.Label, filtered.Label);
        }

        foreach (RaceResultPlacement filtered in blueFiltered)
        {
            RaceResultPlacement official = full.FinishOrder.Single(place => place.RiderId == filtered.RiderId);
            Assert.Equal(official.Place, filtered.Place);
        }
    }

    [Fact]
    public void WorldTourRaceResultFilterWorksForAnyOrganization()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        for (int day = 0; day < 19; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand("race.wt2026.tour_down_under")).Succeeded);

        WorldState world = application.World!;
        Organization alpecin = world.Organizations.Single(
            organization => string.Equals(organization.OriginDefinitionId, "organization.wt2026.alpecin", StringComparison.Ordinal));
        Organization astana = world.Organizations.Single(
            organization => string.Equals(organization.OriginDefinitionId, "organization.wt2026.astana", StringComparison.Ordinal));

        RaceResultProjection full = Assert.IsType<RaceResultProjection>(application.RaceResult);
        Assert.Equal(140, full.FinishOrder.Count);
        IReadOnlyList<RaceResultPlacement> alpecinFiltered =
            Assert.IsType<RaceResultPlacement[]>(application.RaceResultForOrganization(alpecin.Id));
        IReadOnlyList<RaceResultPlacement> astanaFiltered =
            Assert.IsType<RaceResultPlacement[]>(application.RaceResultForOrganization(astana.Id));

        Assert.Equal(7, alpecinFiltered.Count);
        Assert.Equal(7, astanaFiltered.Count);
        Assert.All(alpecinFiltered, place =>
        {
            RaceResultPlacement official = full.FinishOrder.Single(item => item.RiderId == place.RiderId);
            Assert.Equal(official.Place, place.Place);
        });
        Assert.All(astanaFiltered, place => Assert.Equal(astana.Id, place.OrganizationId));
    }

    [Fact]
    public void RenewOwnRiderWithHighEnoughOfferUpdatesWageBill()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        RiderCareer alphaLeader = application.World!.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, AlphaLeaderOriginId, StringComparison.Ordinal));
        long wageBillBefore = Assert.IsType<ClubFinanceProjection>(application.ClubFinance).WageBillAnnual;
        int threshold = ContractNegotiationQueries.ComputeAcceptThreshold(280_000, alphaLeader.Loyalty01);

        Assert.True(application.Execute(new BeginContractNegotiationCommand(alphaLeader.Id)).Succeeded);
        Assert.True(application.Execute(new SetContractOfferCommand(threshold, 10_000)).Succeeded);
        Assert.True(application.Execute(new ConfirmContractOfferCommand()).Succeeded);
        Assert.Equal(GameState.Management, application.State);
        Assert.Null(application.ContractNegotiation);

        long wageBillAfter = Assert.IsType<ClubFinanceProjection>(application.ClubFinance).WageBillAnnual;
        Assert.Equal(wageBillBefore + (threshold - 280_000), wageBillAfter);
        RiderContract active = application.World.TryGetActiveContract(alphaLeader.Id)!;
        Assert.Equal(threshold, active.AnnualWage);
        Assert.Equal(2, application.World.RiderContracts.Count(contract => contract.RiderCareerId == alphaLeader.Id));
    }

    [Fact]
    public void TooLowOfferIsRejectedWithoutChangingWorld()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        RiderCareer alphaLeader = application.World!.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, AlphaLeaderOriginId, StringComparison.Ordinal));
        string checksumBefore = WorldChecksum.Compute(application.World);

        Assert.True(application.Execute(new BeginContractNegotiationCommand(alphaLeader.Id)).Succeeded);
        Assert.True(application.Execute(new SetContractOfferCommand(100_000, 10_000)).Succeeded);
        CommandResult confirm = application.Execute(new ConfirmContractOfferCommand());
        Assert.False(confirm.Succeeded);
        Assert.Equal("CONTRACT_OFFER_REJECTED", confirm.ReasonCode);
        Assert.Equal(checksumBefore, WorldChecksum.Compute(application.World));
        Assert.Null(application.ContractNegotiation);
        Assert.Equal(GameState.Management, application.State);
        Assert.Single(application.World.RiderContracts, contract => contract.RiderCareerId == alphaLeader.Id);
    }

    [Fact]
    public void PoachRiderFromAnotherClubUpdatesBothRosters()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        AccessContext access = application.GetAccessContext();
        WorldEntityId employerId = access.CurrentOrganizationId!.Value;
        Organization blue = world.Organizations.Single(
            organization => string.Equals(organization.OriginDefinitionId, BlueOrganizationOriginId, StringComparison.Ordinal));
        RiderCareer betaLeader = world.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, BetaLeaderOriginId, StringComparison.Ordinal));
        Assert.Equal(4, world.GetRiderCareersForOrganization(blue.Id).Count);
        Assert.Equal(4, world.GetRiderCareersForOrganization(employerId).Count);

        int threshold = ContractNegotiationQueries.ComputeAcceptThreshold(280_000, betaLeader.Loyalty01);
        Assert.True(application.Execute(new BeginContractNegotiationCommand(betaLeader.Id)).Succeeded);
        Assert.True(application.Execute(new SetContractOfferCommand(threshold, 10_000)).Succeeded);
        Assert.True(application.Execute(new ConfirmContractOfferCommand()).Succeeded);

        Assert.Equal(employerId, betaLeader.OrganizationId);
        Assert.Equal(5, world.GetRiderCareersForOrganization(employerId).Count);
        Assert.Equal(3, world.GetRiderCareersForOrganization(blue.Id).Count);
        Assert.DoesNotContain(betaLeader.Id, world.GetRiderCareersForOrganization(blue.Id).Select(career => career.Id));
        Assert.Contains(betaLeader.Id, world.GetRiderCareersForOrganization(employerId).Select(career => career.Id));
    }

    [Fact]
    public void CancelContractNegotiationDiscardsDraft()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        RiderCareer alphaLeader = application.World!.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, AlphaLeaderOriginId, StringComparison.Ordinal));

        Assert.True(application.Execute(new BeginContractNegotiationCommand(alphaLeader.Id)).Succeeded);
        Assert.True(application.Execute(new SetContractOfferCommand(300_000, 10_000)).Succeeded);
        Assert.NotNull(application.ContractNegotiation);
        Assert.True(application.Execute(new CancelContractNegotiationCommand()).Succeeded);
        Assert.Null(application.ContractNegotiation);
        Assert.Equal(GameState.Management, application.State);
    }

    [Fact]
    public void ContractNegotiationStaysInManagementWithNineGameStates()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        RiderCareer alphaLeader = application.World!.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, AlphaLeaderOriginId, StringComparison.Ordinal));

        Assert.True(application.Execute(new BeginContractNegotiationCommand(alphaLeader.Id)).Succeeded);
        Assert.Equal(GameState.Management, application.State);
        Assert.True(application.Execute(new SetContractOfferCommand(300_000, 10_000)).Succeeded);
        Assert.Equal(GameState.Management, application.State);
        Assert.Equal(9, Enum.GetValues<GameState>().Length);
    }

    [Fact]
    public void SchemaVersionEightRoundTripsNegotiationWorldAndChecksum()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "career-phase7.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        RiderCareer alphaLeader = source.World!.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, AlphaLeaderOriginId, StringComparison.Ordinal));
        int threshold = ContractNegotiationQueries.ComputeAcceptThreshold(280_000, alphaLeader.Loyalty01);
        Assert.True(source.Execute(new BeginContractNegotiationCommand(alphaLeader.Id)).Succeeded);
        Assert.True(source.Execute(new SetContractOfferCommand(threshold, 10_000)).Succeeded);
        Assert.True(source.Execute(new ConfirmContractOfferCommand()).Succeeded);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        WorldCheckpoint stored = new SqliteWorldSaveStore().Load(savePath);
        Assert.Equal("8", SqliteWorldSaveStore.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(
            WorldChecksum.Compute(source.World!),
            WorldChecksum.Compute(loaded.World!));
    }

    [Fact]
    public void ArchitectureStillExcludesPlayerTeamAndStubRaceEngine()
    {
        Assert.DoesNotContain(
            typeof(GameApplication).Assembly.GetTypes(),
            type => string.Equals(type.Name, "PlayerTeam", StringComparison.Ordinal));
        Assert.DoesNotContain(
            typeof(GameApplication).Assembly.GetTypes(),
            type => string.Equals(type.Name, "StubRaceEngine", StringComparison.Ordinal));
    }
}
