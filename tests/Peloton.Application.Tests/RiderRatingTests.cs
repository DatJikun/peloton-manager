using System;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class RiderRatingTests
{
    private const long GateSeed = 91234;
    private const string WtScenarioId = "scenario.peloton.wt-2026";

    [Fact]
    public void ClosedFormulaMatchesHandCalculatedFixtureRider()
    {
        RiderRatingSet ratings = RiderRatingQueries.FromPhysiology(
            criticalPowerW: 400,
            wPrimeCapacityJ: 28000,
            peakPowerW: 1100,
            lowIntensityDurability: 0.90,
            highIntensityDurability: 0.88,
            bodyMassKg: 68,
            cdAM2: 0.28,
            baseCrr: 0.004,
            positioning: 0.85,
            handling: 0.82,
            potentialOvr: 88);

        Assert.InRange(ratings.Climb, 55, 95);
        Assert.InRange(ratings.Sprint, 40, 90);
        Assert.InRange(ratings.Ovr, 55, 95);
        Assert.True(ratings.PotentialOvr >= ratings.Ovr);
    }

    [Fact]
    public void WorldTourCreateWorldSatisfiesPogacarPhilipsenInequalities()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;

        RiderCareer pogacar = FindByOrigin(world, "rider.wt2026.uae.leader");
        RiderCareer philipsen = FindByOrigin(world, "rider.wt2026.alpecin.card");
        RiderCareer mvdp = FindByOrigin(world, "rider.wt2026.alpecin.leader");
        RiderCareer almeida = FindByOrigin(world, "rider.wt2026.uae.support-1");

        RiderRatingSet poga = RiderRatingQueries.FromPhysiology(pogacar, pogacar.PotentialOvr);
        RiderRatingSet phil = RiderRatingQueries.FromPhysiology(philipsen, philipsen.PotentialOvr);
        RiderRatingSet classics = RiderRatingQueries.FromPhysiology(mvdp, mvdp.PotentialOvr);
        RiderRatingSet gcSupport = RiderRatingQueries.FromPhysiology(almeida, almeida.PotentialOvr);

        Assert.True(poga.Climb >= phil.Climb + 12);
        Assert.True(phil.Sprint >= poga.Sprint + 12);
        Assert.True(classics.Cobbles >= gcSupport.Cobbles + 8);
        Assert.True(poga.TimeTrial > phil.TimeTrial);
        Assert.True(poga.Ovr >= 88);
        Assert.True(phil.Sprint >= 88);
    }

    [Fact]
    public void EvenepoelMassIsBelowSixtyFiveKilograms()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        RiderCareer evenepoel = FindByOrigin(application.World!, "rider.wt2026.redbull.leader");
        Assert.True(evenepoel.BodyMassKg < 65);
    }

    [Fact]
    public void VanAertMassIsAboveSeventyTwoKilograms()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        RiderCareer vanAert = FindByOrigin(application.World!, "rider.wt2026.visma.support-2");
        Assert.True(vanAert.BodyMassKg > 72);
    }

    [Fact]
    public void PogacarWageIsAtLeastFourMillion()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        RiderCareer pogacar = FindByOrigin(application.World!, "rider.wt2026.uae.leader");
        RiderContract pogacarContract = application.World!.TryGetActiveContract(pogacar.Id)!;
        Assert.True(pogacarContract.AnnualWage >= 4_000_000);
    }

    [Fact]
    public void PhilipsenPeakPowerPerKgIsAtMostTwentyThree()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        RiderCareer philipsen = FindByOrigin(application.World!, "rider.wt2026.alpecin.card");
        double peakPerKg = philipsen.PeakPowerW / philipsen.BodyMassKg;
        Assert.True(peakPerKg <= 23);
    }

    [Fact]
    public void EachWorldTourTeamHasDistinctRatingShapes()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;

        foreach (Organization organization in world.Organizations)
        {
            RiderRatingSet[] ratings = world.GetRiderCareersForOrganization(organization.Id)
                .Select(career => RiderRatingQueries.FromPhysiology(career, career.PotentialOvr))
                .ToArray();
            int expectedCount = 8;
            Assert.Equal(expectedCount, ratings.Length);
            ratings = ratings.Take(4).ToArray();
            bool hasLargeGap = false;
            for (int i = 0; i < ratings.Length; i++)
            {
                for (int j = i + 1; j < ratings.Length; j++)
                {
                    int[] left = { ratings[i].Climb, ratings[i].Hills, ratings[i].Flat, ratings[i].TimeTrial, ratings[i].Sprint, ratings[i].Cobbles };
                    int[] right = { ratings[j].Climb, ratings[j].Hills, ratings[j].Flat, ratings[j].TimeTrial, ratings[j].Sprint, ratings[j].Cobbles };
                    if (left.Zip(right).Any(pair => System.Math.Abs(pair.First - pair.Second) >= 8))
                    {
                        hasLargeGap = true;
                        break;
                    }
                }

                if (hasLargeGap)
                {
                    break;
                }
            }

            Assert.True(hasLargeGap, organization.OriginDefinitionId);
        }
    }

    [Fact]
    public void VisibilityModesHideOrRevealRatings()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        AccessContext ownClub = application.GetAccessContext();
        RiderCareer rival = world.RiderCareers.First(career =>
            career.OrganizationId != ownClub.CurrentOrganizationId);
        Person person = world.Persons.Single(item => item.Id == rival.PersonId);

        RiderRatingProjection all = RiderRatingProjectionQueries.Project(
            rival,
            person,
            rival.PotentialOvr,
            "All",
            ownClub.CurrentOrganizationId);
        Assert.NotNull(all.Exact);

        RiderRatingProjection guessed = RiderRatingProjectionQueries.Project(
            rival,
            person,
            rival.PotentialOvr,
            "Guessed",
            ownClub.CurrentOrganizationId);
        Assert.NotNull(guessed.Guessed);
        Assert.Null(guessed.Exact);

        RiderRatingProjection none = RiderRatingProjectionQueries.Project(
            rival,
            person,
            rival.PotentialOvr,
            "None",
            ownClub.CurrentOrganizationId);
        Assert.Null(none.Exact);
        Assert.Null(none.Guessed);
    }

    [Fact]
    public void AdvanceDayDoesNotChangeDerivedRatings()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        RiderCareer career = application.World!.RiderCareers[0];
        RiderRatingSet before = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr);
        Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        RiderRatingSet after = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr);
        Assert.Equal(before, after);
    }

    private static RiderCareer FindByOrigin(WorldState world, string originId) =>
        world.RiderCareers.Single(career => string.Equals(career.OriginDefinitionId, originId, System.StringComparison.Ordinal));
}
