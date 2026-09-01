using System;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerWorldTourPhase6Tests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string SkeletonScenarioId = "scenario.peloton.skeleton";
    private const string TduRaceContentId = "race.wt2026.tour_down_under";
    private const string AlpecinOriginId = "organization.wt2026.alpecin";
    private const long GateSeed = 91234;

    [Fact]
    public void WorldTourCreateWorldSetsAlpecinFinanceFields()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        AccessContext access = application.GetAccessContext();
        Organization alpecin = world.Organizations.Single(
            organization => organization.Id == access.CurrentOrganizationId);
        Assert.Equal(AlpecinOriginId, alpecin.OriginDefinitionId);
        Assert.Equal(0, alpecin.CashEur);
        Assert.Equal(18_000_000, alpecin.TitleSponsorAnnualFeeEur);
        Assert.Equal(365, world.FinancialYearDays);

        long wageBillAnnual = world.GetRiderCareersForOrganization(alpecin.Id)
            .Select(career => world.RiderContracts.Single(contract => contract.RiderCareerId == career.Id).AnnualWage)
            .Sum(wage => (long)wage);
        Assert.Equal(5_700_000, wageBillAnnual);

        ClubFinanceProjection finance = Assert.IsType<ClubFinanceProjection>(application.ClubFinance);
        long expectedDailySponsor = 18_000_000 / 365;
        long expectedDailyWages = wageBillAnnual / 365;
        Assert.Equal(expectedDailySponsor, finance.DailySponsor);
        Assert.Equal(expectedDailyWages, finance.DailyWages);
        Assert.Equal(expectedDailySponsor - expectedDailyWages, finance.DailyNet);
        Assert.False(finance.Overdrawn);
    }

    [Fact]
    public void WorldTourAdvanceDayUpdatesAlpecinCashDeterministically()
    {
        GameApplication first = TestApplication.Create();
        GameApplication second = TestApplication.Create();
        Assert.True(first.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        Assert.True(second.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);

        ClubFinanceProjection before = Assert.IsType<ClubFinanceProjection>(first.ClubFinance);
        Assert.True(first.Execute(new AdvanceDayCommand()).Succeeded);
        long cashAfterOneDay = first.World!.Organizations
            .Single(organization => organization.Id == first.GetAccessContext().CurrentOrganizationId)
            .CashEur;
        Assert.Equal(before.DailyNet, cashAfterOneDay);

        Assert.True(second.Execute(new AdvanceDayCommand()).Succeeded);
        long secondCash = second.World!.Organizations
            .Single(organization => organization.Id == second.GetAccessContext().CurrentOrganizationId)
            .CashEur;
        Assert.Equal(cashAfterOneDay, secondCash);
    }

    [Fact]
    public void OverdrawnEmployerAddsHubNote()
    {
        WorldEntityId organizationId = new(10);
        WorldEntityId personId = new(20);
        WorldEntityId riderCareerId = new(30);
        WorldEntityId contractId = new(40);
        Organization organization = new(
            organizationId,
            CareerWorldTestSupport.RedOrganizationOriginId,
            "red",
            titleSponsorAnnualFeeEur: 0);
        Person person = new(personId, "Overdrawn Rider", CareerWorldTestSupport.BetaLeaderOriginId);
        RiderCareer riderCareer = new(
            riderCareerId,
            personId,
            organizationId,
            CareerWorldTestSupport.BetaLeaderOriginId,
            criticalPowerW: 415.0,
            wPrimeCapacityJ: 29_000.0,
            peakPowerW: 930.0,
            wPrimeRecoveryJPerSecond: 43.0,
            lowIntensityDurability: 0.92,
            highIntensityDurability: 0.90,
            bodyMassKg: 61.0,
            systemMassKg: 8.0,
            cdAM2: 0.27,
            baseCrr: 0.0038,
            positioning: 0.88,
            handling: 0.83,
            tacticalAwareness: 0.89);
        RiderContract contract = new(
            contractId,
            riderCareerId,
            organizationId,
            280_000,
            new WorldDate(0),
            new WorldDate(10_000));
        WorldState world = new WorldState(
            worldId: "overdrawn-test",
            masterSeed: 1,
            rngContractVersion: 1,
            new WorldDate(0),
            new ContentIdentity(
                "peloton.skeleton",
                "0.1.0",
                1,
                SkeletonScenarioId,
                "Dynamic",
                "Advanced",
                "Guessed",
                "test-hash"),
            rulesIdentity: "test-rules",
            rulesModules: Array.Empty<RulesModuleIdentity>(),
            entityIdHighWaterMark: 40,
            new[] { person },
            Array.Empty<ManagerCareer>(),
            Array.Empty<Employment>(),
            new[] { organization },
            Array.Empty<DecisionAuthority>(),
            calendarPeriodDays: 12,
            riderCareers: new[] { riderCareer },
            riderContracts: new[] { contract });

        world.AdvanceOneDay();
        world.CaptureDayNotes(new AccessContext(null, organizationId, null, "test"));

        Assert.True(organization.CashEur < 0);
        Assert.Contains("The club is overdrawn.", world.LastDayNotes);
    }

    [Fact]
    public void SchemaVersionSixRoundTripsCashFeeAndChecksum()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "career-finance.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        Assert.True(source.Execute(new AdvanceDayCommand()).Succeeded);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        WorldCheckpoint stored = new SqliteWorldSaveStore().Load(savePath);
        Organization storedAlpecin = stored.World.Organizations.Single(
            organization => string.Equals(organization.OriginDefinitionId, AlpecinOriginId, StringComparison.Ordinal));
        Assert.NotEqual(0, storedAlpecin.CashEur);

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(
            WorldChecksum.Compute(source.World!),
            WorldChecksum.Compute(loaded.World!));
    }

    [Fact]
    public void SkeletonTenSeasonRunnerStillCompletesWithFiniteEmployerCash()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        Organization employer = application.World!.Organizations.Single(
            organization => organization.Id == application.GetAccessContext().CurrentOrganizationId);
        Assert.Equal(2_000_000, employer.TitleSponsorAnnualFeeEur);
        Assert.Equal(12, application.World.FinancialYearDays);

        SkeletonCareerRunner runner = new(application);
        SkeletonRunReport report = runner.Run(10, temp.Path);

        Assert.False(report.Crashed);
        Assert.Equal(10, report.RaceCount);
        Assert.Equal(120, report.WorldDay);
        Assert.Equal(12, application.World.RiderCareers.Count(item => item.OrganizationId is not null));
        Assert.NotEqual(0, employer.CashEur);
    }

    [Fact]
    public void WorldTourPreparationTitleUsesCalendarRaceName()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        for (int day = 0; day < 19; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        RacePreparationProjection prep = Assert.IsType<RacePreparationProjection>(application.RacePreparation);
        Assert.StartsWith("Santos Tour Down Under", prep.Title, StringComparison.Ordinal);
        Assert.NotEqual(RacePreparationDefaults.Title, prep.Title);
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
