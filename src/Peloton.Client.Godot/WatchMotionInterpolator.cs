using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public sealed record InterpolatedRiderView(
    long RiderId,
    long OrganizationId,
    string Label,
    int Place,
    double DistanceM,
    double GapM,
    double SpeedMps,
    double ShelterMultiplier,
    double Gradient,
    double Progress);

public sealed record InterpolatedWatchView(
    int WatchSecond,
    int RaceSecond,
    int Rate,
    bool Paused,
    double RouteLengthM,
    double InterpolationT,
    IReadOnlyList<InterpolatedRiderView> Riders,
    IReadOnlyList<InterpolatedRiderView> Field);

public static class WatchMotionInterpolator
{
    public static InterpolatedWatchView Project(
        RaceWatchFrame? previous,
        RaceWatchFrame current,
        double interpolationT,
        IReadOnlyCollection<long>? squadIds = null)
    {
        ArgumentNullException.ThrowIfNull(current);
        double t = current.Paused ? 1.0 : Math.Clamp(interpolationT, 0.0, 1.0);
        RaceWatchFrame from = previous ?? current;
        IReadOnlyList<RaceWatchRiderFrame> source = current.Field.Count > 0 ? current.Field : current.FocalRiders;
        InterpolatedRiderView[] field = source
            .Select(rider => InterpolateRider(from, rider, current.RouteLengthM, t))
            .ToArray();
        InterpolatedRiderView[] riders = FilterSquad(field, squadIds);
        return new InterpolatedWatchView(
            current.WatchSecond,
            current.RaceSecond,
            current.Rate,
            current.Paused,
            current.RouteLengthM,
            t,
            riders,
            field);
    }

    private static InterpolatedRiderView[] FilterSquad(
        InterpolatedRiderView[] field,
        IReadOnlyCollection<long>? squadIds)
    {
        if (squadIds is null || squadIds.Count == 0)
        {
            return field.Take(3).ToArray();
        }

        InterpolatedRiderView[] squad = field
            .Where(rider => squadIds.Contains(rider.RiderId))
            .OrderBy(rider => rider.Place)
            .ThenBy(rider => rider.RiderId)
            .ToArray();
        return squad.Length == 0 ? field.Take(3).ToArray() : squad;
    }

    private static InterpolatedRiderView InterpolateRider(
        RaceWatchFrame previous,
        RaceWatchRiderFrame current,
        double routeLengthM,
        double t)
    {
        IReadOnlyList<RaceWatchRiderFrame> priorSource = previous.Field.Count > 0
            ? previous.Field
            : previous.FocalRiders;
        RaceWatchRiderFrame? prior = priorSource.FirstOrDefault(rider => rider.RiderId == current.RiderId);
        double distanceM = prior is null
            ? current.DistanceM
            : Lerp(prior.DistanceM, current.DistanceM, t);
        double gapM = prior is null ? current.GapM : Lerp(prior.GapM, current.GapM, t);
        double speedMps = prior is null ? current.SpeedMps : Lerp(prior.SpeedMps, current.SpeedMps, t);
        double shelter = prior is null
            ? current.ShelterMultiplier
            : Lerp(prior.ShelterMultiplier, current.ShelterMultiplier, t);
        double gradient = prior is null ? current.Gradient : Lerp(prior.Gradient, current.Gradient, t);
        double progress = routeLengthM <= 0.0 ? 0.0 : Math.Clamp(distanceM / routeLengthM, 0.0, 1.0);
        return new InterpolatedRiderView(
            current.RiderId.Value,
            current.OrganizationId.Value,
            current.Label,
            current.Place,
            distanceM,
            Math.Max(0.0, gapM),
            Math.Max(0.0, speedMps),
            Math.Clamp(shelter, 0.0, 1.0),
            gradient,
            progress);
    }

    private static double Lerp(double from, double to, double t) => from + ((to - from) * t);
}
