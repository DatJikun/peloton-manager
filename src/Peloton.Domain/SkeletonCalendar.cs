using System;
using System.Linq;

namespace Peloton.Domain;

public static class SkeletonCalendar
{
    public const string OpeningClassic = "Opening Classic";
    public const string HillClassic = "Hill Classic";
    public const string SeasonFinale = "Season Finale";

    public static int[] Offsets(int periodDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(periodDays);
        return new[] { periodDays / 3, (periodDays * 2) / 3, periodDays };
    }

    public static string TitleForOffset(int offset, int periodDays)
    {
        int[] offsets = Offsets(periodDays);
        if (offset == offsets[0])
        {
            return OpeningClassic;
        }

        if (offset == offsets[1])
        {
            return HillClassic;
        }

        if (offset == offsets[2])
        {
            return SeasonFinale;
        }

        return "Skeleton race";
    }

    public static CalendarEntry[] CreateSeason(
        WorldEntityIdAllocator allocator,
        int seasonIndex,
        int periodDays)
    {
        ArgumentNullException.ThrowIfNull(allocator);
        ArgumentOutOfRangeException.ThrowIfNegative(seasonIndex);
        int start = checked(seasonIndex * periodDays);
        return Offsets(periodDays)
            .Select(offset => new CalendarEntry(
                allocator.Allocate(),
                start + offset,
                CalendarEntryKind.Race,
                TitleForOffset(offset, periodDays)))
            .ToArray();
    }
}
