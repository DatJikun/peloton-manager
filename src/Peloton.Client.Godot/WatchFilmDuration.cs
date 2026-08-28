using System;
using System.Globalization;
using System.Linq;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public static class WatchFilmDuration
{
    public static readonly int[] ChoicesSeconds = { 30, 60, 120, 180, 300 };

    public const int DefaultSeconds = 300;

    /// <summary>
    /// Effective film-pacing speed, including climb and prototype fatigue.
    /// Live board speed stays ~40 km/h on the flat (11 m/s); using that snapshot
    /// under-estimates duration and the 5 min film overruns the 5 min cap.
    /// 6 m/s on 5400 m ≈ 15 min of physics → ×3 → about 5 min of watching.
    /// </summary>
    public const double EffectivePaceMps = 6.0;

    public static bool IsChoice(int seconds) => ChoicesSeconds.Contains(seconds);

    public static int EstimatePhysicsSeconds(double routeLengthM)
    {
        if (!double.IsFinite(routeLengthM) || routeLengthM <= 0.0)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Round(routeLengthM / EffectivePaceMps, MidpointRounding.AwayFromZero));
    }

    public static int EstimatePhysicsSeconds(RaceWatchCourse? course)
    {
        return EstimatePhysicsSeconds(course?.TotalLengthM ?? 0.0);
    }

    public static int RateFor(double routeLengthM, int targetFilmSeconds)
    {
        return RateFor(EstimatePhysicsSeconds(routeLengthM), targetFilmSeconds);
    }

    public static int RateFor(RaceWatchCourse? course, int targetFilmSeconds)
    {
        return RateFor(EstimatePhysicsSeconds(course), targetFilmSeconds);
    }

    public static int RateFor(int estimatedPhysicsSeconds, int targetFilmSeconds)
    {
        if (!IsChoice(targetFilmSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(targetFilmSeconds));
        }

        int physics = Math.Max(1, estimatedPhysicsSeconds);
        int rate = (int)Math.Round(
            physics / (double)targetFilmSeconds,
            MidpointRounding.AwayFromZero);
        return Math.Clamp(rate, RaceWatchClock.MinimumRate, RaceWatchClock.MaximumRate);
    }

    public static int EstimateFilmSeconds(RaceWatchCourse? course, int targetFilmSeconds)
    {
        int physics = EstimatePhysicsSeconds(course);
        int rate = RateFor(physics, targetFilmSeconds);
        return Math.Max(1, (int)Math.Round(physics / (double)rate, MidpointRounding.AwayFromZero));
    }

    public static string Label(int seconds)
    {
        return seconds switch
        {
            30 => "30 s",
            60 => "1 min",
            120 => "2 min",
            180 => "3 min",
            300 => "5 min",
            _ => string.Create(CultureInfo.InvariantCulture, $"{seconds} s"),
        };
    }

    public static string Clock(int elapsedSeconds, int targetSeconds)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Format(elapsedSeconds)} / {Format(targetSeconds)}");
    }

    private static string Format(int seconds)
    {
        int bounded = Math.Max(0, seconds);
        int minutes = bounded / 60;
        int rest = bounded % 60;
        return string.Create(CultureInfo.InvariantCulture, $"{minutes}:{rest:00}");
    }
}
