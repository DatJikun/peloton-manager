using System;
using System.Collections.Generic;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public sealed record WatchRoutePoint(
    double DistanceM,
    double ElevationM,
    double Gradient,
    double RoadWidthM,
    RouteTerrainKind Kind);

public static class WatchRouteProfile
{
    public static WatchRoutePoint[] Build(RaceWatchCourse course)
    {
        ArgumentNullException.ThrowIfNull(course);
        IReadOnlyList<RouteProfileSample> samples = RouteProfileGenerator.Expand(course);
        WatchRoutePoint[] points = new WatchRoutePoint[samples.Count];
        for (int index = 0; index < samples.Count; index++)
        {
            RouteProfileSample sample = samples[index];
            points[index] = new WatchRoutePoint(
                sample.DistanceM,
                sample.ElevationM,
                sample.Gradient,
                sample.RoadWidthM,
                sample.Kind);
        }

        return points;
    }

    public static (double X, double Y) PointOnPolyline(
        IReadOnlyList<WatchRoutePoint> points,
        double distanceM,
        double left,
        double right,
        double top,
        double bottom)
    {
        if (points.Count == 0)
        {
            return ((left + right) / 2.0, (top + bottom) / 2.0);
        }

        double minElevation = points[0].ElevationM;
        double maxElevation = points[0].ElevationM;
        double totalLength = points[^1].DistanceM;
        for (int index = 1; index < points.Count; index++)
        {
            minElevation = Math.Min(minElevation, points[index].ElevationM);
            maxElevation = Math.Max(maxElevation, points[index].ElevationM);
        }

        double span = Math.Max(12.0, maxElevation - minElevation);
        double target = Math.Clamp(distanceM, 0.0, totalLength);
        WatchRoutePoint from = points[0];
        WatchRoutePoint to = points[^1];
        for (int index = 0; index < points.Count - 1; index++)
        {
            if (target <= points[index + 1].DistanceM)
            {
                from = points[index];
                to = points[index + 1];
                break;
            }
        }

        double segmentLength = Math.Max(1e-6, to.DistanceM - from.DistanceM);
        double t = (target - from.DistanceM) / segmentLength;
        double elevation = from.ElevationM + ((to.ElevationM - from.ElevationM) * t);
        double x = left + ((target / Math.Max(1e-6, totalLength)) * (right - left));
        double y = bottom - (((elevation - minElevation) / span) * (bottom - top));
        return (x, y);
    }

    public static bool IsCrosswind(RaceWatchCourseSegment segment)
    {
        return RouteProfileGenerator.IsCrosswind(segment);
    }
}
