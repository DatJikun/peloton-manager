using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Simulation.Race;

public enum RouteSurface
{
    Asphalt,
    Cobble,
    Gravel,
    WhiteRoad,
}

public sealed class RaceRouteSegment
{
    public RaceRouteSegment(
        string id,
        double lengthM,
        double gradient,
        double roadWidthM,
        double windSpeedMps,
        double windYawDegrees,
        RouteSurface surface = RouteSurface.Asphalt)
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
        Surface = surface;
    }

    public string Id { get; }

    public double LengthM { get; }

    public double Gradient { get; }

    public double RoadWidthM { get; }

    public double WindSpeedMps { get; }

    public double WindYawDegrees { get; }

    public RouteSurface Surface { get; }

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
    private readonly double[] prefixLengthsM;

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

        prefixLengthsM = new double[this.segments.Length];
        double cumulative = 0.0;
        for (int i = 0; i < this.segments.Length; i++)
        {
            cumulative += this.segments[i].LengthM;
            prefixLengthsM[i] = cumulative;
        }

        Id = id;
        AirDensityKgPerM3 = airDensityKgPerM3;
        TotalLengthM = cumulative;
    }

    public string Id { get; }

    public double AirDensityKgPerM3 { get; }

    public IReadOnlyList<RaceRouteSegment> Segments => segments;

    public double TotalLengthM { get; }

    public RaceRouteSegment SegmentAt(double distanceM)
    {
        double boundedDistanceM = Math.Max(0.0, distanceM);
        int index = Array.BinarySearch(prefixLengthsM, boundedDistanceM);
        if (index < 0)
        {
            index = ~index;
        }

        if (index >= segments.Length)
        {
            return segments[^1];
        }

        return segments[index];
    }
}
