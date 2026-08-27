using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Simulation.Race;

public sealed class RaceRouteSegment
{
    public RaceRouteSegment(
        string id,
        double lengthM,
        double gradient,
        double roadWidthM,
        double windSpeedMps,
        double windYawDegrees)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        RequirePositive(lengthM, nameof(lengthM));
        RequireFinite(gradient, nameof(gradient));
        RequirePositive(roadWidthM, nameof(roadWidthM));
        RequireNonNegative(windSpeedMps, nameof(windSpeedMps));
        RequireFinite(windYawDegrees, nameof(windYawDegrees));
        Id = id;
        LengthM = lengthM;
        Gradient = gradient;
        RoadWidthM = roadWidthM;
        WindSpeedMps = windSpeedMps;
        WindYawDegrees = windYawDegrees;
    }

    public string Id { get; }

    public double LengthM { get; }

    public double Gradient { get; }

    public double RoadWidthM { get; }

    public double WindSpeedMps { get; }

    public double WindYawDegrees { get; }

    private static void RequirePositive(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class RaceDefinition
{
    private readonly RaceRouteSegment[] segments;

    public RaceDefinition(
        string id,
        double airDensityKgPerM3,
        IEnumerable<RaceRouteSegment> segments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!double.IsFinite(airDensityKgPerM3) || airDensityKgPerM3 <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(airDensityKgPerM3));
        }

        ArgumentNullException.ThrowIfNull(segments);
        this.segments = segments.ToArray();
        if (this.segments.Length == 0)
        {
            throw new ArgumentException("A race route requires at least one segment.", nameof(segments));
        }

        if (this.segments.Select(segment => segment.Id).Distinct(StringComparer.Ordinal).Count() !=
            this.segments.Length)
        {
            throw new ArgumentException("Race segment IDs must be unique.", nameof(segments));
        }

        Id = id;
        AirDensityKgPerM3 = airDensityKgPerM3;
        TotalLengthM = this.segments.Sum(segment => segment.LengthM);
    }

    public string Id { get; }

    public double AirDensityKgPerM3 { get; }

    public IReadOnlyList<RaceRouteSegment> Segments => segments;

    public double TotalLengthM { get; }

    public RaceRouteSegment SegmentAt(double distanceM)
    {
        double boundedDistanceM = Math.Max(0.0, distanceM);
        double cumulativeDistanceM = 0.0;
        foreach (RaceRouteSegment segment in segments)
        {
            cumulativeDistanceM += segment.LengthM;
            if (boundedDistanceM < cumulativeDistanceM)
            {
                return segment;
            }
        }

        return segments[^1];
    }
}
