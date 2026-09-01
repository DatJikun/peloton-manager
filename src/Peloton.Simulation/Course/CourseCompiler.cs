using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Simulation.Course;

public static class CourseCompiler
{
    public static RaceDefinition ToRaceDefinition(CourseProfile profile, CourseWeather weather, string routeId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(weather);
        ArgumentException.ThrowIfNullOrWhiteSpace(routeId);

        List<RaceRouteSegment> segments = new(profile.Samples.Count - 1);
        for (int i = 0; i < profile.Samples.Count - 1; i++)
        {
            CourseSampleVertex current = profile.Samples[i];
            CourseSampleVertex next = profile.Samples[i + 1];
            double gradient = (next.ElevationM - current.ElevationM) / CourseMetrics.SampleSpacingM;
            double yaw = NormalizeDegrees(weather.WindFromDegrees - current.HeadingDegrees);
            double windSpeed = weather.WindSpeedMps * (current.Exposure01 * 0.65 + 0.35);
            segments.Add(new RaceRouteSegment(
                $"{routeId}.seg.{i}",
                CourseMetrics.SampleSpacingM,
                gradient,
                current.WidthM,
                windSpeed,
                yaw,
                MapSurface(current.Surface)));
        }

        return new RaceDefinition(routeId, 1.225, segments);
    }

    public static int MaximumDurationSeconds(CourseProfile profile) =>
        Math.Max(3600, (int)Math.Ceiling(profile.LengthM / 3.0) + 1800);

    public static double EffectiveRollingResistance(double baseCrr, double handling, CourseSurface surface)
    {
        double surfaceDelta = surface switch
        {
            CourseSurface.Asphalt => 0.0,
            CourseSurface.WhiteRoad => 0.0025,
            CourseSurface.Gravel => 0.0050,
            CourseSurface.Cobble => 0.0085,
            _ => 0.0,
        };

        return baseCrr + surfaceDelta * (1.35 - 0.50 * handling);
    }

    private static RouteSurface MapSurface(CourseSurface surface) => surface switch
    {
        CourseSurface.Cobble => RouteSurface.Cobble,
        CourseSurface.Gravel => RouteSurface.Gravel,
        CourseSurface.WhiteRoad => RouteSurface.WhiteRoad,
        _ => RouteSurface.Asphalt,
    };

    private static double NormalizeDegrees(double degrees)
    {
        double normalized = degrees % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }
}
