using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record CalendarEntryProjection(
    WorldEntityId Id,
    int DayNumber,
    string Kind,
    string Status,
    string Title);

public sealed record InboxItemProjection(
    string Identity,
    string Category,
    string Body,
    int? DayNumber,
    WorldEntityId? RelatedEntryId);

internal static class CareerProjectionQueries
{
    public static IReadOnlyList<CalendarEntryProjection> BuildCalendar(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        return world.CalendarEntries
            .Select(entry => new CalendarEntryProjection(
                entry.Id,
                entry.DayNumber,
                FormatKind(entry.Kind),
                DeriveStatus(world, entry),
                entry.Title))
            .ToArray();
    }

    public static IReadOnlyList<InboxItemProjection> BuildInbox(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.IsRaceDue)
        {
            return Array.Empty<InboxItemProjection>();
        }

        CalendarEntry? dueEntry = world.CalendarEntries
            .FirstOrDefault(entry =>
                entry.DayNumber == world.CurrentDate.DayNumber &&
                DeriveStatus(world, entry) == "due");
        if (dueEntry is null)
        {
            return Array.Empty<InboxItemProjection>();
        }

        return new[]
        {
            new InboxItemProjection(
                FormatDueIdentity(dueEntry.Id),
                "race-due",
                "A race is due today.",
                dueEntry.DayNumber,
                dueEntry.Id),
        };
    }

    public static string FormatDueIdentity(WorldEntityId entryId) =>
        string.Create(CultureInfo.InvariantCulture, $"calendar:{entryId.Value}:due");

    private static string DeriveStatus(WorldState world, CalendarEntry entry)
    {
        if (entry.DayNumber <= world.LastCompletedRaceDay)
        {
            return "completed";
        }

        if (entry.DayNumber == world.CurrentDate.DayNumber && world.IsRaceDue)
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
}
