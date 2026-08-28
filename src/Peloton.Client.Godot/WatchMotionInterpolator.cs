using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public sealed record InterpolatedRiderView(
    long RiderId,
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
    IReadOnlyList<InterpolatedRiderView> Riders);

public static class WatchMotionInterpolator
{
    public static InterpolatedWatchView Project(
        RaceWatchFrame? previous,
        RaceWatchFrame current,
        double interpolationT)
    {
        ArgumentNullException.ThrowIfNull(current);
        double t = current.Paused ? 1.0 : Math.Clamp(interpolationT, 0.0, 1.0);
        RaceWatchFrame from = previous ?? current;
        InterpolatedRiderView[] riders = current.FocalRiders
            .Select(rider => InterpolateRider(from, rider, current.RouteLengthM, t))
            .ToArray();
        return new InterpolatedWatchView(
            current.WatchSecond,
            current.RaceSecond,
            current.Rate,
            current.Paused,
            current.RouteLengthM,
            t,
            riders);
    }

    private static InterpolatedRiderView InterpolateRider(
        RaceWatchFrame previous,
        RaceWatchRiderFrame current,
        double routeLengthM,
        double t)
    {
        RaceWatchRiderFrame? prior = previous.FocalRiders.FirstOrDefault(
            rider => rider.RiderId == current.RiderId);
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
            distanceM,
            Math.Max(0.0, gapM),
            Math.Max(0.0, speedMps),
            Math.Clamp(shelter, 0.0, 1.0),
            gradient,
            progress);
    }

    private static double Lerp(double from, double to, double t) => from + ((to - from) * t);
}
