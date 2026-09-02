using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Peloton.Domain;
using Peloton.Simulation;

namespace Peloton.Application;

/// <summary>D-056 / CAREER_SEASON_ROLLOVER_AND_AGING_v0.1.md §4.4.</summary>
public static class SeasonNeoPros
{
    public const int LivingRiderCap = 512;

    public const double NeoTtCdAFactor = 0.82;

    /// <summary>Set by ApplicationFactory to the catalog content root.</summary>
    public static string? ContentRoot { get; set; }

    public static IReadOnlyList<RiderCareer> Apply(
        WorldState world,
        int newSeasonYear,
        IReadOnlyList<RiderCareer> retired)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(retired);
        if (retired.Count == 0)
        {
            return Array.Empty<RiderCareer>();
        }

        NameBank names = NameBank.Load(ContentRoot);
        List<RiderCareer> created = new();
        for (int index = 0; index < retired.Count; index++)
        {
            if (world.LivingRiderCount >= LivingRiderCap)
            {
                break;
            }

            created.Add(CreateOne(world, names, newSeasonYear, index, retired[index]));
        }

        return created;
    }

    public static NameBank LoadNames(string? contentRoot) => NameBank.Load(contentRoot);

    internal static RiderCareer CreateOne(
        WorldState world,
        NameBank names,
        int seasonYear,
        int index,
        RiderCareer? retired = null)
    {
        ulong seed = StableSeedDerivation.Derive(world.MasterSeed, $"neo:{seasonYear}:{index}");
        DeterministicRng rng = new(seed);
        int age = 19 + (int)(rng.NextUInt64() % 3UL);
        int birthYear = seasonYear - age;
        string originId = $"rider.generated.{seasonYear}.{index}";
        string? preferredNation = null;
        if (retired is not null)
        {
            Person? retiredPerson = world.Persons.FirstOrDefault(person => person.Id == retired.PersonId);
            preferredNation = retiredPerson?.Nationality;
        }

        (string given, string family, string nation) = names.Pick(rng, preferredNation);
        WorldEntityId personId = world.AllocateEntityId();
        WorldEntityId careerId = world.AllocateEntityId();
        Person person = new(
            personId,
            $"{given} {family}",
            originId,
            nation,
            birthYear);
        world.AddPerson(person);

        double massKg = Lerp(61.0, 64.0, rng.NextUnitInterval());
        double criticalPowerW = Lerp(370.0, 392.0, rng.NextUnitInterval());
        double wPrimeCapacityJ = Lerp(24_000.0, 27_000.0, rng.NextUnitInterval());
        double peakPowerW = Lerp(1_000.0, 1_080.0, rng.NextUnitInterval());
        double cdARoadM2 = Lerp(0.28, 0.30, rng.NextUnitInterval());
        double cdATtM2 = cdARoadM2 * NeoTtCdAFactor;
        double positioning = Lerp(0.60, 0.78, rng.NextUnitInterval());
        double handling = Lerp(0.60, 0.78, rng.NextUnitInterval());
        double tactical = Lerp(0.60, 0.78, rng.NextUnitInterval());
        int potentialOvr = 65 + (int)(rng.NextUInt64() % 26UL);

        RiderCareer career = new(
            careerId,
            personId,
            organizationId: null,
            originId,
            criticalPowerW,
            wPrimeCapacityJ,
            peakPowerW,
            wPrimeRecoveryJPerSecond: 40.0,
            lowIntensityDurability: Lerp(0.80, 0.86, rng.NextUnitInterval()),
            highIntensityDurability: Lerp(0.80, 0.86, rng.NextUnitInterval()),
            bodyMassKg: massKg,
            systemMassKg: 8.0,
            cdAM2: cdARoadM2,
            baseCrr: 0.0039,
            positioning: positioning,
            handling: handling,
            tacticalAwareness: tactical,
            form01: 1.0,
            freshness01: 1.0,
            fatigue01: 0.0,
            loyalty01: 0.5,
            potentialOvr: potentialOvr,
            cdATtM2: cdATtM2);
        int ovr = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr).Ovr;
        career.EnsurePotentialOvrAtLeast(ovr);
        world.AddRiderCareer(career);
        return career;
    }

    private static double Lerp(double min, double max, double t) => min + ((max - min) * t);

    public sealed class NameBank
    {
        private readonly IReadOnlyList<NationNames> _nations;

        private NameBank(IReadOnlyList<NationNames> nations) => _nations = nations;

        public IReadOnlyList<NationNames> Nations => _nations;

        public static NameBank Load(string? contentRoot)
        {
            string? path = ResolvePath(contentRoot);
            if (path is null || !File.Exists(path))
            {
                return Fallback();
            }

            string json = File.ReadAllText(path);
            using JsonDocument document = JsonDocument.Parse(json);
            List<NationNames> nations = new();
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                string[] first = property.Value.GetProperty("first").EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(item => item.Length > 0)
                    .ToArray();
                string[] last = property.Value.GetProperty("last").EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(item => item.Length > 0)
                    .ToArray();
                if (first.Length == 0 || last.Length == 0)
                {
                    continue;
                }

                nations.Add(new NationNames(property.Name, first, last));
            }

            return nations.Count == 0 ? Fallback() : new NameBank(nations);
        }

        public (string Given, string Family, string Nation) Pick(DeterministicRng rng, string? preferredNation = null)
        {
            NationNames nation = ResolveNation(rng, preferredNation);
            string given = nation.First[(int)(rng.NextUInt64() % (ulong)nation.First.Count)];
            string family = nation.Last[(int)(rng.NextUInt64() % (ulong)nation.Last.Count)];
            return (given, family, nation.Code);
        }

        private NationNames ResolveNation(DeterministicRng rng, string? preferredNation)
        {
            if (!string.IsNullOrWhiteSpace(preferredNation))
            {
                NationNames? match = _nations.FirstOrDefault(item =>
                    string.Equals(item.Code, preferredNation, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    return match;
                }
            }

            return _nations[(int)(rng.NextUInt64() % (ulong)_nations.Count)];
        }

        private static string? ResolvePath(string? contentRoot)
        {
            if (!string.IsNullOrWhiteSpace(contentRoot))
            {
                string direct = Path.Combine(contentRoot, "peloton.wt-2026", "names.json");
                if (File.Exists(direct))
                {
                    return direct;
                }

                string nested = Path.Combine(contentRoot, "content", "peloton.wt-2026", "names.json");
                if (File.Exists(nested))
                {
                    return nested;
                }
            }

            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, "content", "peloton.wt-2026", "names.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static NameBank Fallback() =>
            new(
            [
                new NationNames("BEL", ["Jens", "Wout", "Jasper"], ["Peeters", "Evenepoel", "Philipsen"]),
                new NationNames("NED", ["Mathieu", "Dylan", "Fabio"], ["van der Poel", "Groenewegen", "Jakobsen"]),
                new NationNames("FRA", ["Julian", "Thibaut", "Christophe"], ["Alaphilippe", "Pinot", "Laporte"]),
                new NationNames("OTHER", ["Filippo", "Geraint", "Adam"], ["Ganna", "Thomas", "Yates"]),
            ]);
    }

    public sealed record NationNames(string Code, IReadOnlyList<string> First, IReadOnlyList<string> Last);
}
