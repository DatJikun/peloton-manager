using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Rules;

namespace Peloton.Content;

public sealed class JsonScenarioCatalog : IScenarioCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly string contentRoot;

    public JsonScenarioCatalog(string contentRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRoot);
        this.contentRoot = Path.GetFullPath(contentRoot);
    }

    public WorldRecipe Resolve(string scenarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        if (!Directory.Exists(contentRoot))
        {
            throw new InvalidDataException("Content root does not exist.");
        }

        foreach (string packPath in Directory
                     .EnumerateFiles(contentRoot, "pack.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            PackDocument pack = ReadJson<PackDocument>(packPath);
            string packRoot = Path.GetDirectoryName(packPath)!;
            RosterDocument? roster = null;
            foreach (ResourceDocument resource in pack.Resources
                         .Where(resource => string.Equals(resource.Kind, "roster", StringComparison.Ordinal))
                         .OrderBy(resource => resource.Path, StringComparer.Ordinal))
            {
                string resourcePath = ResolveInsidePack(packRoot, resource.Path);
                roster = ReadJson<RosterDocument>(resourcePath);
                break;
            }

            foreach (ResourceDocument resource in pack.Resources
                         .Where(resource => string.Equals(resource.Kind, "scenarios", StringComparison.Ordinal))
                         .OrderBy(resource => resource.Path, StringComparer.Ordinal))
            {
                string resourcePath = ResolveInsidePack(packRoot, resource.Path);
                ScenarioDocument scenario = ReadJson<ScenarioDocument>(resourcePath);
                if (!string.Equals(scenario.Id, scenarioId, StringComparison.Ordinal))
                {
                    continue;
                }

                Validate(pack, scenario, roster);
                RulesModuleIdentity[] modules = scenario.Modules
                    .Select(module => new RulesModuleIdentity(
                        module.Slot,
                        module.Id,
                        module.Contract,
                        module.ContractVersion,
                        module.ParameterIdentity))
                    .OrderBy(module => module.Slot, StringComparer.Ordinal)
                    .ToArray();
                string? rosterPath = ResolveRosterPath(packRoot, pack);
                string aggregateHash = rosterPath is null
                    ? ComputeArtifactHash(packPath, resourcePath)
                    : ComputeArtifactHash(packPath, resourcePath, rosterPath);
                ContentIdentity contentIdentity = new(
                    pack.PackId,
                    pack.PackVersion,
                    pack.ContentSchemaVersion,
                    scenario.Id,
                    scenario.HistoryMode,
                    scenario.Difficulty,
                    scenario.AttributeVisibility,
                    aggregateHash);
                OrganizationDefinition[] organizations = scenario.Organizations
                    .Select(id => new OrganizationDefinition(id, DisplayName(id)))
                    .ToArray();
                TeamRaceMappingDefinition[] teamMappings = roster!.TeamMappings
                    .Select(mapping => new TeamRaceMappingDefinition(
                        mapping.OrganizationId,
                        mapping.RaceTeamId))
                    .OrderBy(mapping => mapping.OrganizationId, StringComparer.Ordinal)
                    .ToArray();
                RiderDefinition[] riders = roster.Riders
                    .Select(rider => new RiderDefinition(
                        rider.Id,
                        rider.Name,
                        rider.OrganizationId,
                        rider.CriticalPowerW,
                        rider.WPrimeCapacityJ,
                        rider.PeakPowerW,
                        rider.WPrimeRecoveryJPerSecond,
                        rider.LowIntensityDurability,
                        rider.HighIntensityDurability,
                        rider.BodyMassKg,
                        rider.SystemMassKg,
                        rider.CdAM2,
                        rider.BaseCrr,
                        rider.Positioning,
                        rider.Handling,
                        rider.TacticalAwareness,
                        rider.AnnualWage,
                        rider.ContractEndDay,
                        rider.Loyalty01 ?? 0.5))
                    .OrderBy(rider => rider.Id, StringComparer.Ordinal)
                    .ToArray();
                ManagerDefinition manager = new(roster.Manager.Name, roster.Manager.OrganizationId);
                return new WorldRecipe(
                    contentIdentity,
                    modules,
                    ResolvedRuleset.ComputeIdentity(modules),
                    organizations,
                    teamMappings,
                    riders,
                    manager);
            }
        }

        throw new InvalidDataException($"Scenario '{scenarioId}' was not found.");
    }

    private static T ReadJson<T>(string path)
    {
        try
        {
            T? document = JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
            return document ?? throw new InvalidDataException($"JSON document '{path}' was empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"JSON document '{path}' is invalid.", exception);
        }
    }

    internal static string ResolveInsidePack(string packRoot, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException("Content resource path must be relative.");
        }

        string normalizedRoot = Path.GetFullPath(packRoot) + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(Path.Combine(packRoot, relativePath));
        if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Content resource path escapes its pack.");
        }

        return candidate;
    }

    private static string? ResolveRosterPath(string packRoot, PackDocument pack)
    {
        ResourceDocument? rosterResource = pack.Resources
            .FirstOrDefault(resource => string.Equals(resource.Kind, "roster", StringComparison.Ordinal));
        return rosterResource is null ? null : ResolveInsidePack(packRoot, rosterResource.Path);
    }

    private static void Validate(PackDocument pack, ScenarioDocument scenario, RosterDocument? roster)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pack.PackId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pack.PackVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pack.ContentSchemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario.Id);
        if (scenario.Organizations.Count == 0)
        {
            throw new InvalidDataException("Skeleton scenario requires organizations.");
        }

        if (scenario.Modules.Count == 0 ||
            scenario.Modules.Select(module => module.Slot).Distinct(StringComparer.Ordinal).Count() != scenario.Modules.Count)
        {
            throw new InvalidDataException("Scenario rule module slots must be present and unique.");
        }

        if (roster is null || roster.Riders.Count == 0 || roster.TeamMappings.Count == 0)
        {
            throw new InvalidDataException("Skeleton scenario requires a roster with riders and team mappings.");
        }

        foreach (RiderDocument rider in roster.Riders)
        {
            if (rider.AnnualWage <= 0)
            {
                throw new InvalidDataException($"Rider '{rider.Id}' requires annualWage > 0.");
            }

            if (rider.ContractEndDay < 0)
            {
                throw new InvalidDataException($"Rider '{rider.Id}' requires contractEndDay >= 0.");
            }
        }
    }

    private static string ComputeArtifactHash(params string[] paths)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string path in paths.OrderBy(path => path, StringComparer.Ordinal))
        {
            byte[] name = Encoding.UTF8.GetBytes(Path.GetFileName(path));
            hash.AppendData(name);
            hash.AppendData(File.ReadAllBytes(path));
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string DisplayName(string definitionId)
    {
        int separator = definitionId.LastIndexOf('.');
        string localName = separator < 0 ? definitionId : definitionId[(separator + 1)..];
        return localName.Replace('_', ' ');
    }

    private sealed record PackDocument(
        string PackId,
        string PackVersion,
        int ContentSchemaVersion,
        IReadOnlyList<ResourceDocument> Resources,
        IReadOnlyList<object> Dependencies);

    private sealed record ResourceDocument(string Kind, string Path);

    private sealed record ScenarioDocument(
        string Id,
        string StartDate,
        string HistoryMode,
        string Difficulty,
        string AttributeVisibility,
        IReadOnlyList<string> Organizations,
        IReadOnlyList<ModuleDocument> Modules);

    private sealed record ModuleDocument(
        string Slot,
        string Id,
        string Contract,
        int ContractVersion,
        string ParameterIdentity);

    private sealed record RosterDocument(
        ManagerDocument Manager,
        IReadOnlyList<TeamMappingDocument> TeamMappings,
        IReadOnlyList<RiderDocument> Riders);

    private sealed record ManagerDocument(string Name, string OrganizationId);

    private sealed record TeamMappingDocument(string OrganizationId, string RaceTeamId);

    private sealed record RiderDocument(
        string Id,
        string Name,
        string OrganizationId,
        double CriticalPowerW,
        double WPrimeCapacityJ,
        double PeakPowerW,
        double WPrimeRecoveryJPerSecond,
        double LowIntensityDurability,
        double HighIntensityDurability,
        double BodyMassKg,
        double SystemMassKg,
        double CdAM2,
        double BaseCrr,
        double Positioning,
        double Handling,
        double TacticalAwareness,
        int AnnualWage,
        int ContractEndDay,
        double? Loyalty01 = null);
}
