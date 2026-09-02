using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Peloton.Application;

public static class RiderMetadataCatalog
{
    private static readonly object Gate = new();
    private static Dictionary<string, RiderMetadata>? cache;
    private static string? loadedRoot;

    public static string? ContentRoot { get; set; }

    public static string ResolveWageBand(string originDefinitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originDefinitionId);
        if (originDefinitionId.StartsWith("rider.generated.", StringComparison.Ordinal))
        {
            return "neo";
        }

        return TryGet(originDefinitionId)?.WageBand ?? WageBands.DefaultForArchetype(
            TryGet(originDefinitionId)?.Archetype ?? "domestique");
    }

    public static string ResolveArchetype(string originDefinitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originDefinitionId);
        if (originDefinitionId.StartsWith("rider.generated.", StringComparison.Ordinal))
        {
            return "neo";
        }

        return TryGet(originDefinitionId)?.Archetype ?? "domestique";
    }

    private static RiderMetadata? TryGet(string originDefinitionId)
    {
        EnsureLoaded();
        return cache!.GetValueOrDefault(originDefinitionId);
    }

    private static void EnsureLoaded()
    {
        string? root = ContentRoot;
        lock (Gate)
        {
            if (cache is not null && string.Equals(loadedRoot, root, StringComparison.Ordinal))
            {
                return;
            }

            cache = Load(root);
            loadedRoot = root;
        }
    }

    private static Dictionary<string, RiderMetadata> Load(string? contentRoot)
    {
        string? path = ResolvePath(contentRoot);
        if (path is null || !File.Exists(path))
        {
            return new Dictionary<string, RiderMetadata>(StringComparer.Ordinal);
        }

        string json = File.ReadAllText(path);
        using JsonDocument document = JsonDocument.Parse(json);
        Dictionary<string, RiderMetadata> riders = new(StringComparer.Ordinal);
        foreach (JsonElement rider in document.RootElement.GetProperty("riders").EnumerateArray())
        {
            string id = rider.GetProperty("id").GetString() ?? string.Empty;
            if (id.Length == 0)
            {
                continue;
            }

            string archetype = rider.TryGetProperty("archetype", out JsonElement archetypeElement)
                ? archetypeElement.GetString() ?? "domestique"
                : "domestique";
            string wageBand = rider.TryGetProperty("wageBand", out JsonElement wageBandElement)
                ? wageBandElement.GetString() ?? WageBands.DefaultForArchetype(archetype)
                : WageBands.DefaultForArchetype(archetype);
            riders[id] = new RiderMetadata(archetype, wageBand);
        }

        return riders;
    }

    private static string? ResolvePath(string? contentRoot)
    {
        if (!string.IsNullOrWhiteSpace(contentRoot))
        {
            string direct = Path.Combine(contentRoot, "peloton.wt-2026", "roster.json");
            if (File.Exists(direct))
            {
                return direct;
            }

            string nested = Path.Combine(contentRoot, "content", "peloton.wt-2026", "roster.json");
            if (File.Exists(nested))
            {
                return nested;
            }
        }

        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "content", "peloton.wt-2026", "roster.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private sealed record RiderMetadata(string Archetype, string WageBand);
}
