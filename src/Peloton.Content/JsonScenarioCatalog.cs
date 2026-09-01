using System;
using System.Collections.Generic;
using System.Globalization;
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
    private const string DefaultRaceTemplateId = "race-scenario.peloton.prototype-v0";

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

                OrganizationsFileDocument? organizationsFile = TryLoadOrganizations(packRoot, pack);
                CalendarFileDocument? calendarFile = TryLoadCalendar(packRoot, pack);
                Validate(pack, scenario, roster, organizationsFile);
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
                string? organizationsPath = ResolveResourcePath(packRoot, pack, "organizations");
                string? calendarPath = ResolveResourcePath(packRoot, pack, "calendar");
                string? raceIdentitiesPath = ResolveResourcePath(packRoot, pack, "race-identities");
                string aggregateHash = ComputeArtifactHash(
                    new[] { packPath, resourcePath, rosterPath, organizationsPath, calendarPath, raceIdentitiesPath }
                        .Where(path => path is not null)
                        .Cast<string>()
                        .ToArray());
                ContentIdentity contentIdentity = new(
                    pack.PackId,
                    pack.PackVersion,
                    pack.ContentSchemaVersion,
                    scenario.Id,
                    scenario.HistoryMode,
                    scenario.Difficulty,
                    scenario.AttributeVisibility,
                    aggregateHash);
                OrganizationDefinition[] organizations = BuildOrganizations(
                    scenario,
                    organizationsFile);
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
                        rider.Loyalty01 ?? 0.5,
                        rider.Nationality,
                        rider.BirthYear,
                        rider.PotentialOvr))
                    .OrderBy(rider => rider.Id, StringComparer.Ordinal)
                    .ToArray();
                ManagerDefinition manager = new(roster.Manager.Name, roster.Manager.OrganizationId);
                CalendarRaceDefinition[] calendarRaces = BuildCalendarRaces(scenario, calendarFile);
                RaceIdentityConstraints[] raceIdentities = LoadRaceIdentities(packRoot, pack);
                bool generatePeriodicRaces = !UsesCalendarFromContent(modules);
                return new WorldRecipe(
                    contentIdentity,
                    modules,
                    ResolvedRuleset.ComputeIdentity(modules),
                    organizations,
                    teamMappings,
                    riders,
                    manager,
                    calendarRaces,
                    raceIdentities,
                    generatePeriodicRaces,
                    DefaultRaceTemplateId);
            }
        }

        throw new InvalidDataException($"Scenario '{scenarioId}' was not found.");
    }

    private static OrganizationDefinition[] BuildOrganizations(
        ScenarioDocument scenario,
        OrganizationsFileDocument? organizationsFile)
    {
        if (organizationsFile is null)
        {
            return scenario.Organizations
                .Select(id => new OrganizationDefinition(id, DisplayName(id)))
                .ToArray();
        }

        Dictionary<string, OrganizationRecordDocument> byId = organizationsFile.Organizations
            .ToDictionary(organization => organization.Id, StringComparer.Ordinal);
        return scenario.Organizations
            .Select(id =>
            {
                if (!byId.TryGetValue(id, out OrganizationRecordDocument? record))
                {
                    throw new InvalidDataException($"Organization '{id}' is missing from organizations.json.");
                }

                return new OrganizationDefinition(
                    record.Id,
                    record.Name,
                    record.Country ?? string.Empty,
                    record.Division ?? "WorldTour",
                    record.LicenceYearsRemaining,
                    record.TitleSponsor ?? string.Empty,
                    record.Bike ?? string.Empty,
                    record.Groupset ?? string.Empty,
                    record.EstimatedBudgetEur);
            })
            .ToArray();
    }

    private static CalendarRaceDefinition[] BuildCalendarRaces(
        ScenarioDocument scenario,
        CalendarFileDocument? calendarFile)
    {
        if (calendarFile is null)
        {
            return Array.Empty<CalendarRaceDefinition>();
        }

        DateOnly scenarioStart = DateOnly.Parse(
            scenario.StartDate,
            CultureInfo.InvariantCulture);
        return calendarFile.Races
            .Select(race =>
            {
                DateOnly raceStart = DateOnly.Parse(race.Start, CultureInfo.InvariantCulture);
                DateOnly raceEnd = string.IsNullOrWhiteSpace(race.End)
                    ? raceStart
                    : DateOnly.Parse(race.End, CultureInfo.InvariantCulture);
                int dayNumber = raceStart.DayNumber - scenarioStart.DayNumber;
                int endDayNumber = raceEnd.DayNumber - scenarioStart.DayNumber;
                return new CalendarRaceDefinition(
                    race.Id,
                    race.Name,
                    dayNumber,
                    race.Country ?? string.Empty,
                    race.Kind ?? "oneDay",
                    endDayNumber);
            })
            .OrderBy(race => race.DayNumber)
            .ThenBy(race => race.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool UsesCalendarFromContent(IReadOnlyList<RulesModuleIdentity> modules)
    {
        RulesModuleIdentity? calendar = modules.FirstOrDefault(
            module => string.Equals(module.Slot, "calendarStructure", StringComparison.Ordinal));
        return calendar is not null &&
               string.Equals(calendar.ParameterIdentity, "calendar-from-content", StringComparison.Ordinal);
    }

    private static OrganizationsFileDocument? TryLoadOrganizations(string packRoot, PackDocument pack)
    {
        string? path = ResolveResourcePath(packRoot, pack, "organizations");
        return path is null ? null : ReadJson<OrganizationsFileDocument>(path);
    }

    private static RaceIdentityConstraints[] LoadRaceIdentities(string packRoot, PackDocument pack)
    {
        string? path = ResolveResourcePath(packRoot, pack, "race-identities");
        if (path is null)
        {
            return Array.Empty<RaceIdentityConstraints>();
        }

        RaceIdentitiesFileDocument document = ReadJson<RaceIdentitiesFileDocument>(path);
        return document.Races
            .Select(race => new RaceIdentityConstraints(
                race.RaceContentId,
                race.Kind,
                race.RacingStageCount,
                race.IttMin,
                race.IttMax,
                race.TttMin,
                race.TttMax,
                race.MountainMin,
                race.MountainMax,
                race.HillyMin,
                race.HillyMax,
                race.FlatMin,
                race.FlatMax,
                race.SummitFinishMin,
                race.SummitFinishMax,
                race.TotalKmMin,
                race.TotalKmMax,
                race.CobbleKmMin,
                race.CobbleKmMax,
                race.TerrainPalette))
            .OrderBy(identity => identity.RaceContentId, StringComparer.Ordinal)
            .ToArray();
    }

    private static CalendarFileDocument? TryLoadCalendar(string packRoot, PackDocument pack)
    {
        string? path = ResolveResourcePath(packRoot, pack, "calendar");
        return path is null ? null : ReadJson<CalendarFileDocument>(path);
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

    private static string? ResolveRosterPath(string packRoot, PackDocument pack) =>
        ResolveResourcePath(packRoot, pack, "roster");

    private static string? ResolveResourcePath(string packRoot, PackDocument pack, string kind)
    {
        ResourceDocument? resource = pack.Resources
            .FirstOrDefault(item => string.Equals(item.Kind, kind, StringComparison.Ordinal));
        return resource is null ? null : ResolveInsidePack(packRoot, resource.Path);
    }

    private static void Validate(
        PackDocument pack,
        ScenarioDocument scenario,
        RosterDocument? roster,
        OrganizationsFileDocument? organizationsFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pack.PackId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pack.PackVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pack.ContentSchemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario.Id);
        if (scenario.Organizations.Count == 0)
        {
            throw new InvalidDataException("Scenario requires organizations.");
        }

        if (scenario.Modules.Count == 0 ||
            scenario.Modules.Select(module => module.Slot).Distinct(StringComparer.Ordinal).Count() != scenario.Modules.Count)
        {
            throw new InvalidDataException("Scenario rule module slots must be present and unique.");
        }

        if (roster is null || roster.Riders.Count == 0)
        {
            throw new InvalidDataException("Scenario requires a roster with riders.");
        }

        bool usesCalendarFromContent = scenario.Modules.Any(
            module => string.Equals(module.Slot, "calendarStructure", StringComparison.Ordinal) &&
                      string.Equals(module.ParameterIdentity, "calendar-from-content", StringComparison.Ordinal));
        if (usesCalendarFromContent)
        {
            if (organizationsFile is null || organizationsFile.Organizations.Count == 0)
            {
                throw new InvalidDataException("WorldTour scenario requires organizations.json.");
            }
        }
        else if (roster.TeamMappings.Count == 0)
        {
            throw new InvalidDataException("Skeleton scenario requires team mappings.");
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
        double? Loyalty01 = null,
        string? Nationality = null,
        int? BirthYear = null,
        int? PotentialOvr = null);

    private sealed record RaceIdentitiesFileDocument(IReadOnlyList<RaceIdentityRecordDocument> Races);

    private sealed record RaceIdentityRecordDocument(
        string RaceContentId,
        string Kind,
        int RacingStageCount,
        int IttMin,
        int IttMax,
        int TttMin,
        int TttMax,
        int MountainMin,
        int MountainMax,
        int HillyMin,
        int HillyMax,
        int FlatMin,
        int FlatMax,
        int SummitFinishMin,
        int SummitFinishMax,
        int TotalKmMin,
        int TotalKmMax,
        int CobbleKmMin,
        int CobbleKmMax,
        IReadOnlyList<string> TerrainPalette);

    private sealed record OrganizationsFileDocument(IReadOnlyList<OrganizationRecordDocument> Organizations);

    private sealed record OrganizationRecordDocument(
        string Id,
        string Name,
        string? Country,
        string? Division,
        int LicenceYearsRemaining,
        string? TitleSponsor,
        string? Bike,
        string? Groupset,
        long EstimatedBudgetEur);

    private sealed record CalendarFileDocument(IReadOnlyList<CalendarRaceRecordDocument> Races);

    private sealed record CalendarRaceRecordDocument(
        string Id,
        string Name,
        string Start,
        string? End = null,
        string? Country = null,
        string? Kind = null);
}
