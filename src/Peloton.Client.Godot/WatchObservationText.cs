using System;
using System.Globalization;

namespace Peloton.Client.Godot;

public static class WatchObservationText
{
    public static string Terrain(double gradient)
    {
        if (gradient >= 0.03)
        {
            return "podjazd";
        }

        if (gradient <= -0.03)
        {
            return "zjazd";
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

    public static string Speed(double speedMps)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{speedMps:0.0} m/s");
    }

    public static string Gap(double gapM)
    {
        return string.Create(CultureInfo.InvariantCulture, $"+{Math.Max(0.0, gapM):0} m");
    }
}
