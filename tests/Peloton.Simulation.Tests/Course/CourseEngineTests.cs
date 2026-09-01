using System;
using System.Diagnostics;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Course;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Course;

public sealed class CourseEngineTests
{
    [Fact]
    public void SegmentAtBinarySearchIsFastForLargeProfile()
    {
        DeterministicRng rng = new(99);
        var samples = CourseBricks.BuildRolling(rng, 250_000, 100);
        var segments = new System.Collections.Generic.List<RaceRouteSegment>();
        for (int i = 0; i < samples.Count - 1; i++)
        {
            double gradient = (samples[i + 1].ElevationM - samples[i].ElevationM) / CourseMetrics.SampleSpacingM;
            segments.Add(new RaceRouteSegment($"seg-{i}", CourseMetrics.SampleSpacingM, gradient, 6, 0, 0));
        }

        RaceDefinition definition = new("perf", 1.225, segments);
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int lookup = 0; lookup < 20_000; lookup++)
        {
            _ = definition.SegmentAt(lookup * 12.5);
        }

        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 500);
    }

    [Fact]
    public void HandlingReducesCobbleRollingResistance()
    {
        double low = CourseCompiler.EffectiveRollingResistance(0.004, 0.5, CourseSurface.Cobble);
        double high = CourseCompiler.EffectiveRollingResistance(0.004, 0.95, CourseSurface.Cobble);
        Assert.True(high < low);
    }
}
