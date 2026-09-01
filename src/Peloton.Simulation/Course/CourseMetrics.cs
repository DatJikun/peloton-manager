using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Simulation.Course;

public static class CourseMetrics
{
    public const double SampleSpacingM = 25.0;

    public static double GradientAt(IReadOnlyList<CourseSampleVertex> samples, int index)
    {
        if (samples.Count < 2)
        {
            return 0.0;
        }

        if (index <= 0)
        {
            return (samples[1].ElevationM - samples[0].ElevationM) / SampleSpacingM;
        }

        if (index >= samples.Count - 1)
        {
            int last = samples.Count - 1;
            return (samples[last].ElevationM - samples[last - 1].ElevationM) / SampleSpacingM;
        }

        return (samples[index + 1].ElevationM - samples[index - 1].ElevationM) / (2.0 * SampleSpacingM);
    }

    public static (double LengthM, double GainM, double LossM, double CobbleM, double GravelM, double MaxGrad, double MinGrad)
        Compute(IReadOnlyList<CourseSampleVertex> samples)
    {
        if (samples.Count < 2)
        {
            return (0, 0, 0, 0, 0, 0, 0);
        }

        double lengthM = samples[^1].DistanceM;
        double gain = 0;
        double loss = 0;
        double cobbleM = 0;
        double gravelM = 0;
        double maxGrad = double.MinValue;
        double minGrad = double.MaxValue;

        for (int i = 1; i < samples.Count; i++)
        {
            double delta = samples[i].ElevationM - samples[i - 1].ElevationM;
            if (delta > 0)
            {
                gain += delta;
            }
            else
            {
                loss += -delta;
            }

            double grad = delta / SampleSpacingM;
            maxGrad = Math.Max(maxGrad, grad);
            minGrad = Math.Min(minGrad, grad);

            CourseSurface surface = samples[i].Surface;
            if (surface == CourseSurface.Cobble)
            {
                cobbleM += SampleSpacingM;
            }
            else if (surface is CourseSurface.Gravel or CourseSurface.WhiteRoad)
            {
                gravelM += SampleSpacingM;
            }
        }

        return (lengthM, gain, loss, cobbleM, gravelM, maxGrad, minGrad);
    }
}
