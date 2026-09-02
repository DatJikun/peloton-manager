using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Peloton.Domain;

namespace Peloton.Application;

public static class SeasonInboxSupport
{
    public static void PublishSeasonSummary(WorldState world, int completedSeasonYear)
    {
        ArgumentNullException.ThrowIfNull(world);
        WorldEntityId? resolvedEmployerId = ResolvePlayerOrganizationId(world);
        if (resolvedEmployerId is null)
        {
            return;
        }

        WorldEntityId employerId = resolvedEmployerId.Value;
        int completedSeasonStart = world.SeasonStartDayNumber;
        int newSeasonStart = world.CurrentDate.DayNumber;

        Organization employer = world.Organizations.Single(organization => organization.Id == employerId);
        Dictionary<WorldEntityId, Person> personsById = world.Persons.ToDictionary(person => person.Id);
        List<(string Name, int Points)> topResults = world.GetRiderCareersForOrganization(employerId)
            .Select(career =>
            {
                Person person = personsById[career.PersonId];
                return (
                    person.Name,
                    SeasonPointsQueries.ComputePoints(career, completedSeasonStart, newSeasonStart));
            })
            .Where(item => item.Item2 > 0)
            .OrderByDescending(item => item.Item2)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .Take(3)
            .ToList();

        int squadRetirements = world.RiderCareers.Count(career =>
            career.IsRetired &&
            career.RetiredFromOrganizationId == employerId);

        List<string> expiring = world.GetRiderCareersForOrganization(employerId)
            .Select(career =>
            {
                RiderContract? contract = world.TryGetActiveContract(career.Id);
                if (contract is null)
                {
                    return null;
                }

                Person person = personsById[career.PersonId];
                int endDay = contract.EndDate.DayNumber;
                if (endDay < newSeasonStart ||
                    endDay >= checked(newSeasonStart + world.FinancialYearDays))
                {
                    return null;
                }

                return $"{person.Name} ({CareerCalendarDates.FormatLong(endDay)})";
            })
            .Where(item => item is not null)
            .Cast<string>()
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToList();

        StringBuilder body = new();
        body.Append(CultureInfo.InvariantCulture, $"Podsumowanie sezonu {completedSeasonYear} — {employer.Name}.");
        if (topResults.Count > 0)
        {
            body.Append(" Najlepsze wyniki: ");
            body.Append(string.Join(
                ", ",
                topResults.Select(item =>
                    string.Create(CultureInfo.InvariantCulture, $"{item.Name} ({item.Points} pkt)"))));
            body.Append('.');
        }

        if (squadRetirements > 0)
        {
            body.Append(CultureInfo.InvariantCulture, $" Emerytury w kadrze: {squadRetirements}.");
        }

        if (expiring.Count > 0)
        {
            body.Append(" Kontrakty wygasają w tym sezonie: ");
            body.Append(string.Join(", ", expiring));
            body.Append('.');
        }

        world.SetSeasonSummaryInbox(completedSeasonYear, body.ToString());
    }

    public static string FormatSeasonSummaryIdentity(int seasonYear) =>
        string.Create(CultureInfo.InvariantCulture, $"season-summary:{seasonYear}");

    public static string FormatContractExpiryIdentity(WorldEntityId riderId, int endDay, int daysRemaining) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"contract-expiry:{riderId.Value}:{endDay}:{daysRemaining}");

    public static IReadOnlyList<InboxItemProjection> BuildContractExpiryWarnings(
        WorldState world,
        WorldEntityId organizationId)
    {
        ArgumentNullException.ThrowIfNull(world);
        Dictionary<WorldEntityId, Person> personsById = world.Persons.ToDictionary(person => person.Id);
        List<InboxItemProjection> warnings = new();
        int today = world.CurrentDate.DayNumber;
        foreach (RiderCareer career in world.GetRiderCareersForOrganization(organizationId))
        {
            RiderContract? contract = world.TryGetActiveContract(career.Id);
            if (contract is null)
            {
                continue;
            }

            int daysRemaining = contract.EndDate.DayNumber - today;
            if (daysRemaining is not 60 and not 30)
            {
                continue;
            }

            string identity = FormatContractExpiryIdentity(career.Id, contract.EndDate.DayNumber, daysRemaining);
            if (world.IsInboxItemDismissed(identity))
            {
                continue;
            }

            Person person = personsById[career.PersonId];
            warnings.Add(new InboxItemProjection(
                identity,
                "contract-expiry",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Kontrakt {person.Name} wygasa za {daysRemaining} dni ({CareerCalendarDates.FormatLong(contract.EndDate.DayNumber)})."),
                contract.EndDate.DayNumber,
                career.Id));
        }

        return warnings;
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
