using System;
using System.Collections.Generic;
using System.Globalization;

namespace Peloton.Simulation.Race;

public sealed record RouteProfileSample(
    double DistanceM,
    double ElevationM,
    double Gradient,
    double RoadWidthM,
    double WindSpeedMps,
    double WindYawDegrees,
    RouteTerrainKind Kind);

public static class RouteProfileGenerator
{
    public const double SampleSpacingM = 40.0;
    public const double JoinBlendM = 100.0;
    public const double ReferenceLengthM = 1800.0;

    public static RouteTerrainKind Classify(RaceWatchCourseSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        if (IsNamed(segment.Id, "rolling", "falist"))
        {
            return RouteTerrainKind.Rolling;
        }

        if (IsCrosswind(segment) || IsNamed(segment.Id, "crosswind", "wiatr"))
        {
            return RouteTerrainKind.Crosswind;
        }

        if (segment.Gradient >= 0.03 || IsNamed(segment.Id, "climb", "podjazd"))
        {
            return RouteTerrainKind.Climb;
        }

        if (segment.Gradient <= -0.03 || IsNamed(segment.Id, "descent", "zjazd"))
        {
            return RouteTerrainKind.Descent;
        }

        return RouteTerrainKind.Flat;
    }

    public static bool IsCrosswind(RaceWatchCourseSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        double yaw = Math.Abs(segment.WindYawDegrees) % 180.0;
        double fromBroadside = Math.Min(Math.Abs(yaw - 90.0), Math.Abs(yaw + 90.0));
        return segment.RoadWidthM < 4.0 || (segment.WindSpeedMps >= 5.0 && fromBroadside <= 35.0);
    }

    public static int VariantFor(string segmentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segmentId);
        uint hash = 2166136261;
        foreach (char character in segmentId)
        {
            hash ^= character;
            hash *= 16777619;
        }

