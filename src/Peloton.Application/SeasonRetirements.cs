using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Application;

/// <summary>D-056 / CAREER_SEASON_ROLLOVER_AND_AGING_v0.1.md §4.3.</summary>
public static class SeasonRetirements
{
    public static IReadOnlyList<RiderCareer> Apply(WorldState world, int newSeasonYear)
    {
        ArgumentNullException.ThrowIfNull(world);
        List<RiderCareer> retired = new();
        foreach (RiderCareer career in world.RiderCareers)
        {
            if (career.IsRetired)
            {
                continue;
            }

            if (!ShouldRetire(world, career, newSeasonYear))
            {
                continue;
            }

            career.Retire();
            retired.Add(career);
        }

        return retired;
    }

    public static bool ShouldRetire(WorldState world, RiderCareer career, int newSeasonYear)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(career);
        Person? person = world.Persons.FirstOrDefault(item => item.Id == career.PersonId);
        if (person?.BirthYear is not int birthYear)
        {
            return false;
        }

        int age = newSeasonYear - birthYear;
        if (age >= 40)
        {
            return true;
        }

        bool hasContract = world.TryGetActiveContract(career.Id) is not null;
        int ovr = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr).Ovr;
        if (age >= 35 && ovr < 60 && !hasContract)
        {
            return true;
        }

        if (age >= 33 && !hasContract && !HasTopTwentyInLastTwoSeasons(world, career))
        {
            return true;
        }

        return false;
    }

    private static bool HasTopTwentyInLastTwoSeasons(WorldState world, RiderCareer career)
    {
        int windowStart = Math.Max(0, world.SeasonStartDayNumber - 730);
        return career.Results.Any(result =>
            !result.DidNotFinish &&
            result.Place >= 1 &&
            result.Place <= 20 &&
            result.DayNumber >= windowStart);
    }
}
