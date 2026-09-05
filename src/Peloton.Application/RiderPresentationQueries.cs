using System;
using System.Collections.Generic;
using System.Globalization;
using Peloton.Domain;

namespace Peloton.Application;

public static class RiderPresentationQueries
{
    private static readonly Dictionary<string, string> RoleLabels = new(StringComparer.Ordinal)
    {
        ["sprinter"] = "SPRINTER",
        ["classics"] = "KLASYKI",
        ["diesel"] = "ALL-ROUND",
        ["neo"] = "NEO",
        ["gc"] = "GÓRY",
        ["super-gc"] = "GÓRY",
        ["tt"] = "CZASOWIEC",
        ["domestique"] = "POMOCNIK",
        ["puncheur"] = "PUNCHEUR",
    };

    public static string FormatRoleLabel(string originDefinitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originDefinitionId);
        string archetype = RiderMetadataCatalog.ResolveArchetype(originDefinitionId);
        if (RoleLabels.TryGetValue(archetype, out string? label))
        {
            return label;
        }

        return archetype.ToUpperInvariant();
    }

    public static int ComputeFormPercent(double form01) =>
        Math.Clamp((int)Math.Round(form01 * 100.0, MidpointRounding.AwayFromZero), 0, 100);

    public static int? ComputeAge(WorldState world, Person person) =>
        person.BirthYear is int birthYear
            ? world.SeasonYear - birthYear
            : null;

    public static string BuildIdentityLine(string? nationality, int? age, string roleLabel)
    {
        List<string> parts = new();
        if (!string.IsNullOrWhiteSpace(nationality))
        {
            parts.Add(nationality);
        }

        if (age is int resolvedAge)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{resolvedAge} LAT"));
        }

        if (!string.IsNullOrWhiteSpace(roleLabel))
        {
            parts.Add(roleLabel);
        }

        return parts.Count == 0 ? string.Empty : string.Join(" · ", parts);
    }

    public static RiderPresentationFields BuildFields(WorldState world, Person person, RiderCareer career)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(career);

        string roleLabel = FormatRoleLabel(career.OriginDefinitionId);
        int? age = ComputeAge(world, person);
        return new RiderPresentationFields(
            person.Nationality,
            age,
            roleLabel,
            ComputeFormPercent(career.Form01),
            BuildIdentityLine(person.Nationality, age, roleLabel));
    }
}

public sealed record RiderPresentationFields(
    string? Nationality,
    int? Age,
    string RoleLabel,
    int FormPercent,
    string IdentityLine);
