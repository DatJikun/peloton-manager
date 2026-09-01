using System;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation.Course;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CourseEngineIntegrationTests
{
    private const long GateSeed = 91234;
    private const string WtScenarioId = "scenario.peloton.wt-2026";

    [Fact]
    public void Seed91234CatalogHasAtLeastEightFlatStages()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        int flatCount = application.World!.CourseProfiles
            .Count(profile => profile.ClassifiedStageType == ClassifiedStageType.Flat);
        Assert.True(flatCount >= 8);
    }

    [Fact]
    public void CopenhagenSprintIsClassifiedFlatWithModestGain()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        CourseProfile copenhagen = application.World!.CourseProfiles.Single(
            profile => string.Equals(profile.RaceContentId, "race.wt2026.copenhagen_sprint", StringComparison.Ordinal));
        Assert.Equal(ClassifiedStageType.Flat, copenhagen.ClassifiedStageType);
        Assert.True(copenhagen.ElevationGainM < 2000);
    }

    [Fact]
    public void TourDownUnderIncludesAtLeastOneFlatStage()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        bool hasFlat = application.World!.CourseProfiles
            .Where(profile => string.Equals(profile.RaceContentId, "race.wt2026.tour_down_under", StringComparison.Ordinal))
            .Any(profile => profile.ClassifiedStageType == ClassifiedStageType.Flat);
        Assert.True(hasFlat);
    }

    [Fact]
    public void TourDeFranceHasFlatMountainAndIttWithoutExtremeGain()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        CourseProfile[] tdf = application.World!.CourseProfiles
            .Where(profile => string.Equals(profile.RaceContentId, "race.wt2026.tdf", StringComparison.Ordinal))
            .OrderBy(profile => profile.StageIndex)
            .ToArray();
        Assert.Equal(21, tdf.Length);
        Assert.Contains(tdf, stage => stage.ClassifiedStageType == ClassifiedStageType.Flat);
        Assert.Contains(tdf, stage => stage.ClassifiedStageType is ClassifiedStageType.Mountain or ClassifiedStageType.MountainSummit);
        Assert.Contains(tdf, stage => stage.ClassifiedStageType == ClassifiedStageType.IndividualTimeTrial);
        Assert.All(tdf, stage => Assert.True(stage.ElevationGainM <= 8000));
    }

    [Fact]
    public void TourDeFrance2026HasTwentyOneDenseStages()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        CourseProfile[] tdf = world.CourseProfiles
            .Where(profile => string.Equals(profile.RaceContentId, "race.wt2026.tdf", StringComparison.Ordinal))
            .OrderBy(profile => profile.StageIndex)
            .ToArray();
        Assert.Equal(21, tdf.Length);
        foreach (CourseProfile stage in tdf)
        {
            int expected = (int)(stage.LengthM / CourseMetrics.SampleSpacingM) + 1;
            Assert.InRange(stage.Samples.Count, expected - 2, expected + 2);
            Assert.True(stage.Samples.Count >= 200);
        }
    }

    [Fact]
    public void RoubaixCobbleBandAndClassifier()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        CourseProfile roubaix = application.World!.CourseProfiles.Single(
            profile => string.Equals(profile.RaceContentId, "race.wt2026.roubaix", StringComparison.Ordinal));
        double cobbleKm = roubaix.CobbleM / 1000.0;
        Assert.InRange(cobbleKm, 45, 70);
        Assert.Equal(ClassifiedStageType.CobbleClassic, roubaix.ClassifiedStageType);
    }

    [Fact]
    public void WorldTourTduStageOneUsesLongStoredCourse()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        CalendarEntry tduStage1 = application.World!.CalendarEntries
            .Where(entry => string.Equals(entry.RaceContentId, "race.wt2026.tour_down_under", StringComparison.Ordinal))
            .OrderBy(entry => entry.StageIndex)
            .First();
        CourseProfile profile = application.World.TryGetCourseProfile(tduStage1.CourseProfileId!.Value)!;
        Assert.True(profile.LengthM > 50_000);
    }

    [Fact]
    public void SchemaVersionEightRoundTripsCourseSamples()
    {
        using TemporaryDirectory temp = new();
        string savePath = System.IO.Path.Combine(temp.Path, "wt-v8.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        Assert.True(source.World!.CourseProfiles.Count > 100);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(source.World.CourseProfiles.Count, loaded.World!.CourseProfiles.Count);
        Assert.Equal(
            source.World.CourseProfiles[0].Samples.Count,
            loaded.World.CourseProfiles[0].Samples.Count);
    }
}
