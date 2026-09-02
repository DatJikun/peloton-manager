using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation;

namespace Peloton.Application;

public static class SeasonAging
{
    public const double GrowthVarianceAmplitude = 0.006;

    public static int DeriveBirthYear(long masterSeed, string riderOriginId, int seasonYear = 2026)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(riderOriginId);
        ulong seed = StableSeedDerivation.Derive(masterSeed, $"birth-year:{riderOriginId}");
        DeterministicRng rng = new(seed);
        int age = 20 + (int)(rng.NextUInt64() % 15UL);
        return seasonYear - age;
    }

    public static void Apply(WorldState world, int newSeasonYear)
    {
        ArgumentNullException.ThrowIfNull(world);
        Dictionary<WorldEntityId, Person> personsById = world.Persons.ToDictionary(person => person.Id);
        foreach (RiderCareer career in world.RiderCareers)
        {
            Person person = personsById[career.PersonId];
            int birthYear = person.BirthYear ??
                throw new InvalidOperationException($"Rider '{career.OriginDefinitionId}' has no BirthYear.");
            int age = newSeasonYear - birthYear;
            ApplyToCareer(world.MasterSeed, newSeasonYear, career, age);
        }
    }

    public static void ApplyToCareer(long masterSeed, int seasonYear, RiderCareer career, int age)
    {
        ArgumentNullException.ThrowIfNull(career);
        int currentOvr = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr).Ovr;
        double growth = GrowthForAge(age);
        bool growthYear = age <= 28;
        double talentGate = 1.0;
        if (growthYear)
        {
            talentGate = Math.Clamp((career.PotentialOvr - currentOvr) / 15.0, 0.2, 1.0);
        }

        ulong varianceSeed = StableSeedDerivation.Derive(
            masterSeed,
            $"aging:{seasonYear}:{career.Id.Value}");
        double variance = new DeterministicRng(varianceSeed).NextSignedAmplitude(GrowthVarianceAmplitude);
        double factor = 1.0 + ((growth + variance) * talentGate);

        double low = career.LowIntensityDurability;
        if (age <= 30)
        {
            low = Clamp01(low + 0.010);
        }
        else if (age > 33)
        {
            low = Clamp01(low - 0.010);
        }

        double high = career.HighIntensityDurability;
        if (age > 31)
        {
            high = Clamp01(high - 0.005);
        }

        double positioning = career.Positioning;
        double handling = career.Handling;
        double tactical = career.TacticalAwareness;
        if (age <= 30)
        {
            positioning = Math.Min(0.98, positioning + 0.010);
            handling = Math.Min(0.98, handling + 0.010);
            tactical = Math.Min(0.98, tactical + 0.010);
        }

        career.ApplyAgingPhysiology(
            career.CriticalPowerW * factor,
            career.WPrimeCapacityJ * factor,
            career.PeakPowerW * factor,
            low,
            high,
            positioning,
            handling,
            tactical);

        int afterOvr = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr).Ovr;
        career.EnsurePotentialOvrAtLeast(afterOvr);
    }

    public static double GrowthForAge(int age)
    {
        if (age <= 22)
        {
            return 0.030;
        }

        if (age <= 25)
        {
            return 0.018;
        }

        if (age <= 28)
        {
            return 0.006;
        }

        if (age <= 31)
        {
            return 0.000;
        }

        if (age <= 34)
        {
            return -0.012;
        }

        if (age <= 37)
        {
            return -0.025;
        }

        return -0.040;
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0.0, 1.0);
}
