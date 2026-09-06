using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class ScoutingAndSponsorshipTests
{
    private const string SkeletonScenarioId = "scenario.peloton.skeleton";
    private const long GateSeed = 91234;

    [Fact]
    public void RiderStyleAndStarsCalculationMatchesArchetypes()
    {
        // Climber
        RiderRatingSet climberRatings = new(
            Climb: 90,
            Hills: 85,
            Flat: 68,
            TimeTrial: 72,
            Sprint: 65,
            Cobbles: 60,
            Ovr: 84,
            PotentialOvr: 88);

        RiderStarsProfile climberProfile = RiderStyleRatingQueries.ComputeProfile(climberRatings, "climber.test");
        Assert.Equal("GÓRY", climberProfile.PrimaryStyleLabel);
        Assert.True(climberProfile.PrimaryStars >= 4.0);
        Assert.Contains("★", climberProfile.PrimaryStarsDisplay);

        // Sprinter
        RiderRatingSet sprinterRatings = new(
            Climb: 58,
            Hills: 64,
            Flat: 82,
            TimeTrial: 65,
            Sprint: 92,
            Cobbles: 70,
            Ovr: 82,
            PotentialOvr: 85);

        RiderStarsProfile sprinterProfile = RiderStyleRatingQueries.ComputeProfile(sprinterRatings, "sprinter.test");
        Assert.Equal("SPRINT", sprinterProfile.PrimaryStyleLabel);
        Assert.True(sprinterProfile.PrimaryStars >= 4.0);

        // Time Trialist
        RiderRatingSet ttRatings = new(
            Climb: 68,
            Hills: 72,
            Flat: 84,
            TimeTrial: 91,
            Sprint: 64,
            Cobbles: 62,
            Ovr: 83,
            PotentialOvr: 86);

        RiderStarsProfile ttProfile = RiderStyleRatingQueries.ComputeProfile(ttRatings, "tt.test");
        Assert.Equal("CZASOWIEC", ttProfile.PrimaryStyleLabel);
        Assert.True(ttProfile.PrimaryStars >= 4.0);
    }

    [Fact]
    public void StartScoutingMissionAndAdvanceDayDiscoversTargetRider()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        WorldEntityId playerOrgId = application.GetAccessContext().CurrentOrganizationId!.Value;
        Organization playerOrg = world.Organizations.Single(o => o.Id == playerOrgId);

        // Pick a rival rider
        RiderCareer target = world.RiderCareers.First(c => c.OrganizationId != playerOrgId);
        Assert.Equal(ScoutingKnowledgeLevel.Unknown, playerOrg.ScoutingStore.GetLevel(target.Id));

        // Start 3-day mission
        CommandResult start = application.Execute(new StartScoutingMissionCommand(target.Id, "Top Scout", DurationDays: 3));
        Assert.True(start.Succeeded);

        ScoutingOverviewProjection? overview = application.ScoutingOverview;
        Assert.NotNull(overview);
        Assert.Single(overview.ActiveMissions);
        Assert.Equal(target.Id, overview.ActiveMissions[0].TargetRiderCareerId);
        Assert.Equal(3, overview.ActiveMissions[0].DaysRemaining);

        // Advance 1 day
        world.AdvanceOneDay();
        overview = application.ScoutingOverview;
        Assert.NotNull(overview);
        Assert.Single(overview.ActiveMissions);
        Assert.Equal(2, overview.ActiveMissions[0].DaysRemaining);

        // Advance 2 more days to finish initial mission
        world.AdvanceOneDay();
        world.AdvanceOneDay();

        Assert.Equal(ScoutingKnowledgeLevel.Observed, playerOrg.ScoutingStore.GetLevel(target.Id));
        overview = application.ScoutingOverview;
        Assert.NotNull(overview);
        Assert.Contains(overview.DiscoveredRiders, r => r.RiderCareerId == target.Id);

        // Second deeper mission moves to Discovered
        Assert.True(application.Execute(new StartScoutingMissionCommand(target.Id, "Top Scout", DurationDays: 2)).Succeeded);
        world.AdvanceOneDay();
        world.AdvanceOneDay();
        Assert.Equal(ScoutingKnowledgeLevel.Discovered, playerOrg.ScoutingStore.GetLevel(target.Id));
    }

    [Fact]
    public void ExtendSponsorAgreementIncreasesDurationAndTrust()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        WorldEntityId playerOrgId = application.GetAccessContext().CurrentOrganizationId!.Value;
        Organization playerOrg = world.Organizations.Single(o => o.Id == playerOrgId);

        SponsorOverviewProjection? initialSponsor = application.SponsorOverview;
        Assert.NotNull(initialSponsor);
        int initialEndYear = initialSponsor.EndSeasonYear;
        double initialTrust = initialSponsor.Trust01;

        CommandResult extend = application.Execute(new ExtendSponsorAgreementCommand(AdditionalYears: 2));
        Assert.True(extend.Succeeded);

        SponsorOverviewProjection? after = application.SponsorOverview;
        Assert.NotNull(after);
        Assert.Equal(initialEndYear + 2, after.EndSeasonYear);
        Assert.True(after.Trust01 >= initialTrust);
        Assert.True(after.AnnualFeeEur > initialSponsor.AnnualFeeEur);
    }

    [Fact]
    public void MarketRidersReflectFogOfWarForUnknownAndDiscoveredRiders()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        WorldEntityId playerOrgId = application.GetAccessContext().CurrentOrganizationId!.Value;
        Organization playerOrg = world.Organizations.Single(o => o.Id == playerOrgId);

        IReadOnlyList<MarketRiderProjection> market = application.MarketRiders;
        Assert.NotEmpty(market);

        MarketRiderProjection first = market[0];
        Assert.Equal("Nieznany", first.ScoutingLevel);
        Assert.Contains("-", first.StarsDisplay); // Fog band e.g. "3.5-5.0 ★"

        // Discover the first rider
        playerOrg.ScoutingStore.SetLevel(first.RiderCareerId, ScoutingKnowledgeLevel.Discovered);
        IReadOnlyList<MarketRiderProjection> updatedMarket = application.MarketRiders;
        MarketRiderProjection discovered = updatedMarket.Single(r => r.RiderCareerId == first.RiderCareerId);
        Assert.Equal("Pełny", discovered.ScoutingLevel);
        Assert.DoesNotContain("-", discovered.StarsDisplay);
        Assert.Contains("★", discovered.StarsDisplay);
    }
}
