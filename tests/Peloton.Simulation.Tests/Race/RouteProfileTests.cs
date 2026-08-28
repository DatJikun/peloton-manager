using System;
using System.Linq;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class RouteProfileTests
{
    [Fact]
    public void LibraryHasThreeAuthoredVariantsForEachTerrainKind()
    {
        RouteTerrainKind[] kinds = Enum.GetValues<RouteTerrainKind>();
        Assert.Equal(5, kinds.Length);
        Assert.Equal(kinds.Length * RouteProfileLibrary.VariantsPerKind, RouteProfileLibrary.All.Count);
        foreach (RouteTerrainKind kind in kinds)
        {
            RouteProfileTemplate[] variants = RouteProfileLibrary.All
                .Where(template => template.Kind == kind)
                .OrderBy(template => template.Variant)
                .ToArray();
            Assert.Equal(3, variants.Length);
            Assert.Equal(0, variants[0].Variant);
            Assert.Equal(1, variants[1].Variant);
            Assert.Equal(2, variants[2].Variant);
            Assert.All(variants, template =>
            {
                Assert.Equal(0.0, template.Knots[0].T);
                Assert.Equal(1.0, template.Knots[^1].T);
                Assert.True(template.Knots.Count >= 5);
            });
        }
    }

    [Fact]
    public void PrototypeExpansionIsDeterministicAndNotThreeStraightRamps()
    {
        RaceWatchCourse course = PrototypeCourse();
        RouteProfileSample[] first = RouteProfileGenerator.Expand(course).ToArray();
        RouteProfileSample[] second = RouteProfileGenerator.Expand(course).ToArray();

        Assert.Equal(first.Select(sample => sample.ElevationM), second.Select(sample => sample.ElevationM));
        Assert.True(first.Length >= 100);

        RouteProfileSample[] climb = first
            .Where(sample => sample.Kind == RouteTerrainKind.Climb)
            .ToArray();
        Assert.True(climb.Length >= 20);
        double climbGain = climb[^1].ElevationM - climb[0].ElevationM;
        Assert.InRange(climbGain, 85.0, 95.0);
        Assert.True(climb.Max(sample => sample.Gradient) > 0.065);
        Assert.True(climb.Min(sample => sample.Gradient) < 0.035);

        double linearStart = climb[0].ElevationM;
        double maxDeviation = climb.Max(sample =>
        {
            double t = (sample.DistanceM - climb[0].DistanceM) / Math.Max(1.0, climb[^1].DistanceM - climb[0].DistanceM);
            double linear = linearStart + (t * climbGain);
            return Math.Abs(sample.ElevationM - linear);
        });
        Assert.True(maxDeviation > 4.0);

        RouteProfileSample[] flat = first
            .Where(sample => sample.Kind == RouteTerrainKind.Flat)
            .ToArray();
        Assert.All(flat, sample => Assert.InRange(sample.ElevationM, -12.0, 12.0));

        RouteProfileSample[] crosswind = first
            .Where(sample => sample.Kind == RouteTerrainKind.Crosswind)
            .ToArray();
        Assert.True(crosswind.Min(sample => sample.RoadWidthM) < 3.0);
        Assert.Equal(RouteTerrainKind.Flat, RouteProfileGenerator.Classify(course.Segments[0]));
        Assert.Equal(RouteTerrainKind.Climb, RouteProfileGenerator.Classify(course.Segments[1]));
        Assert.Equal(RouteTerrainKind.Crosswind, RouteProfileGenerator.Classify(course.Segments[2]));
    }

    [Fact]
    public void GeneratorComposesSeededCoursesFromTheLibrary()
    {
        RouteTerrainKind[] composition =
        {
            RouteTerrainKind.Flat,
            RouteTerrainKind.Climb,
            RouteTerrainKind.Descent,
            RouteTerrainKind.Rolling,
            RouteTerrainKind.Crosswind,
        };
        RaceWatchCourse first = RouteProfileGenerator.GenerateCourse(42, 9000.0, composition);
        RaceWatchCourse second = RouteProfileGenerator.GenerateCourse(42, 9000.0, composition);
        RaceWatchCourse otherSeed = RouteProfileGenerator.GenerateCourse(99, 9000.0, composition);

        Assert.Equal(9000.0, first.TotalLengthM);
        Assert.Equal(5, first.Segments.Count);
        Assert.Equal(first.Segments.Select(segment => segment.Id), second.Segments.Select(segment => segment.Id));
        Assert.Equal(
            RouteProfileGenerator.Expand(first).Select(sample => sample.ElevationM),
            RouteProfileGenerator.Expand(second).Select(sample => sample.ElevationM));
        Assert.Equal(composition, first.Segments.Select(RouteProfileGenerator.Classify));
        Assert.Contains(
            true,
            composition.Select((kind, index) =>
                RouteProfileGenerator.VariantFor(first.Segments[index].Id)
                != RouteProfileGenerator.VariantFor(otherSeed.Segments[index].Id)));

        RouteProfileSample[] samples = RouteProfileGenerator.Expand(first).ToArray();
        Assert.Equal(composition.Length, samples.Select(sample => sample.Kind).Distinct().Count());
        Assert.InRange(samples[^1].ElevationM - samples[0].ElevationM, -130.0, 20.0);
    }

    private static RaceWatchCourse PrototypeCourse()
    {
        return new RaceWatchCourse(
            5400.0,
            new[]
            {
                new RaceWatchCourseSegment("race-segment.peloton.flat-open", 1800.0, 0.0, 6.0, 2.0, 0.0),
                new RaceWatchCourseSegment("race-segment.peloton.sustained-climb", 1800.0, 0.05, 5.0, 1.0, 20.0),
                new RaceWatchCourseSegment("race-segment.peloton.exposed-crosswind", 1800.0, 0.0, 3.2, 10.0, 90.0),
            });
    }
}
