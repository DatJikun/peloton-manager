using System;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerSeasonRolloverStage1Tests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string UaeOriginId = "organization.wt2026.uae";
    private const long GateSeed = 91234;

    [Fact]
    public void CreateWorldStartsSeason2026AtDayZero()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;

        Assert.Equal(2026, world.SeasonYear);
        Assert.Equal(0, world.SeasonStartDayNumber);
        Assert.Equal(0, world.CurrentDate.DayNumber);
    }

    [Fact]
    public void RolloverHappensExactlyOnceWhenCrossingInto2027()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        AdvanceToDay(application, 364);
        Assert.Equal(2026, application.World!.SeasonYear);

        application.World.AdvanceOneDay();
        Assert.True(application.World.SeasonRolloverOccurred);
        Assert.Equal(2027, application.World.SeasonYear);

        application.World.AdvanceOneDay();
        Assert.False(application.World.SeasonRolloverOccurred);
        Assert.Equal(2027, application.World.SeasonYear);
    }

    [Fact]
    public void AdvanceDayCommandReopensPreSeasonOnRollover()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        AdvanceToDay(application, 364);
        Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        Assert.Equal(GameState.PreSeasonPlanningFlow, application.State);
        Assert.Equal(2027, application.PreSeasonPlanning!.SeasonYear);
        Assert.NotEmpty(application.PreSeasonPlanning.Races);
        Assert.All(
            application.PreSeasonPlanning.Races,
            race => Assert.True(race.DayNumber >= application.World!.SeasonStartDayNumber));
    }

    [Fact]
    public void DayAfter31Dec2026Is1Jan2027WithSeasonYear2027()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        AdvanceToDay(application, 364);
        Assert.Equal("31 grudnia 2026", CareerCalendarDates.FormatLong(application.World!.CurrentDate.DayNumber));

        application.World.AdvanceOneDay();
        Assert.Equal(365, application.World.CurrentDate.DayNumber);
        Assert.Equal("1 stycznia 2027", CareerCalendarDates.FormatLong(application.World.CurrentDate.DayNumber));
        Assert.Equal(2027, application.World.SeasonYear);
    }

    [Fact]
    public void Courses2027ExistAndKeep2026ProfilesForHistory()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        int profiles2026Before = application.World!.CourseProfiles.Count(profile => profile.SeasonYear == 2026);
        AdvanceToDay(application, 365);

        if (application.State == GameState.PreSeasonPlanningFlow)
        {
            SkipAllRacesAndConfirm(application);
        }

        WorldState world = application.World!;
        int profiles2026After = world.CourseProfiles.Count(profile => profile.SeasonYear == 2026);
        int profiles2027 = world.CourseProfiles.Count(profile => profile.SeasonYear == 2027);
        Assert.Equal(profiles2026Before, profiles2026After);
        Assert.True(profiles2027 > 100);
        Assert.Equal(
            profiles2026Before + profiles2027,
            world.CourseProfiles.Count);
    }

    [Fact]
    public void Roubaix2027RemainsCobbleClassicAndTdf2027HasItt()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        AdvanceToDay(application, 365);
        if (application.State == GameState.PreSeasonPlanningFlow)
        {
            SkipAllRacesAndConfirm(application);
        }

        WorldState world = application.World!;
        CourseProfile roubaix2027 = world.CourseProfiles.Single(
            profile => string.Equals(profile.RaceContentId, "race.wt2026.roubaix", StringComparison.Ordinal) &&
                       profile.SeasonYear == 2027);
        Assert.Equal(ClassifiedStageType.CobbleClassic, roubaix2027.ClassifiedStageType);

        bool tdfHasItt = world.CourseProfiles
            .Where(profile => string.Equals(profile.RaceContentId, "race.wt2026.tdf", StringComparison.Ordinal) &&
                              profile.SeasonYear == 2027)
            .Any(profile => profile.ClassifiedStageType == ClassifiedStageType.IndividualTimeTrial);
        Assert.True(tdfHasItt);
    }

    [Fact]
    public void RolloverResetsFormOnWinterTick()
    {
        GameApplication application = CreateWorldSkippingPlayerRaces();
        RiderCareer rider = application.World!.RiderCareers.First(career => career.OrganizationId is not null);
        rider.ApplyRaceLoad();
        rider.ApplyRaceLoad();
        Assert.True(rider.Fatigue01 > 0.0);

        AdvanceToDay(application, 364);
        application.World.AdvanceOneDay();
        Assert.True(application.World.RiderCareers
            .Where(career => !career.IsRetired)
            .All(career =>
            career.Form01 == 1.0 && career.Freshness01 == 1.0 && career.Fatigue01 == 0.0));
    }

    [Fact]
    public void SchemaVersionElevenRoundTripsAfterRollover()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "wt-v11-rollover.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        AdvanceToDay(source, 365);
        string checksumBefore = WorldChecksum.Compute(source.World!);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(11, SqliteWorldSaveStore.SchemaVersion);
        Assert.Equal(2027, loaded.World!.SeasonYear);
        Assert.Equal(checksumBefore, WorldChecksum.Compute(loaded.World));
    }

    private static GameApplication CreateWorldSkippingPlayerRaces()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed, UaeOriginId)).Succeeded);
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        SkipAllRacesAndConfirm(application);
        return application;
    }

    private static void SkipAllRacesAndConfirm(GameApplication application)
    {
        foreach (PreSeasonRaceEntryProjection race in application.PreSeasonPlanning!.Races)
        {
            Assert.True(application.Execute(new SetSeasonRaceEntryCommand(race.RaceContentId, Entered: false)).Succeeded);
        }

        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);
    }

    private static void AdvanceToDay(GameApplication application, int targetDayNumber)
    {
        WorldState world = application.World!;
        while (world.CurrentDate.DayNumber < targetDayNumber)
        {
            if (application.State == GameState.PreSeasonPlanningFlow)
            {
                SkipAllRacesAndConfirm(application);
            }

            world.AdvanceOneDay();
        }
    }

    private static void AdvanceToDayThroughApplication(GameApplication application, int targetDayNumber)
    {
        while (application.World!.CurrentDate.DayNumber < targetDayNumber)
        {
            if (application.State == GameState.PreSeasonPlanningFlow)
            {
                SkipAllRacesAndConfirm(application);
            }

            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }
    }
}
