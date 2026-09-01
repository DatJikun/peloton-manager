using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Simulation.Course;

public static class CourseClassifier
{
    private const double ClimbMeanGradient = 0.04;
    private const double ClimbMinLengthM = 1000.0;
    private const double ClimbMinGainM = 50.0;

    public static ClassifiedStageType Classify(
        CourseKind kind,
        IReadOnlyList<CourseSampleVertex> samples,
        double lengthM,
        double elevationGainM,
        double cobbleM)
    {
        if (kind == CourseKind.IndividualTimeTrial)
        {
            return ClassifiedStageType.IndividualTimeTrial;
        }

        if (kind == CourseKind.TeamTimeTrial)
        {
            return ClassifiedStageType.TeamTimeTrial;
        }

        if (lengthM > 0 && cobbleM / lengthM >= 0.12)
        {
            return ClassifiedStageType.CobbleClassic;
        }

        double maxElevation = samples.Max(vertex => vertex.ElevationM);
        double last3kmStart = Math.Max(0, lengthM - 3000);
        var last3km = samples.Where(vertex => vertex.DistanceM >= last3kmStart).ToArray();
        double maxInLast3 = last3km.Length == 0 ? 0 : last3km.Max(vertex => vertex.ElevationM);
        double meanGradLast3 = MeanGradient(samples, last3kmStart, lengthM);

        if (elevationGainM >= 2800 &&
            maxInLast3 >= maxElevation - 80 &&
            meanGradLast3 >= 0.04)
        {
            return ClassifiedStageType.MountainSummit;
        }

        if (elevationGainM >= 2800 ||
            (elevationGainM >= 2000 && LongestClimbM(samples) >= 8000))
        {
            return ClassifiedStageType.Mountain;
        }

        if (elevationGainM >= 1200 || CountClimbs(samples) >= 4)
        {
            return ClassifiedStageType.Hilly;
        }

        if (elevationGainM < 800 && cobbleM < 8000)
        {
            return ClassifiedStageType.Flat;
        }

        return ClassifiedStageType.Mixed;
    }

    private static double MeanGradient(IReadOnlyList<CourseSampleVertex> samples, double startM, double endM)
    {
        var segment = samples.Where(vertex => vertex.DistanceM >= startM && vertex.DistanceM <= endM).ToArray();
        if (segment.Length < 2)
        {
            return 0;
        }

        double rise = segment[^1].ElevationM - segment[0].ElevationM;
        double dist = segment[^1].DistanceM - segment[0].DistanceM;
        return dist <= 0 ? 0 : rise / dist;
    }

    private static double LongestClimbM(IReadOnlyList<CourseSampleVertex> samples)
    {
        double longest = 0;
        double runStart = 0;
        double runGain = 0;
        for (int i = 1; i < samples.Count; i++)
        {
            double delta = samples[i].ElevationM - samples[i - 1].ElevationM;
            double grad = delta / CourseMetrics.SampleSpacingM;
            if (grad >= ClimbMeanGradient)
            {
                if (runGain <= 0)
                {
                    runStart = samples[i - 1].DistanceM;
                }

                runGain += delta;
            }
            else if (runGain >= ClimbMinGainM)
            {
                double runLength = samples[i - 1].DistanceM - runStart;
                if (runLength >= ClimbMinLengthM)
                {
                    longest = Math.Max(longest, runLength);
                }

                runGain = 0;
            }
            else
            {
                runGain = 0;
            }
        }

        return longest;
    }

    private static int CountClimbs(IReadOnlyList<CourseSampleVertex> samples)
    {
        int count = 0;
        double runStart = 0;
        double runGain = 0;
        for (int i = 1; i < samples.Count; i++)
        {
            double delta = samples[i].ElevationM - samples[i - 1].ElevationM;
            double grad = delta / CourseMetrics.SampleSpacingM;
            if (grad >= ClimbMeanGradient)
            {
                if (runGain <= 0)
                {
                    runStart = samples[i - 1].DistanceM;
                }

                runGain += delta;
            }
            else if (runGain >= ClimbMinGainM)
            {
                double runLength = samples[i - 1].DistanceM - runStart;
                if (runLength >= 2000)
                {
                    count++;
                }

                runGain = 0;
            }
            else
            {
                runGain = 0;
            }
        }

        return count;
    }
}
