using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Content;

public sealed class JsonRacePrototypeCatalog : IRaceScenarioCatalog
{
    private const long MaximumJsonBytes = 1_048_576;
    private const int MaximumDefinitionsPerResource = 64;
    private const int MaximumTeamsPerScenario = 64;
    private const int MaximumRidersPerScenario = 512;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 64,
    };

    private readonly string contentRoot;

    public JsonRacePrototypeCatalog(string contentRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRoot);
        this.contentRoot = Path.GetFullPath(contentRoot);
    }

    public RaceScenario Resolve(string scenarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        if (!Directory.Exists(contentRoot))
        {
            throw Issue("CONTENT_ROOT_MISSING", contentRoot, "$", "Content root does not exist.");
        }

        List<LocatedScenario> definitions = new();
        foreach (string packPath in Directory
                     .EnumerateFiles(contentRoot, "pack.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            PackDocument pack = ReadJson<PackDocument>(packPath);
            ValidateManifest(pack, packPath);
            string packRoot = Path.GetDirectoryName(packPath)!;
            HashSet<string> normalizedPaths = new(StringComparer.OrdinalIgnoreCase);
            foreach (ResourceDocument resource in pack.Resources!
                         .OrderBy(resource => resource.Path, StringComparer.Ordinal))
            {
                string resourcePath = ResolveResourcePath(packRoot, resource, packPath);
                string normalizedPath = Path.GetFullPath(resourcePath);
                if (!normalizedPaths.Add(normalizedPath))
                {
                    throw Issue(
                        "RESOURCE_PATH_DUPLICATE",
                        packPath,
                        "$.resources",
                        "Pack declares the same normalized resource path more than once.");
                }

                if (!string.Equals(
                        resource.Kind,
                        "racePrototypeScenarios",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                RaceResourceDocument document = ReadJson<RaceResourceDocument>(resourcePath);
                if (document.Definitions is null ||
                    document.Definitions.Count == 0 ||
                    document.Definitions.Count > MaximumDefinitionsPerResource)
                {
                    throw Issue(
                        "RESOURCE_SCHEMA_INVALID",
                        resourcePath,
                        "$.definitions",
                        "Race prototype resource must contain a bounded, non-empty definitions array.");
                }

                for (int index = 0; index < document.Definitions.Count; index++)
                {
                    RaceScenarioDocument definition = document.Definitions[index];
                    ValidateDefinitionHeader(definition, resourcePath, index);
                    definitions.Add(new LocatedScenario(
                        pack.PackId!,
                        resourcePath,
                        index,
                        definition));
                }
            }
        }

        LocatedScenario[] duplicates = definitions
            .GroupBy(item => item.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .OrderBy(item => item.Definition.Id, StringComparer.Ordinal)
            .ThenBy(item => item.ResourcePath, StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw Issue(
                "DEFINITION_ID_DUPLICATE",
                duplicates[0].ResourcePath,
                $"$.definitions[{duplicates[0].DefinitionIndex}].id",
                "Race scenario definition IDs must be unique under ordinal case-insensitive comparison.");
        }

        ValidatedScenario[] validatedDefinitions = definitions
            .OrderBy(item => item.Definition.Id, StringComparer.Ordinal)
            .ThenBy(item => item.ResourcePath, StringComparer.Ordinal)
            .Select(Validate)
            .ToArray();
        ValidatedScenario? selected = validatedDefinitions.FirstOrDefault(
            item => string.Equals(item.Source.Id, scenarioId, StringComparison.Ordinal));
        if (selected is null)
        {
            throw Issue(
                "SCENARIO_NOT_FOUND",
                contentRoot,
                "$.definitions",
                $"Race prototype scenario '{scenarioId}' was not found.");
        }

        return Build(selected);
    }

    private static string ResolveResourcePath(
        string packRoot,
        ResourceDocument resource,
        string packPath)
    {
        if (string.IsNullOrWhiteSpace(resource.Path))
        {
            throw Issue("RESOURCE_SCHEMA_INVALID", packPath, "$.resources[].path", "Resource path is required.");
        }

        try
        {
            return JsonScenarioCatalog.ResolveInsidePack(packRoot, resource.Path);
        }
        catch (InvalidDataException exception)
        {
            throw Issue(
                "PATH_OUTSIDE_PACK",
                packPath,
                "$.resources[].path",
                "Content resource path must remain inside its pack.",
                exception);
        }
    }

    private static void ValidateManifest(PackDocument pack, string packPath)
    {
        if (string.IsNullOrWhiteSpace(pack.PackId) || string.IsNullOrWhiteSpace(pack.PackVersion))
        {
            throw Issue("MANIFEST_INVALID", packPath, "$", "Pack identity and version are required.");
        }

        if (pack.ContentSchemaVersion != 1)
        {
            throw Issue(
                "MANIFEST_SCHEMA_UNSUPPORTED",
                packPath,
                "$.contentSchemaVersion",
                "Race prototype content supports schema version 1 only.");
        }

        if (pack.Resources is null || pack.Dependencies is null)
        {
            throw Issue("MANIFEST_INVALID", packPath, "$", "Resources and dependencies are required.");
        }
    }

    private static void ValidateDefinitionHeader(
        RaceScenarioDocument definition,
        string resourcePath,
        int definitionIndex)
    {
        string root = $"$.definitions[{definitionIndex}]";
        ValidateDefinitionId(definition.Id, resourcePath, $"{root}.id");
        if (!string.Equals(definition.Kind, "racePrototypeScenario", StringComparison.Ordinal))
        {
            throw Issue(
                "DEFINITION_KIND_INVALID",
                resourcePath,
                $"{root}.kind",
                "Race prototype definition has an unsupported kind.");
        }
    }

    private static ValidatedScenario Validate(LocatedScenario located)
    {
        RaceScenarioDocument definition = located.Definition;
        string path = located.ResourcePath;
        string root = $"$.definitions[{located.DefinitionIndex}]";
        ValidateDefinitionId(definition.TuningIdentity, path, $"{root}.tuningIdentity");
        ValidateRange(definition.AirDensityKgPerM3, 0.8, 1.5, path, $"{root}.airDensityKgPerM3");
        ValidateRange(definition.InitialSpeedMps, 1.0, 30.0, path, $"{root}.initialSpeedMps");
        ValidateRange(definition.MaximumDurationSeconds, 60, 100_000, path, $"{root}.maximumDurationSeconds");
        if (definition.Route is null || definition.Teams is null || definition.Riders is null ||
            definition.StartingOrder is null || definition.Commands is null || definition.TacticalPlans is null)
        {
            throw Issue("RESOURCE_SCHEMA_INVALID", path, root, "Race scenario requires every canonical fixture field.");
        }

        ValidateDefinitionId(definition.Route.Id, path, $"{root}.route.id");
        if (definition.Route.Segments is null || definition.Route.Segments.Count != 3)
        {
            throw Issue(
                "RESOURCE_SCHEMA_INVALID",
                path,
                $"{root}.route.segments",
                "Prototype route must contain flat, climb, and crosswind sectors.");
        }

        for (int index = 0; index < definition.Route.Segments.Count; index++)
        {
            RouteSegmentDocument segment = definition.Route.Segments[index];
            string segmentPath = $"{root}.route.segments[{index}]";
            ValidateDefinitionId(segment.Id, path, $"{segmentPath}.id");
            ValidateRange(segment.LengthM, 100, 100_000, path, $"{segmentPath}.lengthM");
            ValidateRange(segment.Gradient, -0.25, 0.30, path, $"{segmentPath}.gradient");
            ValidateRange(segment.RoadWidthM, 1.0, 20.0, path, $"{segmentPath}.roadWidthM");
            ValidateRange(segment.WindSpeedMps, 0.0, 40.0, path, $"{segmentPath}.windSpeedMps");
            ValidateRange(segment.WindYawDegrees, -180.0, 180.0, path, $"{segmentPath}.windYawDegrees");
        }

        if (definition.Teams.Count < 2 || definition.Teams.Count > MaximumTeamsPerScenario)
        {
            throw Issue("RESOURCE_SCHEMA_INVALID", path, $"{root}.teams", "Prototype requires a bounded multi-team field.");
        }

        if (definition.Riders.Count < 4 || definition.Riders.Count > MaximumRidersPerScenario)
        {
            throw Issue("RESOURCE_SCHEMA_INVALID", path, $"{root}.riders", "Prototype requires a bounded peloton.");
        }

        EnsureUniqueIds(definition.Teams.Select(team => team.Id), path, $"{root}.teams");
        EnsureUniqueIds(definition.Riders.Select(rider => rider.Id), path, $"{root}.riders");
        EnsureUniqueIds(
            definition.Teams.Select(team => team.Id).Concat(definition.Riders.Select(rider => rider.Id)),
            path,
            root);

        Dictionary<string, ValidatedTeam> teams = new(StringComparer.Ordinal);
        for (int index = 0; index < definition.Teams.Count; index++)
        {
            TeamDocument team = definition.Teams[index];
            string teamPath = $"{root}.teams[{index}]";
            ValidateDefinitionId(team.Id, path, $"{teamPath}.id");
            RaceObjective objective = ParseEnum<RaceObjective>(team.Objective, path, $"{teamPath}.objective");
            if (team.Briefing is null)
            {
                throw Issue("RESOURCE_SCHEMA_INVALID", path, $"{teamPath}.briefing", "Team briefing is required.");
            }

            RaceBriefingKind briefingKind = ParseEnum<RaceBriefingKind>(
                team.Briefing.Kind,
                path,
                $"{teamPath}.briefing.kind");
            teams.Add(team.Id!, new ValidatedTeam(team.Id!, objective, new RaceBriefing(
                briefingKind,
                team.Briefing.ConsultManager)));
        }

        Dictionary<string, RiderDocument> riders = new(StringComparer.Ordinal);
        for (int index = 0; index < definition.Riders.Count; index++)
        {
            RiderDocument rider = definition.Riders[index];
            string riderPath = $"{root}.riders[{index}]";
            ValidateDefinitionId(rider.Id, path, $"{riderPath}.id");
            RequireReference(teams, rider.TeamId, path, $"{riderPath}.teamId");
            ValidateRange(rider.CriticalPowerW, 100, 650, path, $"{riderPath}.criticalPowerW");
            ValidateRange(rider.WPrimeCapacityJ, 1_000, 100_000, path, $"{riderPath}.wPrimeCapacityJ");
            ValidateRange(rider.PeakPowerW, 300, 2_000, path, $"{riderPath}.peakPowerW");
            ValidateRange(rider.WPrimeRecoveryJPerSecond, 0.1, 500, path, $"{riderPath}.wPrimeRecoveryJPerSecond");
            ValidateRange(rider.LowIntensityDurability, 0, 1, path, $"{riderPath}.lowIntensityDurability");
            ValidateRange(rider.HighIntensityDurability, 0, 1, path, $"{riderPath}.highIntensityDurability");
            ValidateRange(rider.BodyMassKg, 35, 120, path, $"{riderPath}.bodyMassKg");
            ValidateRange(rider.SystemMassKg, 5, 20, path, $"{riderPath}.systemMassKg");
            ValidateRange(rider.CdAM2, 0.15, 0.60, path, $"{riderPath}.cdAM2");
            ValidateRange(rider.BaseCrr, 0.001, 0.020, path, $"{riderPath}.baseCrr");
            ValidateRange(rider.Positioning, 0, 1, path, $"{riderPath}.positioning");
            ValidateRange(rider.Handling, 0, 1, path, $"{riderPath}.handling");
            ValidateRange(rider.TacticalAwareness, 0, 1, path, $"{riderPath}.tacticalAwareness");
            riders.Add(rider.Id!, rider);
        }

        ValidateStartingOrder(definition.StartingOrder, riders, path, $"{root}.startingOrder");
        ValidateCommands(definition.Commands, riders, teams, definition.MaximumDurationSeconds, path, $"{root}.commands");
        ValidateTacticalPlans(
            definition.TacticalPlans,
            riders,
            teams,
            definition.MaximumDurationSeconds,
            path,
            $"{root}.tacticalPlans");
        return new ValidatedScenario(definition, teams, riders);
    }

    private static void ValidateStartingOrder(
        IReadOnlyList<string> startingOrder,
        IReadOnlyDictionary<string, RiderDocument> riders,
        string resourcePath,
        string jsonPath)
    {
        if (startingOrder.Count != riders.Count ||
            startingOrder.Distinct(StringComparer.Ordinal).Count() != startingOrder.Count)
        {
            throw Issue(
                "REFERENCE_MISSING",
                resourcePath,
                jsonPath,
                "Starting order must reference every rider exactly once.");
        }

        for (int index = 0; index < startingOrder.Count; index++)
        {
            RequireReference(riders, startingOrder[index], resourcePath, $"{jsonPath}[{index}]");
        }
    }

    private static void ValidateCommands(
        IReadOnlyList<CommandDocument> commands,
        IReadOnlyDictionary<string, RiderDocument> riders,
        IReadOnlyDictionary<string, ValidatedTeam> teams,
        int maximumDurationSeconds,
        string resourcePath,
        string jsonPath)
    {
        for (int index = 0; index < commands.Count; index++)
        {
            CommandDocument command = commands[index];
            string commandPath = $"{jsonPath}[{index}]";
            ValidateRange(command.SimulationSecond, 0, maximumDurationSeconds - 1, resourcePath, $"{commandPath}.simulationSecond");
            RequireReference(teams, command.TeamId, resourcePath, $"{commandPath}.teamId");
            RiderDocument rider = RequireReference(riders, command.RiderId, resourcePath, $"{commandPath}.riderId");
            if (!string.Equals(rider.TeamId, command.TeamId, StringComparison.Ordinal))
            {
                throw Issue(
                    "REFERENCE_KIND_MISMATCH",
                    resourcePath,
                    commandPath,
                    "Strategic command team must own its rider.");
            }

            _ = ParseEnum<RaceCommandKind>(command.Intent, resourcePath, $"{commandPath}.intent");
        }
    }

    private static void ValidateTacticalPlans(
        IReadOnlyList<TacticalPlanDocument> plans,
        IReadOnlyDictionary<string, RiderDocument> riders,
        IReadOnlyDictionary<string, ValidatedTeam> teams,
        int maximumDurationSeconds,
        string resourcePath,
        string jsonPath)
    {
        for (int index = 0; index < plans.Count; index++)
        {
            TacticalPlanDocument plan = plans[index];
            string planPath = $"{jsonPath}[{index}]";
            ValidateRange(plan.TriggerSecond, 0, maximumDurationSeconds - 1, resourcePath, $"{planPath}.triggerSecond");
            ValidatedTeam team = RequireReference(teams, plan.TeamId, resourcePath, $"{planPath}.teamId");
            RiderDocument support = RequireReference(riders, plan.SupportRiderId, resourcePath, $"{planPath}.supportRiderId");
            if (!string.Equals(support.TeamId, team.Id, StringComparison.Ordinal))
            {
                throw Issue(
                    "REFERENCE_KIND_MISMATCH",
                    resourcePath,
                    planPath,
                    "Tactical plan team must own its support rider.");
            }

            ValidateRange(plan.OfficialGapSeconds, 0, 3_600, resourcePath, $"{planPath}.officialGapSeconds");
            _ = ParseEnum<RaceResourceEstimate>(plan.ResourceEstimate, resourcePath, $"{planPath}.resourceEstimate");
            _ = ParseEnum<RaceThreatEstimate>(plan.ThreatEstimate, resourcePath, $"{planPath}.threatEstimate");
            _ = ParseEnum<RacePositionBand>(plan.LeaderPositionBand, resourcePath, $"{planPath}.leaderPositionBand");
            _ = ParseEnum<RaceInformationConfidence>(plan.Confidence, resourcePath, $"{planPath}.confidence");
        }
    }

    private static RaceScenario Build(ValidatedScenario validated)
    {
        RaceScenarioDocument source = validated.Source;
        Dictionary<string, WorldEntityId> teamIds = validated.Teams.Keys
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select((id, index) => (id, worldId: new WorldEntityId(index + 1L)))
            .ToDictionary(item => item.id, item => item.worldId, StringComparer.Ordinal);
        Dictionary<string, WorldEntityId> authorityIds = validated.Teams.Keys
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select((id, index) => (id, worldId: new WorldEntityId(101L + index)))
            .ToDictionary(item => item.id, item => item.worldId, StringComparer.Ordinal);
        Dictionary<string, WorldEntityId> riderIds = validated.Riders.Keys
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select((id, index) => (id, worldId: new WorldEntityId(1_001L + index)))
            .ToDictionary(item => item.id, item => item.worldId, StringComparer.Ordinal);

        RaceDefinition route = new(
            source.Route!.Id!,
            source.AirDensityKgPerM3,
            source.Route.Segments!.Select(segment => new RaceRouteSegment(
                segment.Id!,
                segment.LengthM,
                segment.Gradient,
                segment.RoadWidthM,
                segment.WindSpeedMps,
                segment.WindYawDegrees)).ToArray());
        RaceRiderProfile[] riders = validated.Riders.Values
            .OrderBy(rider => rider.Id, StringComparer.Ordinal)
            .Select(rider => new RaceRiderProfile(
                riderIds[rider.Id!],
                teamIds[rider.TeamId!],
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
                rider.TacticalAwareness))
            .ToArray();
        IReadOnlyList<string> startingOrder = source.StartingOrder!;
        RaceStartingPosition[] startingPositions = startingOrder
            .Select((id, index) => new RaceStartingPosition(
                riderIds[id],
                (startingOrder.Count - 1 - index) * 0.7))
            .ToArray();
        RaceCommand[] commands = source.Commands!
            .OrderBy(command => command.SimulationSecond)
            .ThenBy(command => command.TeamId, StringComparer.Ordinal)
            .ThenBy(command => command.RiderId, StringComparer.Ordinal)
            .Select(command => new RaceCommand(
                command.SimulationSecond,
                teamIds[command.TeamId!],
                riderIds[command.RiderId!],
                Enum.Parse<RaceCommandKind>(command.Intent!, ignoreCase: false)))
            .ToArray();
        RaceTacticalPlan[] tacticalPlans = source.TacticalPlans!
            .OrderBy(plan => plan.TriggerSecond)
            .ThenBy(plan => plan.TeamId, StringComparer.Ordinal)
            .Select(plan =>
            {
                ValidatedTeam team = validated.Teams[plan.TeamId!];
                return new RaceTacticalPlan(
                    plan.TriggerSecond,
                    riderIds[plan.SupportRiderId!],
                    new TeamRaceObservation(
                        teamIds[team.Id],
                        authorityIds[team.Id],
                        plan.OfficialGapSeconds,
                        plan.VisibleSplit,
                        Enum.Parse<RacePositionBand>(plan.LeaderPositionBand!, ignoreCase: false),
                        Enum.Parse<RaceResourceEstimate>(plan.ResourceEstimate!, ignoreCase: false),
                        Enum.Parse<RaceThreatEstimate>(plan.ThreatEstimate!, ignoreCase: false),
                        team.Objective,
                        Enum.Parse<RaceInformationConfidence>(plan.Confidence!, ignoreCase: false)),
                    team.Briefing);
            })
            .ToArray();
        return new RaceScenario(
            source.Id!,
            route,
            riders,
            startingPositions,
            commands,
            source.InitialSpeedMps,
            source.MaximumDurationSeconds,
            tacticalPlans,
            source.TuningIdentity!);
    }

    private static T ReadJson<T>(string path)
    {
        try
        {
            FileInfo file = new(path);
            if (!file.Exists)
            {
                throw Issue("RESOURCE_MISSING", path, "$", "Declared content resource does not exist.");
            }

            if (file.Length > MaximumJsonBytes)
            {
                throw Issue("RESOURCE_TOO_LARGE", path, "$", "JSON resource exceeds the prototype size limit.");
            }

            T? result = JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
            return result ?? throw Issue("JSON_INVALID", path, "$", "JSON document is empty.");
        }
        catch (JsonException exception)
        {
            throw Issue("JSON_INVALID", path, exception.Path ?? "$", "JSON document is structurally invalid.", exception);
        }
    }

    private static void EnsureUniqueIds(IEnumerable<string?> ids, string resourcePath, string jsonPath)
    {
        string? duplicate = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (duplicate is not null)
        {
            throw Issue(
                "DEFINITION_ID_DUPLICATE",
                resourcePath,
                jsonPath,
                $"Definition ID '{duplicate}' is duplicated.");
        }
    }

    private static void ValidateDefinitionId(string? id, string resourcePath, string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(id) || !id.Contains('.', StringComparison.Ordinal) ||
            id.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-' or '_')))
        {
            throw Issue(
                "DEFINITION_ID_INVALID",
                resourcePath,
                jsonPath,
                "Content definition ID must be a lowercase namespaced identifier.");
        }
    }

    private static void ValidateRange(
        double value,
        double minimum,
        double maximum,
        string resourcePath,
        string jsonPath)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
        {
            throw Issue(
                "VALUE_OUT_OF_RANGE",
                resourcePath,
                jsonPath,
                $"Numeric value must be within [{minimum}, {maximum}].");
        }
    }

    private static void ValidateRange(
        int value,
        int minimum,
        int maximum,
        string resourcePath,
        string jsonPath)
    {
        if (value < minimum || value > maximum)
        {
            throw Issue(
                "VALUE_OUT_OF_RANGE",
                resourcePath,
                jsonPath,
                $"Integer value must be within [{minimum}, {maximum}].");
        }
    }

    private static T ParseEnum<T>(string? value, string resourcePath, string jsonPath)
        where T : struct, Enum
    {
        if (!Enum.TryParse(value, ignoreCase: false, out T parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw Issue("VALUE_INVALID", resourcePath, jsonPath, $"Unsupported {typeof(T).Name} value.");
        }

        return parsed;
    }

    private static TValue RequireReference<TValue>(
        IReadOnlyDictionary<string, TValue> definitions,
        string? id,
        string resourcePath,
        string jsonPath)
    {
        if (id is null || !definitions.TryGetValue(id, out TValue? value))
        {
            throw Issue("REFERENCE_MISSING", resourcePath, jsonPath, "Required content reference is missing.");
        }

        return value;
    }

    private static ContentValidationException Issue(
        string code,
        string resourcePath,
        string jsonPath,
        string message,
        Exception? innerException = null)
    {
        return new ContentValidationException(code, resourcePath, jsonPath, message, innerException);
    }

    private sealed record LocatedScenario(
        string PackId,
        string ResourcePath,
        int DefinitionIndex,
        RaceScenarioDocument Definition);

    private sealed record ValidatedScenario(
        RaceScenarioDocument Source,
        IReadOnlyDictionary<string, ValidatedTeam> Teams,
        IReadOnlyDictionary<string, RiderDocument> Riders);

    private sealed record ValidatedTeam(
        string Id,
        RaceObjective Objective,
        RaceBriefing Briefing);

    private sealed record PackDocument(
        string? PackId,
        string? PackVersion,
        int ContentSchemaVersion,
        IReadOnlyList<ResourceDocument>? Resources,
        IReadOnlyList<object>? Dependencies);

    private sealed record ResourceDocument(string? Kind, string? Path);

    private sealed record RaceResourceDocument(IReadOnlyList<RaceScenarioDocument>? Definitions);

    private sealed record RaceScenarioDocument(
        string? Id,
        string? Kind,
        string? TuningIdentity,
        double AirDensityKgPerM3,
        double InitialSpeedMps,
        int MaximumDurationSeconds,
        RouteDocument? Route,
        IReadOnlyList<TeamDocument>? Teams,
        IReadOnlyList<RiderDocument>? Riders,
        IReadOnlyList<string>? StartingOrder,
        IReadOnlyList<CommandDocument>? Commands,
        IReadOnlyList<TacticalPlanDocument>? TacticalPlans);

    private sealed record RouteDocument(string? Id, IReadOnlyList<RouteSegmentDocument>? Segments);

    private sealed record RouteSegmentDocument(
        string? Id,
        double LengthM,
        double Gradient,
        double RoadWidthM,
        double WindSpeedMps,
        double WindYawDegrees);

    private sealed record TeamDocument(
        string? Id,
        string? Objective,
        BriefingDocument? Briefing);

    private sealed record BriefingDocument(string? Kind, bool ConsultManager);

    private sealed record RiderDocument(
        string? Id,
        string? TeamId,
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
        double TacticalAwareness);

    private sealed record CommandDocument(
        int SimulationSecond,
        string? TeamId,
        string? RiderId,
        string? Intent);

    private sealed record TacticalPlanDocument(
        int TriggerSecond,
        string? TeamId,
        string? SupportRiderId,
        int OfficialGapSeconds,
        bool VisibleSplit,
        string? LeaderPositionBand,
        string? ResourceEstimate,
        string? ThreatEstimate,
        string? Confidence);
}
