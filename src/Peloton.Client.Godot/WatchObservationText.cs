using System;
using System.Collections.Generic;
using System.Globalization;

namespace Peloton.Client.Godot;

public static class WatchObservationText
{
    public static string Terrain(double gradient)
    {
        if (gradient >= 0.03)
        {
            return string.Create(CultureInfo.InvariantCulture, $"podjazd {gradient * 100:0}%");
        }

        if (gradient <= -0.03)
        {
            return string.Create(CultureInfo.InvariantCulture, $"zjazd {Math.Abs(gradient) * 100:0}%");
        }

        if (Math.Abs(gradient) >= 0.008)
        {
            return "faliste";
        }

        return "płasko";
    }

    public static string Shelter(double shelterMultiplier)
    {
        return shelterMultiplier < 0.99 ? "w kole" : "na wietrze";
    }

    public static string DecisionOption(string option)
    {
        return option switch
        {
            "CommitSupport" => "Pościg",
            "WaitForRivals" => "Czekać na rywali",
            "ProtectSecondLeader" => "Chronić drugiego lidera",
            "TrustDs" => "Zaufać DS",
            _ => option,
        };
    }

    public static string DsAction(string option)
    {
        return string.Create(CultureInfo.InvariantCulture, $"DS (chce: {DecisionOption(option)})");
    }

    public static double SpeedKmh(double speedMps) => Math.Max(0.0, speedMps * 3.6);

    public static string Speed(double speedMps)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{SpeedKmh(speedMps):0} km/h");
    }

    public static string Gap(double gapM)
    {
        if (gapM < 0.5)
        {
            return "—";
        }

        return string.Create(CultureInfo.InvariantCulture, $"+{Math.Max(0.0, gapM):0} m");
    }

    public static string GapClock(double gapM, double speedMps)
    {
        if (gapM < 0.5)
        {
            return "0:00";
        }

        double seconds = gapM / Math.Max(1.0, speedMps);
        int total = Math.Max(1, (int)Math.Round(seconds, MidpointRounding.AwayFromZero));
        return string.Create(CultureInfo.InvariantCulture, $"+{total / 60}:{total % 60:00}");
    }

    public static string DisplayName(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "—";
        }

        string[] parts = label.Replace('-', ' ').Replace('.', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "—";
        }
        for (int index = 0; index < parts.Length; index++)
        {
            string part = parts[index];
            parts[index] = part.Length == 1
                ? char.ToUpperInvariant(part[0]).ToString()
                : char.ToUpperInvariant(part[0]) + part[1..];
        }

        return string.Join(' ', parts);
    }

    public static string Radio(
        double speedMps,
        double shelterMultiplier,
        double gradient,
        double gapM)
    {
        double kmh = SpeedKmh(speedMps);
        if (gradient >= 0.06)
        {
            return shelterMultiplier < 0.99 ? "ścianka, trzymam koło" : "ścianka, na wietrze";
        }

        if (gradient >= 0.03)
        {
            return shelterMultiplier < 0.99 ? "ciężko na górze, w kole" : "ciężko na górze, ciągnę";
        }

        if (gradient <= -0.03)
        {
            return "zjazd, nabieram";
        }

        if (gapM >= 40.0)
        {
            return "tracę kontakt";
        }

        if (shelterMultiplier >= 0.99)
        {
            return kmh >= 38.0 ? "ciągnę na wietrze" : "na wietrze, wolno";
        }

        return kmh >= 38.0 ? "luźno, siedzę w kole" : "w kole, tempo spada";
    }
}