        return (int)(hash % (uint)RouteProfileLibrary.VariantsPerKind);
    }

    public static RouteProfileTemplate TemplateFor(RaceWatchCourseSegment segment)
    {
        return RouteProfileLibrary.Get(Classify(segment), VariantFor(segment.Id));
    }

    public static RaceWatchCourse GenerateCourse(
        long seed,
        double totalLengthM,
        IReadOnlyList<RouteTerrainKind> kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        if (kinds.Count == 0)
        {
            throw new ArgumentException("Route composition must contain at least one terrain kind.", nameof(kinds));
        }

        if (!double.IsFinite(totalLengthM) || totalLengthM <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLengthM));
        }

        double each = totalLengthM / kinds.Count;
        RaceWatchCourseSegment[] segments = new RaceWatchCourseSegment[kinds.Count];
        for (int index = 0; index < kinds.Count; index++)
        {
            RouteTerrainKind kind = kinds[index];
            (double gradient, double width, double wind, double yaw) = SpecFor(kind);
            string id = string.Create(
                CultureInfo.InvariantCulture,
                $"generated.{seed}.{index}.{kind.ToString().ToLowerInvariant()}");
            segments[index] = new RaceWatchCourseSegment(id, each, gradient, width, wind, yaw);
        }

        return new RaceWatchCourse(totalLengthM, segments);
    }

    public static IReadOnlyList<RouteProfileSample> Expand(RaceWatchCourse course)
    {
        ArgumentNullException.ThrowIfNull(course);
        if (course.TotalLengthM <= 0.0 || course.Segments.Count == 0)
        {
            return Array.Empty<RouteProfileSample>();
        }

        List<double> distances = new();
        for (double distance = 0.0; distance < course.TotalLengthM; distance += SampleSpacingM)
        {
            distances.Add(distance);
        }

        if (distances.Count == 0 || distances[^1] < course.TotalLengthM)
        {
            distances.Add(course.TotalLengthM);
        }

        double[] elevation = new double[distances.Count];
        double[] width = new double[distances.Count];
        double[] wind = new double[distances.Count];
        double[] yaw = new double[distances.Count];
        RouteTerrainKind[] kinds = new RouteTerrainKind[distances.Count];
        for (int index = 0; index < distances.Count; index++)
        {
            SampleAt(
                course,
                distances[index],
                out elevation[index],
                out width[index],
                out wind[index],
                out yaw[index],
                out kinds[index]);
        }

        BlendJoins(course, distances, elevation, width, wind);
        RouteProfileSample[] samples = new RouteProfileSample[distances.Count];
        for (int index = 0; index < distances.Count; index++)
        {
            double gradient = LocalGradient(distances, elevation, index);
            samples[index] = new RouteProfileSample(
                distances[index],
                elevation[index],
                gradient,
                width[index],
                wind[index],
                yaw[index],
                kinds[index]);
        }

        return samples;
    }

    private static void SampleAt(
        RaceWatchCourse course,
        double distanceM,
        out double elevation,
        out double roadWidthM,
        out double windSpeedMps,
        out double windYawDegrees,
        out RouteTerrainKind kind)
    {
        double remaining = Math.Clamp(distanceM, 0.0, course.TotalLengthM);
        double startElevation = 0.0;
        for (int index = 0; index < course.Segments.Count; index++)
        {
            RaceWatchCourseSegment segment = course.Segments[index];
            bool last = index == course.Segments.Count - 1;
            if (remaining > segment.LengthM && !last)
            {
                startElevation += segment.LengthM * segment.Gradient;
                remaining -= segment.LengthM;
                continue;
            }

            double local = Math.Clamp(remaining, 0.0, segment.LengthM);
            double t = segment.LengthM <= 1e-9 ? 1.0 : local / segment.LengthM;
            RouteProfileTemplate template = TemplateFor(segment);
            RouteProfileKnot knot = Interpolate(template.Knots, t);
            elevation = ShapedElevation(startElevation, segment, template, knot.Shape, t);
            roadWidthM = Math.Max(2.4, segment.RoadWidthM * knot.WidthScale);
            windSpeedMps = Math.Max(0.0, segment.WindSpeedMps * knot.WindScale);
            windYawDegrees = segment.WindYawDegrees;
            kind = template.Kind;
            return;
        }

        RaceWatchCourseSegment fallback = course.Segments[^1];
        elevation = startElevation + (fallback.LengthM * fallback.Gradient);
        roadWidthM = fallback.RoadWidthM;
        windSpeedMps = fallback.WindSpeedMps;
        windYawDegrees = fallback.WindYawDegrees;
        kind = Classify(fallback);
    }

    private static double ShapedElevation(
        double startElevation,
        RaceWatchCourseSegment segment,
        RouteProfileTemplate template,
        double shape,
        double t)
    {
        double net = segment.LengthM * segment.Gradient;
        double linear = startElevation + (t * net);
        double first = template.Knots[0].Shape;
        double last = template.Knots[^1].Shape;
        double span = last - first;
        if (Math.Abs(net) >= 0.5 && Math.Abs(span) >= 0.05)
        {
            double warped = (shape - first) / span;
            return startElevation + (net * warped);
        }

        double reliefScale = segment.LengthM / ReferenceLengthM;
        double relief = (shape - first) * reliefScale;
        double endRelief = (last - first) * reliefScale;
        return linear + relief - (t * endRelief);
    }

    private static RouteProfileKnot Interpolate(IReadOnlyList<RouteProfileKnot> knots, double t)
    {
        if (t <= knots[0].T)
        {
            return Extrapolate(knots[0], knots[1], t);
        }

        for (int index = 0; index < knots.Count - 1; index++)
        {
            RouteProfileKnot from = knots[index];
            RouteProfileKnot to = knots[index + 1];
            if (t > to.T)
            {
                continue;
            }

            return Extrapolate(from, to, t);
        }

        return Extrapolate(knots[^2], knots[^1], t);
    }

    private static RouteProfileKnot Extrapolate(RouteProfileKnot from, RouteProfileKnot to, double t)
    {
        double span = to.T - from.T;
        double u = span <= 1e-9 ? 0.0 : (t - from.T) / span;
        return new RouteProfileKnot(
            t,
            from.Shape + ((to.Shape - from.Shape) * u),
            from.WidthScale + ((to.WidthScale - from.WidthScale) * u),
            from.WindScale + ((to.WindScale - from.WindScale) * u));
    }

    private static void BlendJoins(
        RaceWatchCourse course,
        List<double> distances,
        double[] elevation,
        double[] width,
        double[] wind)
    {
        double cursor = 0.0;
        for (int join = 0; join < course.Segments.Count - 1; join++)
        {
            cursor += course.Segments[join].LengthM;
            for (int index = 0; index < distances.Count; index++)
            {
                double offset = distances[index] - cursor;
                if (Math.Abs(offset) > JoinBlendM)
                {
                    continue;
                }

                double t = (offset + JoinBlendM) / (2.0 * JoinBlendM);
                double ease = 0.5 - (0.5 * Math.Cos(Math.Clamp(t, 0.0, 1.0) * Math.PI));
                SampleAt(course, cursor + offset, out double _, out double rawWidth, out double rawWind, out _, out _);
                double leftElevation = ElevationWithoutJoin(course, join, cursor + offset);
                double rightElevation = ElevationWithoutJoin(course, join + 1, cursor + offset);
                elevation[index] = leftElevation + ((rightElevation - leftElevation) * ease);
                width[index] = rawWidth;
                wind[index] = rawWind;
            }
        }
    }

    private static double ElevationWithoutJoin(RaceWatchCourse course, int segmentIndex, double distanceM)
    {
        double start = 0.0;
        for (int index = 0; index < segmentIndex; index++)
        {
            start += course.Segments[index].LengthM * course.Segments[index].Gradient;
        }

        RaceWatchCourseSegment segment = course.Segments[segmentIndex];
        double local = distanceM;
        for (int index = 0; index < segmentIndex; index++)
        {
            local -= course.Segments[index].LengthM;
        }

        double t = segment.LengthM <= 1e-9 ? 1.0 : local / segment.LengthM;
        RouteProfileTemplate template = TemplateFor(segment);
        RouteProfileKnot knot = Interpolate(template.Knots, t);
        return ShapedElevation(start, segment, template, knot.Shape, t);
    }

    private static double LocalGradient(List<double> distances, double[] elevation, int index)
    {
        int from = Math.Max(0, index - 1);
        int to = Math.Min(distances.Count - 1, index + 1);
        double span = distances[to] - distances[from];
        if (span <= 1e-9)
        {
            return 0.0;
        }

        return (elevation[to] - elevation[from]) / span;
    }

    private static (double Gradient, double Width, double Wind, double Yaw) SpecFor(RouteTerrainKind kind)
    {
        return kind switch
        {
            RouteTerrainKind.Climb => (0.05, 5.0, 1.0, 20.0),
            RouteTerrainKind.Descent => (-0.06, 5.0, 2.0, 10.0),
            RouteTerrainKind.Rolling => (0.0, 5.5, 3.0, 30.0),
            RouteTerrainKind.Crosswind => (0.0, 3.2, 10.0, 90.0),
            _ => (0.0, 6.0, 2.0, 0.0),
        };
    }

    private static bool IsNamed(string id, params string[] tokens)
    {
        foreach (string token in tokens)
        {
            if (id.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
