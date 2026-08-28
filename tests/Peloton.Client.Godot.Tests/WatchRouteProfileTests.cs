using System.Linq;
using Peloton.Client.Godot;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class WatchRouteProfileTests
{
    [Fact]
    public void BuildSamplesTheLibraryProfileAlongThePolyline()
    {
        RaceWatchCourse course = new(
            5400.0,
            new[]
            {
                new RaceWatchCourseSegment("race-segment.peloton.flat-open", 1800.0, 0.0, 6.0, 2.0, 0.0),
                new RaceWatchCourseSegment("race-segment.peloton.sustained-climb", 1800.0, 0.05, 5.0, 1.0, 20.0),
                new RaceWatchCourseSegment("race-segment.peloton.exposed-crosswind", 1800.0, 0.0, 3.2, 10.0, 90.0),
            });

        WatchRoutePoint[] points = WatchRouteProfile.Build(course);

        Assert.True(points.Length > 12);
        Assert.True(points.Any(point => point.Kind == RouteTerrainKind.Climb && point.Gradient > 0.06));
        Assert.True(points.Any(point => point.Kind == RouteTerrainKind.Crosswind && point.RoadWidthM < 3.0));
        (double x, double y) start = WatchRouteProfile.PointOnPolyline(points, 0.0, 0, 100, 10, 90);
        (double x, double y) finish = WatchRouteProfile.PointOnPolyline(points, 5400.0, 0, 100, 10, 90);
        Assert.Equal(0.0, start.x, 6);
        Assert.Equal(100.0, finish.x, 6);
        Assert.True(finish.y < start.y);
    }
}
