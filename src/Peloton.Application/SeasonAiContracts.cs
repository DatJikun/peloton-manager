using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Application;

/// <summary>D-056 / CAREER_SEASON_ROLLOVER_AND_AGING_v0.1.md §5.</summary>
public static class SeasonAiContracts
{
    public const int MinimumSquadSize = 8;

    public static void Apply(WorldState world, int newSeasonYear)
    {
        ArgumentNullException.ThrowIfNull(world);
        WorldEntityId? playerOrganizationId = ResolvePlayerOrganizationId(world);
        int newSeasonStartDay = world.CurrentDate.DayNumber;
        int newSeasonEndDayExclusive = checked(newSeasonStartDay + world.FinancialYearDays);
        int lastSeasonStartDay = world.SeasonStartDayNumber;
        int lastSeasonEndDayExclusive = newSeasonStartDay;

        foreach (Organization organization in world.Organizations
                     .OrderBy(item => world.GetRiderCareersForOrganization(item.Id).Count)
                     .ThenBy(item => item.Id.Value))
        {
            if (playerOrganizationId is WorldEntityId playerId && organization.Id == playerId)
            {
                continue;
            }

            RenewContracts(
                world,
                organization,
                newSeasonYear,
                newSeasonStartDay,
                newSeasonEndDayExclusive,
                lastSeasonStartDay,
                lastSeasonEndDayExclusive);
            SignFreeAgents(
                world,
                organization,
                newSeasonYear,
                newSeasonStartDay,
                lastSeasonStartDay,
                lastSeasonEndDayExclusive);
        }
    }

    private static void RenewContracts(
        WorldState world,
        Organization organization,
        int newSeasonYear,
        int newSeasonStartDay,
        int newSeasonEndDayExclusive,
        int lastSeasonStartDay,
        int lastSeasonEndDayExclusive)
    {
        HashSet<WorldEntityId> topEight = TopEightByLastSeasonPoints(
            world,
            organization.Id,
            lastSeasonStartDay,
            lastSeasonEndDayExclusive);
        Dictionary<WorldEntityId, Person> personsById = world.Persons.ToDictionary(person => person.Id);

        foreach (RiderCareer career in world.GetRiderCareersForOrganization(organization.Id))
        {
            if (career.IsRetired)
            {
                continue;
            }

            RiderContract? contract = world.TryGetActiveContract(career.Id);
            if (contract is null)
            {
                continue;
            }

            int endDay = contract.EndDate.DayNumber;
            if (endDay < newSeasonStartDay || endDay >= newSeasonEndDayExclusive)
            {
                continue;
            }

            Person person = personsById[career.PersonId];
            int birthYear = person.BirthYear ??
                throw new InvalidOperationException($"Rider '{career.OriginDefinitionId}' has no BirthYear.");
            int age = newSeasonYear - birthYear;
            if (age >= 35)
            {
                continue;
            }

            if (age > 25 && !topEight.Contains(career.Id))
            {
                continue;
            }

            int extensionDays = age >= 32 ? world.FinancialYearDays : checked(world.FinancialYearDays * 2);
            int newEndDay = checked(newSeasonStartDay + extensionDays - 1);
            string wageBand = RiderMetadataCatalog.ResolveWageBand(career.OriginDefinitionId);
            int newWage = WageBands.RenewalWage(contract.AnnualWage, wageBand);
            world.TryTerminateActiveContract(career.Id, new WorldDate(checked(newSeasonStartDay - 1)));
            world.AddRiderContract(new RiderContract(
                world.AllocateEntityId(),
                career.Id,
                organization.Id,
                newWage,
                new WorldDate(newSeasonStartDay),
                new WorldDate(newEndDay)));
        }
    }

    private static void SignFreeAgents(
        WorldState world,
        Organization organization,
        int newSeasonYear,
        int newSeasonStartDay,
        int lastSeasonStartDay,
        int lastSeasonEndDayExclusive)
    {
        Dictionary<WorldEntityId, Person> personsById = world.Persons.ToDictionary(person => person.Id);
        while (world.GetRiderCareersForOrganization(organization.Id).Count < MinimumSquadSize)
        {
            RiderCareer? candidate = world.RiderCareers
                .Where(career =>
                    !career.IsRetired &&
                    career.OrganizationId is null &&
                    world.TryGetActiveContract(career.Id) is null)
                .Select(career =>
                {
                    Person person = personsById[career.PersonId];
                    int birthYear = person.BirthYear ?? newSeasonYear - 25;
                    int points = SeasonPointsQueries.ComputePoints(
                        career,
                        lastSeasonStartDay,
                        lastSeasonEndDayExclusive);
                    return (career, birthYear, points);
                })
                .OrderByDescending(item => item.points)
                .ThenByDescending(item => item.birthYear)
                .ThenBy(item => item.career.Id.Value)
                .Select(item => item.career)
                .FirstOrDefault();
            if (candidate is null)
            {
                break;
            }

            Person candidatePerson = personsById[candidate.PersonId];
            int age = newSeasonYear - (candidatePerson.BirthYear ?? newSeasonYear - 25);
            string wageBand = RiderMetadataCatalog.ResolveWageBand(candidate.OriginDefinitionId);
            (int floor, _) = WageBands.Resolve(wageBand);
            int contractLength = age <= 24
                ? checked(world.FinancialYearDays * 2)
                : world.FinancialYearDays;
            int endDay = checked(newSeasonStartDay + contractLength - 1);
            world.AddRiderContract(new RiderContract(
                world.AllocateEntityId(),
                candidate.Id,
                organization.Id,
                floor,
                new WorldDate(newSeasonStartDay),
                new WorldDate(endDay)));
            candidate.AttachToClub(organization.Id);
        }
    }

    private static HashSet<WorldEntityId> TopEightByLastSeasonPoints(
        WorldState world,
        WorldEntityId organizationId,
        int lastSeasonStartDay,
        int lastSeasonEndDayExclusive)
    {
        return world.GetRiderCareersForOrganization(organizationId)
            .Select(career => (
                career.Id,
                Points: SeasonPointsQueries.ComputePoints(career, lastSeasonStartDay, lastSeasonEndDayExclusive)))
            .OrderByDescending(item => item.Points)
            .ThenBy(item => item.Id.Value)
            .Take(8)
            .Select(item => item.Id)
            .ToHashSet();
    }

    private static WorldEntityId? ResolvePlayerOrganizationId(WorldState world)
    {
        ManagerCareer? manager = world.ManagerCareers.Count > 0 ? world.ManagerCareers[0] : null;
        if (manager?.ActiveEmploymentId is not WorldEntityId employmentId)
        {
            return null;
        }

        Employment? employment = world.Employments.FirstOrDefault(item => item.Id == employmentId);
        return employment?.OrganizationId;
    }
}
