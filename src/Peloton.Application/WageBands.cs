using System;
using System.Collections.Generic;

namespace Peloton.Application;

/// <summary>Gameplay wage bands from content/peloton.wt-2026/README.md (D-056 §5).</summary>
public static class WageBands
{
    private static readonly Dictionary<string, (int Floor, int Cap)> Bands =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["star"] = (3_500_000, 8_000_000),
            ["leader"] = (600_000, 2_500_000),
            ["sprinter"] = (180_000, 1_200_000),
            ["super-domestique"] = (250_000, 1_200_000),
            ["neo"] = (80_000, 450_000),
            ["domestique"] = (100_000, 250_000),
        };

    public static (int Floor, int Cap) Resolve(string wageBand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wageBand);
        if (Bands.TryGetValue(wageBand, out (int Floor, int Cap) band))
        {
            return band;
        }

        return Bands["domestique"];
    }

    public static string DefaultForArchetype(string archetype)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archetype);
        return archetype.ToLowerInvariant() switch
        {
            "super-gc" or "gc" => "leader",
            "sprinter" => "sprinter",
            "tt" or "super-domestique" => "super-domestique",
            "neo" => "neo",
            _ => "domestique",
        };
    }

    public static int RenewalWage(int currentWage, string wageBand)
    {
        (int _, int cap) = Resolve(wageBand);
        int raised = (int)Math.Floor(currentWage * 1.6);
        return Math.Max(currentWage, Math.Min(raised, cap));
    }
}
