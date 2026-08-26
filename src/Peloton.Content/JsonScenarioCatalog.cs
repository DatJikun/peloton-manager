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
                    .Select(id => new OrganizationDefinition(id, DisplayName(id)))
                    .ToArray();
                return new WorldRecipe(
                    contentIdentity,
                    modules,
                    ResolvedRuleset.ComputeIdentity(modules),
                    organizations);
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
        if (scenario.Organizations.Count == 0)
        {
            throw new InvalidDataException("Skeleton scenario requires organizations.");
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
}
