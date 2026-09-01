using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Infrastructure;
using Peloton.Simulation.Race;

namespace Peloton.SimRunner;

public static class HistoricalCompareCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string PrototypeTemplateId = "race-scenario.peloton.prototype-v0";
    private static readonly string[] SprinterNameHints =
    {
        "Philipsen", "Groves", "Meeus", "Bauhaus", "Milan", "Kooij", "Girmay",
        "Welsford", "Merlier", "Coquard", "Groenewegen", "Kristoff", "Jakobsen",
        "Bennett",
    };

    public static int Execute(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int index = 1; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Options must use '--name value' pairs.", nameof(args));
            }

            if (!values.TryAdd(args[index], args[index + 1]))
            {
                throw new ArgumentException($"Duplicate option '{args[index]}'.", nameof(args));
            }
        }

        if (!values.TryGetValue("--scenario", out string? scenarioId) || string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException("Required option '--scenario' is missing.", nameof(args));
        }

        if (!values.TryGetValue("--seed", out string? seedText) ||
            !long.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long seed))
        {
            throw new ArgumentException("--seed must be a signed integer.", nameof(args));
        }

        string contentRoot = values.TryGetValue("--content-root", out string? configuredRoot)
            ? Path.GetFullPath(configuredRoot)
            : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "content"));

        string comparisonsPath = Path.Combine(contentRoot, "peloton.wt-2026", "historical-comparisons.json");
        if (!File.Exists(comparisonsPath))
        {
            error.WriteLine("reason=COMPARISONS_NOT_FOUND");
            return 1;
        }

        ComparisonsFile? file = JsonSerializer.Deserialize<ComparisonsFile>(
            File.ReadAllText(comparisonsPath),
            JsonOptions);
        if (file?.Cases is null || file.Cases.Count == 0)
        {
            error.WriteLine("reason=COMPARISONS_EMPTY");
            return 1;
        }

        GameApplication application = ApplicationFactory.Create(contentRoot);
        CommandResult create = application.Execute(new CreateWorldCommand(scenarioId, seed));
        if (!create.Succeeded || application.World is null)
        {
            error.WriteLine($"crashed=true reason={create.ReasonCode}");
            return 1;
        }

        WorldState world = application.World;
        WorldRecipe recipe = new JsonScenarioCatalog(contentRoot).Resolve(scenarioId);
        RaceScenarioTemplate template = new JsonRacePrototypeCatalog(contentRoot).ResolveTemplate(PrototypeTemplateId);
        PrototypeRaceEngine engine = new();
        Dictionary<(string Race, int Stage), RaceResult> cache = new();

        output.WriteLine("honesty=CreateWorld readiness; not race-week form; D-001 analogues not a script");
        foreach (ComparisonCaseDocument comparison in file.Cases)
        {
            if (comparison.CompareJerseys)
            {
                WriteJerseyCase(output, world, recipe, template, engine, cache, seed, comparison);
                continue;
            }

            CourseProfile? course = ResolveCourse(world, comparison);
            if (course is null)
            {
                output.WriteLine($"case={comparison.Id} skipped=course_not_found");
                continue;
            }

            RaceResult result = Simulate(world, recipe, template, engine, cache, seed, course);
            WriteRaceCase(output, world, comparison, course, result);
        }

        return 0;
    }

    private static void WriteJerseyCase(
        TextWriter output,
        WorldState world,
        WorldRecipe recipe,
        RaceScenarioTemplate template,
        PrototypeRaceEngine engine,
        Dictionary<(string Race, int Stage), RaceResult> cache,
        long seed,
        ComparisonCaseDocument comparison)
    {
        List<RiderStageTime> times = new();
        RaceResult? last = null;
        CourseProfile[] stages = world.CourseProfiles
            .Where(profile => string.Equals(profile.RaceContentId, comparison.SimRaceContentId, StringComparison.Ordinal))
            .OrderBy(profile => profile.StageIndex)
            .ToArray();
        foreach (CourseProfile stage in stages)
        {
            last = Simulate(world, recipe, template, engine, cache, seed, stage);
            times.AddRange(last.RiderMetrics.Select(metric =>
                new RiderStageTime(stage.RaceContentId, stage.StageIndex, metric.RiderId, metric.FinishTimeSeconds)));
        }

        ClassificationProjection jerseys = ClassificationQueries.Build(
            world,
            comparison.SimRaceContentId,
            stageTimes: times);
        output.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"case={comparison.Id} real={comparison.RealEvent} fieldSim={last?.FinishOrder.Count ?? 0} fieldReal={comparison.RealFieldRiders} {ClassificationQueries.FormatJerseyLine(jerseys)} realGc={comparison.RealGcWinner} realPoints={comparison.RealPointsWinner} realKom={comparison.RealKomWinner} realYouth={comparison.RealYouthWinner} realTeam={comparison.RealTeamWinner} verdict=jersey_table"));
    }

    private static void WriteRaceCase(
        TextWriter output,
        WorldState world,
        ComparisonCaseDocument comparison,
        CourseProfile course,
        RaceResult result)
    {
        string[] top5 = result.FinishOrder
            .Take(5)
            .Select(id => Describe(world, id))
            .ToArray();
        string winner = Describe(world, result.WinnerId);
        string verdict = Verdict(world, result, comparison);
        string realTop = comparison.RealTop3 is null ? string.Empty : string.Join(",", comparison.RealTop3);
        output.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"case={comparison.Id} course={course.OriginDefinitionId} classified={course.ClassifiedStageType} fieldSim={result.FinishOrder.Count} fieldExpected={comparison.ExpectedSimFieldRiders} fieldReal={comparison.RealFieldRiders} simWinner={winner} simTop5={string.Join("|", top5)} realWinner={comparison.RealWinner} realTop3={realTop} verdict={verdict}"));
    }

    private static string Verdict(WorldState world, RaceResult result, ComparisonCaseDocument comparison)
    {
        string? feel = comparison.Feel;
        int pogacar = PlaceOfOrigin(world, result, "rider.wt2026.uae.leader");
        int philipsen = PlaceOfOrigin(world, result, "rider.wt2026.alpecin.card");
        bool sprinterInTop5 = result.FinishOrder.Take(5).Any(id => IsSprinter(world, id));
        bool classicsTop3 = result.FinishOrder.Take(3).Any(id =>
        {
            string origin = OriginOf(world, id);
            return origin.Contains("alpecin.leader", StringComparison.Ordinal) ||
                   origin.Contains("uae.leader", StringComparison.Ordinal) ||
                   origin.Contains("lidl-trek.card", StringComparison.Ordinal);
        });

        if (string.Equals(feel, "sprint_feel", StringComparison.Ordinal) && sprinterInTop5)
        {
            return "sprint_feel";
        }

        if (string.Equals(feel, "climb_feel", StringComparison.Ordinal) &&
            pogacar > 0 && philipsen > 0 && pogacar < philipsen)
        {
            return "climb_feel";
        }

        if (string.Equals(feel, "classics_feel", StringComparison.Ordinal) && classicsTop3)
        {
            return "classics_feel";
        }

        if (sprinterInTop5)
        {
            return "sprint_feel";
        }

        if (pogacar > 0 && philipsen > 0 && pogacar < philipsen)
        {
            return "climb_feel";
        }

        if (classicsTop3)
        {
            return "classics_feel";
        }

        return "mismatch";
    }

    private static bool IsSprinter(WorldState world, WorldEntityId riderId)
    {
        RiderCareer? career = world.TryGetRiderCareer(riderId);
        if (career is null)
        {
            return false;
        }

        Person? person = world.Persons.FirstOrDefault(item => item.Id == career.PersonId);
        string name = person?.Name ?? career.OriginDefinitionId;
        return SprinterNameHints.Any(hint => name.Contains(hint, StringComparison.OrdinalIgnoreCase));
    }

    private static string Describe(WorldState world, WorldEntityId riderId)
    {
        RiderCareer? career = world.TryGetRiderCareer(riderId);
        Person? person = career is null
            ? null
            : world.Persons.FirstOrDefault(item => item.Id == career.PersonId);
        string name = person?.Name ?? riderId.Value.ToString(CultureInfo.InvariantCulture);
        string origin = career?.OriginDefinitionId ?? string.Empty;
        return $"{name}/{origin}";
    }

    private static string OriginOf(WorldState world, WorldEntityId riderId) =>
        world.TryGetRiderCareer(riderId)?.OriginDefinitionId ?? string.Empty;

    private static int PlaceOfOrigin(WorldState world, RaceResult result, string originId)
    {
        for (int index = 0; index < result.FinishOrder.Count; index++)
        {
            if (string.Equals(OriginOf(world, result.FinishOrder[index]), originId, StringComparison.Ordinal))
            {
                return index + 1;
            }
        }

        return -1;
    }

    private static RaceResult Simulate(
        WorldState world,
        WorldRecipe recipe,
        RaceScenarioTemplate template,
        PrototypeRaceEngine engine,
        Dictionary<(string Race, int Stage), RaceResult> cache,
        long seed,
        CourseProfile course)
    {
        (string Race, int Stage) key = (course.RaceContentId, course.StageIndex);
        if (cache.TryGetValue(key, out RaceResult? cached))
        {
            return cached;
        }

        RaceScenario scenario = WorldRaceScenarioAssembler.Assemble(
            world,
            recipe,
            template,
            course.RaceContentId,
            courseProfile: course,
            masterSeed: seed);
        RaceResult result = engine.RunBatch(scenario, seed);
        cache[key] = result;
        return result;
    }

    private static CourseProfile? ResolveCourse(WorldState world, ComparisonCaseDocument comparison)
    {
        CourseProfile[] profiles = world.CourseProfiles
            .Where(profile => string.Equals(profile.RaceContentId, comparison.SimRaceContentId, StringComparison.Ordinal))
            .ToArray();
        if (profiles.Length == 0)
        {
            return null;
        }

        if (comparison.PickHighestGainStage)
        {
            return profiles.OrderByDescending(profile => profile.ElevationGainM).First();
        }

        if (!string.IsNullOrWhiteSpace(comparison.PreferClassifiedStageType) &&
            Enum.TryParse(comparison.PreferClassifiedStageType, ignoreCase: true, out ClassifiedStageType wanted))
        {
            CourseProfile[] typed = profiles
                .Where(profile => profile.ClassifiedStageType == wanted)
                .ToArray();
            if (typed.Length > 0)
            {
                profiles = typed;
            }
        }

        if (comparison.SimStageIndex > 0)
        {
            CourseProfile? indexed = profiles.FirstOrDefault(profile => profile.StageIndex == comparison.SimStageIndex);
            if (indexed is not null)
            {
                return indexed;
            }
        }

        return profiles.OrderBy(profile => profile.StageIndex).First();
    }

    private sealed record ComparisonsFile(IReadOnlyList<ComparisonCaseDocument>? Cases);

    private sealed record ComparisonCaseDocument(
        string Id,
        string SimRaceContentId,
        int SimStageIndex = 0,
        string? PreferClassifiedStageType = null,
        bool PickHighestGainStage = false,
        bool CompareJerseys = false,
        string? RealEvent = null,
        string? RealWinner = null,
        IReadOnlyList<string>? RealTop3 = null,
        int RealFieldRiders = 0,
        int ExpectedSimFieldRiders = 0,
        string? Feel = null,
        string? RealGcWinner = null,
        string? RealPointsWinner = null,
        string? RealKomWinner = null,
        string? RealYouthWinner = null,
        string? RealTeamWinner = null);
}
