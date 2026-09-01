using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation;

namespace Peloton.Simulation.Course;

public static class CourseBricks
{
    public static List<CourseSampleVertex> BuildFlatRoad(DeterministicRng rng, double lengthM, double baseElevationM)
    {
        int count = VertexCount(lengthM);
        List<CourseSampleVertex> vertices = new(count);
        double elevation = baseElevationM;
        for (int i = 0; i < count; i++)
        {
            double distance = i * CourseMetrics.SampleSpacingM;
            if (i > 0)
            {
                double noise = (rng.NextUnit() - 0.5) * 0.16;
                if (i % 20 == 0)
                {
                    noise += 0.025 * (rng.NextUnit() > 0.5 ? 1 : -1);
                }

                elevation += noise * CourseMetrics.SampleSpacingM;
            }

            vertices.Add(new CourseSampleVertex(
                distance,
                elevation,
                6.0 + rng.NextUnit() * 1.5,
                rng.NextUnit() * 360,
                CourseSurface.Asphalt,
                rng.NextUnit() * 0.15,
                rng.NextUnit() * 0.35));
        }

        return vertices;
    }

    public static List<CourseSampleVertex> BuildRolling(DeterministicRng rng, double lengthM, double baseElevationM)
    {
        int count = VertexCount(lengthM);
        List<CourseSampleVertex> vertices = new(count);
        double elevation = baseElevationM;
        double phase = rng.NextUnit() * Math.PI * 2;
        for (int i = 0; i < count; i++)
        {
            double distance = i * CourseMetrics.SampleSpacingM;
            if (i > 0)
            {
                double wave = Math.Sin((distance / 1500.0) + phase) * 0.045;
                double noise = (rng.NextUnit() - 0.5) * 0.01;
                elevation += (wave + noise) * CourseMetrics.SampleSpacingM;
            }

            vertices.Add(new CourseSampleVertex(
                distance,
                elevation,
                5.5 + rng.NextUnit(),
                rng.NextUnit() * 360,
                CourseSurface.Asphalt,
                rng.NextUnit() * 0.25,
                0.3 + rng.NextUnit() * 0.4));
        }

        return vertices;
    }

    public static List<CourseSampleVertex> BuildClimb(
        DeterministicRng rng,
        double lengthM,
        double meanGradient,
        double baseElevationM,
        string shape = "generic")
    {
        int count = VertexCount(lengthM);
        List<CourseSampleVertex> vertices = new(count);
        double elevation = baseElevationM;
        double targetRise = lengthM * meanGradient;
        double accumulated = 0;
        for (int i = 0; i < count; i++)
        {
            double distance = i * CourseMetrics.SampleSpacingM;
            double t = count <= 1 ? 0 : (double)i / (count - 1);
            double shapeFactor = shape switch
            {
                "alpe" => 0.85 + 0.3 * Math.Pow(Math.Sin(t * Math.PI), 1.5),
                "pyrenean" => 0.9 + 0.2 * Math.Sin(t * Math.PI),
                "wall" => 0.8 + 0.4 * t,
                "cipressa" => 0.75 + 0.35 * Math.Pow(t, 0.7),
                "poggio" => t < 0.85 ? 0.55 : 1.2,
                _ => 1.0,
            };
            double stepRise = count <= 1 ? targetRise : (targetRise / (count - 1)) * shapeFactor;
            double noise = (rng.NextUnit() - 0.5) * 0.008 * CourseMetrics.SampleSpacingM;
            if (i > 0)
            {
                elevation += stepRise + noise;
                accumulated += stepRise;
            }

            double curvature = t > 0.66 ? 0.4 + rng.NextUnit() * 0.5 : rng.NextUnit() * 0.2;
            vertices.Add(new CourseSampleVertex(
                distance,
                elevation,
                5.0 + rng.NextUnit() * 2.0,
                rng.NextUnit() * 360,
                CourseSurface.Asphalt,
                curvature,
                0.4 + rng.NextUnit() * 0.5));
        }

        return vertices;
    }

    public static List<CourseSampleVertex> BuildSummitFinish(
        DeterministicRng rng,
        double approachM,
        double climbM,
        double climbGradient,
        double baseElevationM,
        string shape = "generic")
    {
        List<CourseSampleVertex> approach = BuildRolling(rng, approachM, baseElevationM);
        double climbStartElev = approach[^1].ElevationM;
        List<CourseSampleVertex> climb = BuildClimb(rng, climbM, climbGradient, climbStartElev, shape);
        return Concatenate(approach, climb, skipFirst: true);
    }

    public static List<CourseSampleVertex> BuildDescent(DeterministicRng rng, double lengthM, double startElevationM, double dropM)
    {
        int count = VertexCount(lengthM);
        List<CourseSampleVertex> vertices = new(count);
        double elevation = startElevationM;
        double perStep = count <= 1 ? 0 : -dropM / (count - 1);
        for (int i = 0; i < count; i++)
        {
            double distance = i * CourseMetrics.SampleSpacingM;
            if (i > 0)
            {
                elevation += perStep + (rng.NextUnit() - 0.5) * 0.01 * CourseMetrics.SampleSpacingM;
            }

            vertices.Add(new CourseSampleVertex(
                distance,
                elevation,
                4.0 + rng.NextUnit() * 1.5,
                rng.NextUnit() * 360,
                CourseSurface.Asphalt,
                0.3 + rng.NextUnit() * 0.6,
                0.5 + rng.NextUnit() * 0.4));
        }

        return vertices;
    }

