using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record CourseSparkline(
    IReadOnlyList<int> Heights,
    int DistanceKm,
    int ElevationGainM,
    string ClassifiedStageType);

public static class CourseSparklineQueries
{
    private const int TargetPointCount = 24;

    public static CourseSparkline? BuildFromCourseProfile(CourseProfile? profile)
    {
        if (profile is null || profile.Samples.Count < 2)
        {
            return null;
        }

        double[] elevations = DownsampleElevations(profile.Samples);
        if (elevations.Length < 2)
        {
            return null;
        }

        double min = elevations.Min();
        double max = elevations.Max();
        int[] heights = elevations
            .Select(elevation => NormalizeElevation(elevation, min, max))
            .ToArray();

        return new CourseSparkline(
            heights,
            (int)Math.Round(profile.LengthM / 1000.0, MidpointRounding.AwayFromZero),
            (int)Math.Round(profile.ElevationGainM, MidpointRounding.AwayFromZero),
            profile.ClassifiedStageType.ToString());
    }

    public static CourseSparkline? BuildFromCalendarEntry(WorldState world, CalendarEntry entry)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.CourseProfileId is not WorldEntityId profileId)
        {
            return null;
        }

        return BuildFromCourseProfile(world.TryGetCourseProfile(profileId));
    }

    private static double[] DownsampleElevations(IReadOnlyList<CourseSampleVertex> samples)
    {
        if (samples.Count < 2)
        {
            return Array.Empty<double>();
        }

        double[] elevations = new double[TargetPointCount];
        for (int index = 0; index < TargetPointCount; index++)
        {
            int sampleIndex = index * (samples.Count - 1) / (TargetPointCount - 1);
            if (sampleIndex % 2 != 0 && sampleIndex > 0 && sampleIndex < samples.Count - 1)
            {
                sampleIndex--;
            }

            elevations[index] = samples[sampleIndex].ElevationM;
        }

        elevations[0] = samples[0].ElevationM;
        elevations[^1] = samples[^1].ElevationM;
        return elevations;
    }

    private static int NormalizeElevation(double elevation, double min, double max)
    {
        if (Math.Abs(max - min) < double.Epsilon)
        {
            return 10;
        }

        double normalized = (elevation - min) / (max - min) * 20.0;
        return Math.Clamp((int)Math.Round(normalized, MidpointRounding.AwayFromZero), 0, 20);
    }
}
