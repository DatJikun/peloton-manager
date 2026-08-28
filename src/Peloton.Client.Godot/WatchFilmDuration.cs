using System;
using System.Globalization;
using System.Linq;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public static class WatchFilmDuration
{
    public static readonly int[] ChoicesSeconds = { 30, 60, 120, 180, 300 };

    public const int DefaultSeconds = 120;
    public const double EstimateSpeedMps = 6.0;

    public static bool IsChoice(int seconds) => ChoicesSeconds.Contains(seconds);

    public static int EstimatePhysicsSeconds(double routeLengthM)
    {
        if (!double.IsFinite(routeLengthM) || routeLengthM <= 0.0)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Round(routeLengthM / EstimateSpeedMps, MidpointRounding.AwayFromZero));
    }

    public static int RateFor(double routeLengthM, int targetFilmSeconds)
    {
        if (!IsChoice(targetFilmSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(targetFilmSeconds));
        }

        double estimate = routeLengthM / EstimateSpeedMps;
        int rate = (int)Math.Round(estimate / targetFilmSeconds, MidpointRounding.AwayFromZero);
        return Math.Clamp(rate, RaceWatchClock.MinimumRate, RaceWatchClock.MaximumRate);
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