    public static List<CourseSampleVertex> BuildCobbleSector(DeterministicRng rng, double lengthM, double baseElevationM)
    {
        int count = VertexCount(lengthM);
        List<CourseSampleVertex> vertices = new(count);
        double elevation = baseElevationM;
        for (int i = 0; i < count; i++)
        {
            double distance = i * CourseMetrics.SampleSpacingM;
            if (i > 0)
            {
                elevation += (rng.NextUnit() - 0.5) * 0.08;
            }

            vertices.Add(new CourseSampleVertex(
                distance,
                elevation,
                3.0 + rng.NextUnit() * 1.2,
                rng.NextUnit() * 360,
                CourseSurface.Cobble,
                rng.NextUnit() * 0.2,
                0.2 + rng.NextUnit() * 0.3));
        }

        return vertices;
    }

    public static List<CourseSampleVertex> BuildBerg(DeterministicRng rng, double lengthM, double meanGradient, double baseElevationM)
    {
        return BuildClimb(rng, lengthM, meanGradient, baseElevationM, "wall");
    }

    public static List<CourseSampleVertex> BuildIttOutAndBack(DeterministicRng rng, double lengthM, double baseElevationM)
    {
        int count = VertexCount(lengthM);
        List<CourseSampleVertex> vertices = new(count);
        double elevation = baseElevationM;
        for (int i = 0; i < count; i++)
        {
            double distance = i * CourseMetrics.SampleSpacingM;
            if (i > 0)
            {
                elevation += (rng.NextUnit() - 0.5) * 0.004 * CourseMetrics.SampleSpacingM;
            }

            vertices.Add(new CourseSampleVertex(
                distance,
                elevation,
                7.0 + rng.NextUnit() * 1.5,
                rng.NextUnit() * 360,
                CourseSurface.Asphalt,
                rng.NextUnit() * 0.08,
                0.2 + rng.NextUnit() * 0.2));
        }

        return vertices;
    }

    public static List<CourseSampleVertex> BuildCoastalExposed(DeterministicRng rng, double lengthM, double baseElevationM)
    {
        List<CourseSampleVertex> flat = BuildFlatRoad(rng, lengthM, baseElevationM);
        List<CourseSampleVertex> adjusted = new(flat.Count);
        foreach (CourseSampleVertex vertex in flat)
        {
            adjusted.Add(vertex with { Exposure01 = 0.75 + rng.NextUnit() * 0.25 });
        }

        return adjusted;
    }

    public static List<CourseSampleVertex> BuildWhiteRoad(DeterministicRng rng, double lengthM, double baseElevationM)
    {
        int count = VertexCount(lengthM);
        List<CourseSampleVertex> vertices = new(count);
        double elevation = baseElevationM;
        for (int i = 0; i < count; i++)
        {
            double distance = i * CourseMetrics.SampleSpacingM;
            if (i > 0)
            {
                elevation += (rng.NextUnit() - 0.5) * 0.06;
            }

            vertices.Add(new CourseSampleVertex(
                distance,
                elevation,
                4.5 + rng.NextUnit(),
                rng.NextUnit() * 360,
                CourseSurface.WhiteRoad,
                rng.NextUnit() * 0.15,
                0.5 + rng.NextUnit() * 0.3));
        }

        return vertices;
    }

    public static List<CourseSampleVertex> SmoothJoins(IReadOnlyList<CourseSampleVertex> vertices, int blendVertices = 8)
    {
        if (vertices.Count < 4)
        {
            return vertices.ToList();
        }

        List<CourseSampleVertex> result = vertices.ToList();
        for (int join = blendVertices; join < result.Count; join += Math.Max(blendVertices * 4, 40))
        {
            int start = Math.Max(0, join - blendVertices);
            int end = Math.Min(result.Count - 1, join + blendVertices);
            double startElev = result[start].ElevationM;
            double endElev = result[end].ElevationM;
            for (int i = start; i <= end; i++)
            {
                double t = end == start ? 0 : (double)(i - start) / (end - start);
                double blended = startElev + (endElev - startElev) * t;
                CourseSampleVertex current = result[i];
                result[i] = current with { ElevationM = blended * 0.35 + current.ElevationM * 0.65 };
            }
        }

        return result;
    }

    public static List<CourseSampleVertex> Concatenate(
        IReadOnlyList<CourseSampleVertex> first,
        IReadOnlyList<CourseSampleVertex> second,
        bool skipFirst = false)
    {
        List<CourseSampleVertex> result = new(first.Count + second.Count);
        result.AddRange(first);
        if (second.Count == 0)
        {
            return Renumber(result);
        }

        double offset = first[^1].DistanceM;
        int start = skipFirst ? 1 : 0;
        for (int i = start; i < second.Count; i++)
        {
            CourseSampleVertex vertex = second[i];
            double distance = offset + (i - start + (skipFirst ? 1 : 0)) * CourseMetrics.SampleSpacingM;
            result.Add(vertex with { DistanceM = distance });
        }

        return Renumber(result);
    }

    private static List<CourseSampleVertex> Renumber(List<CourseSampleVertex> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = vertices[i] with { DistanceM = i * CourseMetrics.SampleSpacingM };
        }

        return vertices;
    }

    private static int VertexCount(double lengthM) =>
        Math.Max(2, (int)Math.Round(lengthM / CourseMetrics.SampleSpacingM) + 1);
}

internal static class CourseRng
{
    public static double NextUnit(this DeterministicRng rng) =>
        (rng.NextUInt64() >> 11) * (1.0 / (1UL << 53));
}
