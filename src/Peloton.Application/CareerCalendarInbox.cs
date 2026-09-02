using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record CalendarEntryProjection(
    WorldEntityId Id,
    int DayNumber,
    string Kind,
    string Status,
    string Title,
    string? OfficialResult,
    string? RaceContentId,
    int StageIndex);

public sealed record SeasonEventProjection(
    string RaceContentId,
    string Name,
    int StartDay,
    int EndDay,
    int StageCount,
    string Status);

public sealed record MarketRiderProjection(
    WorldEntityId RiderCareerId,
    string Name,
    string OrganizationName,
    string OrganizationOriginId,
    int AnnualWage,
    int ContractEndDay,
    int Climb,
    int Hills,
    int Flat,
    int TimeTrial,
    int Sprint,
    int Cobbles,
    int Ovr,
    int PotentialOvr);

public sealed record InboxItemProjection(
    string Identity,
    string Category,
    string Body,
    int? DayNumber,
    WorldEntityId? RelatedEntryId);

internal static partial class CareerProjectionQueries
{
    private static readonly Regex StageSuffix = StageSuffixRegex();

    public static IReadOnlyList<CalendarEntryProjection> BuildCalendar(WorldState world, AccessContext access)
    {
        ArgumentNullException.ThrowIfNull(world);

        return world.CalendarEntries
            .Select(entry => new CalendarEntryProjection(
                entry.Id,
                entry.DayNumber,
                FormatKind(entry.Kind),
                DeriveStatus(world, entry, access),
                entry.Title,
                entry.OfficialResult,
                entry.RaceContentId,
                entry.StageIndex))
            .ToArray();
    }

    public static IReadOnlyList<SeasonEventProjection> BuildSeasonEvents(
        WorldState world,
        AccessContext access)
    {
        ArgumentNullException.ThrowIfNull(world);
        return GroupSeasonEvents(BuildCalendar(world, access));
    }

    public static IReadOnlyList<SeasonEventProjection> BuildUpcomingEvents(
        WorldState world,
        AccessContext access)
    {
        ArgumentNullException.ThrowIfNull(world);
        int today = world.CurrentDate.DayNumber;
        return GroupSeasonEvents(BuildCalendar(world, access))
            .Where(item => item.EndDay >= today)
            .OrderBy(item => item.StartDay)
            .Take(5)
            .ToArray();
    }

    public static IReadOnlyList<CalendarEntryProjection> StagesForEvent(
        WorldState world,
        AccessContext access,
        string raceContentId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrWhiteSpace(raceContentId);

        return BuildCalendar(world, access)
            .Where(entry =>
                string.Equals(entry.RaceContentId, raceContentId, StringComparison.Ordinal))
            .OrderBy(entry => entry.DayNumber)
            .ToArray();
    }

    public static IReadOnlyList<MarketRiderProjection> BuildMarketRiders(
        WorldState world,
        AccessContext access)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (access.CurrentOrganizationId is not WorldEntityId employerId)
        {
            return Array.Empty<MarketRiderProjection>();
        }

        Dictionary<WorldEntityId, Person> personsById = world.Persons.ToDictionary(person => person.Id);
        Dictionary<WorldEntityId, Organization> organizationsById = world.Organizations
            .ToDictionary(organization => organization.Id);

