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

                Validate(pack, scenario);
                RulesModuleIdentity[] modules = scenario.Modules
                    .Select(module => new RulesModuleIdentity(
                        module.Slot,
                        module.Id,
                        module.Contract,
                        module.ContractVersion,
                        module.ParameterIdentity))
                    .OrderBy(module => module.Slot, StringComparer.Ordinal)
                    .ToArray();
                string aggregateHash = ComputeArtifactHash(packPath, resourcePath);
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
                    .Select(organization => new OrganizationDefinition(
                        organization.Id,
                        organization.Name,
                        organization.RacePrototypeTeamId))
                    .ToArray();
                RiderDefinition[] riders = scenario.Riders
                    .Select(rider => new RiderDefinition(
                        rider.Id,
                        rider.Name,
                        rider.OrganizationId,
                        rider.RacePrototypeRiderId))
                    .ToArray();
                ManagerDefinition manager = new(scenario.Manager.Id, scenario.Manager.Name);
                return new WorldRecipe(
                    contentIdentity,
                    modules,
                    ResolvedRuleset.ComputeIdentity(modules),
                    organizations,
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

    private static void Validate(PackDocument pack, ScenarioDocument scenario)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pack.PackId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pack.PackVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pack.ContentSchemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario.Id);
        if (scenario.Organizations is null || scenario.Riders is null)
        {
            throw new InvalidDataException("Skeleton scenario requires organizations and riders.");
        }

        if (scenario.Organizations.Count != 3)
        {
            throw new InvalidDataException("Skeleton scenario requires exactly three organizations.");
        }

        if (scenario.Riders.Count != 12)
        {
            throw new InvalidDataException("Skeleton scenario requires exactly twelve named riders.");
        }

        if (scenario.Manager is null ||
            string.IsNullOrWhiteSpace(scenario.Manager.Id) ||
            string.IsNullOrWhiteSpace(scenario.Manager.Name))
        {
            throw new InvalidDataException("Skeleton scenario requires a named manager person.");
        }

        HashSet<string> organizationIds = new(StringComparer.Ordinal);
        foreach (OrganizationDocument organization in scenario.Organizations)
        {
            if (string.IsNullOrWhiteSpace(organization.Id) ||
                string.IsNullOrWhiteSpace(organization.Name) ||
                string.IsNullOrWhiteSpace(organization.RacePrototypeTeamId) ||
                !organizationIds.Add(organization.Id))
            {
                throw new InvalidDataException("Skeleton organizations must be unique named teams.");
            }
        }

        HashSet<string> personIds = new(StringComparer.Ordinal) { scenario.Manager.Id };
        HashSet<string> prototypeRiderIds = new(StringComparer.Ordinal);
        foreach (RiderDocument rider in scenario.Riders)
        {
            if (string.IsNullOrWhiteSpace(rider.Id) ||
                string.IsNullOrWhiteSpace(rider.Name) ||
                string.IsNullOrWhiteSpace(rider.OrganizationId) ||
                string.IsNullOrWhiteSpace(rider.RacePrototypeRiderId) ||
                !organizationIds.Contains(rider.OrganizationId) ||
                !personIds.Add(rider.Id) ||
                !prototypeRiderIds.Add(rider.RacePrototypeRiderId))
            {
                throw new InvalidDataException("Skeleton riders must be unique named people on the three teams.");
            }
        }

        if (scenario.Riders.GroupBy(rider => rider.OrganizationId, StringComparer.Ordinal)
            .Any(group => group.Count() != 4))
        {
            throw new InvalidDataException("Each skeleton team must have exactly four riders.");
        }

        if (scenario.Modules.Count == 0 ||
            scenario.Modules.Select(module => module.Slot).Distinct(StringComparer.Ordinal).Count() != scenario.Modules.Count)
        {
            throw new InvalidDataException("Scenario rule module slots must be present and unique.");
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
        IReadOnlyList<OrganizationDocument> Organizations,
        ManagerDocument Manager,
        IReadOnlyList<RiderDocument> Riders,
        IReadOnlyList<ModuleDocument> Modules);

    private sealed record OrganizationDocument(string Id, string Name, string RacePrototypeTeamId);

    private sealed record ManagerDocument(string Id, string Name);

    private sealed record RiderDocument(
        string Id,
        string Name,
        string OrganizationId,
        string RacePrototypeRiderId);

    private sealed record ModuleDocument(
        string Slot,
        string Id,
        string Contract,
        int ContractVersion,
        string ParameterIdentity);
}
