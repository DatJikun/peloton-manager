using System;
using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public static class CourseSparklineQueries
{
    public const int PointCount = 12;
    public const int HeightFloor = 4;
    public const int HeightCeil = 22;

    public static int[] Build(CourseProfile? profile)
    {
        if (profile is null || profile.Samples.Count < 2)
        {
            return Array.Empty<int>();
        }

        IReadOnlyList<CourseSampleVertex> samples = profile.Samples;
        double[] elevations = new double[PointCount];
        for (int i = 0; i < PointCount; i++)
        {
            int index = i * (samples.Count - 1) / (PointCount - 1);
            elevations[i] = samples[index].ElevationM;
        }

        double min = elevations[0];
        double max = elevations[0];
        for (int i = 1; i < PointCount; i++)
        {
            if (elevations[i] < min)
            {
                min = elevations[i];
            }

            if (elevations[i] > max)
            {
                max = elevations[i];
            }
        }

        int[] sparkline = new int[PointCount];
        if (Math.Abs(max - min) < double.Epsilon)
        {
            int flatHeight = (HeightFloor + HeightCeil) / 2;
            Array.Fill(sparkline, flatHeight);
            return sparkline;
        }

        double scale = (HeightCeil - HeightFloor) / (max - min);
        for (int i = 0; i < PointCount; i++)
        {
            double normalized = HeightFloor + ((elevations[i] - min) * scale);
            int height = (int)Math.Round(normalized, MidpointRounding.AwayFromZero);
            sparkline[i] = Math.Clamp(height, HeightFloor, HeightCeil);
        }

        return sparkline;
    }
}