        return world.RiderCareers
            .Where(career => !career.IsRetired && career.OrganizationId != employerId)
            .Select(career =>
            {
                Person person = personsById[career.PersonId];
                Organization? organization = career.OrganizationId is WorldEntityId organizationId
                    ? organizationsById.GetValueOrDefault(organizationId)
                    : null;
                RiderContract? contract = world.TryGetActiveContract(career.Id);
                RiderRatingSet ratings = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr);
                return new MarketRiderProjection(
                    career.Id,
                    person.Name,
                    organization?.Name ?? string.Empty,
                    organization?.OriginDefinitionId ?? string.Empty,
                    contract?.AnnualWage ?? 0,
                    contract?.EndDate.DayNumber ?? 0,
                    ratings.Climb,
                    ratings.Hills,
                    ratings.Flat,
                    ratings.TimeTrial,
                    ratings.Sprint,
                    ratings.Cobbles,
                    ratings.Ovr,
                    ratings.PotentialOvr);
            })
            .OrderBy(rider => rider.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<InboxItemProjection> BuildInbox(WorldState world, AccessContext access)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (access.CurrentOrganizationId is WorldEntityId organizationId &&
            world.IsRaceDueForOrganization(organizationId))
        {
            CalendarEntry? dueEntry = world.CalendarEntries
                .FirstOrDefault(entry =>
                    entry.DayNumber == world.CurrentDate.DayNumber &&
                    DeriveStatus(world, entry, access) == "due");
            if (dueEntry is not null)
            {
                string eventName = StripStageSuffix(dueEntry.Title);
                return new[]
                {
                    new InboxItemProjection(
                        FormatDueIdentity(dueEntry.Id),
                        "race-due",
                        string.Create(CultureInfo.InvariantCulture, $"Dziś jest wyścig: {eventName}."),
                        dueEntry.DayNumber,
                        dueEntry.Id),
                };
            }
        }

        List<InboxItemProjection> results = new();
        if (access.CurrentOrganizationId is WorldEntityId employerId)
        {
            if (world.SeasonSummaryInboxYear is int seasonYear &&
                !string.IsNullOrWhiteSpace(world.SeasonSummaryInboxBody))
            {
                string identity = SeasonInboxSupport.FormatSeasonSummaryIdentity(seasonYear);
                if (!world.IsInboxItemDismissed(identity))
                {
                    results.Add(new InboxItemProjection(
                        identity,
                        "season-summary",
                        world.SeasonSummaryInboxBody,
                        world.SeasonStartDayNumber,
                        null));
                }
            }

            results.AddRange(SeasonInboxSupport.BuildContractExpiryWarnings(world, employerId));
        }

        foreach (CalendarEntry entry in world.CalendarEntries)
        {
            if (entry.OfficialResult is not null && !entry.ResultAcknowledged)
            {
                string eventName = StripStageSuffix(entry.Title);
                results.Add(new InboxItemProjection(
                    FormatResultIdentity(entry.Id),
                    "race-result",
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{eventName} zakończony. {entry.OfficialResult}."),
                    entry.DayNumber,
                    entry.Id));
            }
        }

        return results;
    }

    public static string FormatDueIdentity(WorldEntityId entryId) =>
        string.Create(CultureInfo.InvariantCulture, $"calendar:{entryId.Value}:due");

    public static string FormatResultIdentity(WorldEntityId entryId) =>
        string.Create(CultureInfo.InvariantCulture, $"calendar:{entryId.Value}:result");

    public static string StripStageSuffix(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        return StageSuffix.Replace(title, string.Empty);
    }

    private static SeasonEventProjection[] GroupSeasonEvents(
        IReadOnlyList<CalendarEntryProjection> calendar)
    {
        return calendar
            .Where(entry => entry.Kind == "race")
            .GroupBy(
                entry => !string.IsNullOrWhiteSpace(entry.RaceContentId)
                    ? entry.RaceContentId!
                    : entry.Title,
                StringComparer.Ordinal)
            .Select(group =>
            {
                CalendarEntryProjection first = group.OrderBy(entry => entry.DayNumber).First();
                string name = StripStageSuffix(first.Title);
                string raceContentId = first.RaceContentId ?? first.Title;
                string status = group.Any(entry => entry.Status == "due")
                    ? "due"
                    : group.All(entry => entry.Status == "completed")
                        ? "completed"
                        : "scheduled";
                return new SeasonEventProjection(
                    raceContentId,
                    name,
                    group.Min(entry => entry.DayNumber),
                    group.Max(entry => entry.DayNumber),
                    group.Count(),
                    status);
            })
            .OrderBy(item => item.StartDay)
            .ToArray();
    }

    private static string DeriveStatus(WorldState world, CalendarEntry entry, AccessContext access)
    {
        if (entry.DayNumber <= world.LastCompletedRaceDay)
        {
            return "completed";
        }

        if (entry.DayNumber == world.CurrentDate.DayNumber &&
            access.CurrentOrganizationId is WorldEntityId organizationId &&
            world.IsRaceDueForOrganization(organizationId))
        {
            return "due";
        }

        return "scheduled";
    }

    private static string FormatKind(CalendarEntryKind kind) =>
        kind switch
        {
            CalendarEntryKind.Race => "race",
            _ => throw new InvalidOperationException($"Unsupported calendar entry kind '{kind}'."),
        };

    [GeneratedRegex(@" — Stage \d+$", RegexOptions.CultureInvariant)]
    private static partial Regex StageSuffixRegex();
}
