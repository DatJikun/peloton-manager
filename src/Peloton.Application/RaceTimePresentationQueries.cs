using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Application;

public static class RaceTimePresentationQueries
{
    public static string FormatFinishTime(double finishTimeSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(finishTimeSeconds);

        TimeSpan time = TimeSpan.FromSeconds(finishTimeSeconds);
        if (finishTimeSeconds >= 3600)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(int)time.TotalMinutes}:{time.Seconds:D2}");
    }

    public static string FormatGap(double gapSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gapSeconds);

        return "+" + FormatFinishTime(gapSeconds);
    }

    public static IReadOnlyDictionary<WorldEntityId, double> ResolveFinishTimes(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.LastRace is null)
        {
            return new Dictionary<WorldEntityId, double>();
        }

        Dictionary<WorldEntityId, double> primary = ResolveFromCompletedCalendarEntry(world);
        if (primary.Count > 0)
        {
            return primary;
        }

        return ResolveFallbackFinishTimes(world);
    }

    private static Dictionary<WorldEntityId, double> ResolveFromCompletedCalendarEntry(WorldState world)
    {
        CalendarEntry? completedEntry = world.CalendarEntries.FirstOrDefault(entry =>
            entry.DayNumber == world.LastCompletedRaceDay &&
            entry.Kind == CalendarEntryKind.Race);
        if (completedEntry?.RaceContentId is not string raceContentId)
        {
            return new Dictionary<WorldEntityId, double>();
        }

        return world.RiderStageTimes
            .Where(time =>
                string.Equals(time.RaceContentId, raceContentId, StringComparison.Ordinal) &&
                time.StageIndex == completedEntry.StageIndex)
            .GroupBy(time => time.RiderId)
            .ToDictionary(group => group.Key, group => group.Last().FinishTimeSeconds);
    }

    private static Dictionary<WorldEntityId, double> ResolveFallbackFinishTimes(WorldState world)
    {
        HashSet<WorldEntityId> finishers = world.LastRace!.FinishOrder.ToHashSet();
        Dictionary<WorldEntityId, double> times = new();
        foreach (RiderStageTime stageTime in world.RiderStageTimes)
        {
            if (!finishers.Contains(stageTime.RiderId))
            {
                continue;
            }

            times[stageTime.RiderId] = stageTime.FinishTimeSeconds;
        }

        return times;
    }

    public static (double? FinishTimeSeconds, double? GapSeconds, string? TimeLabel, string? GapLabel)
        BuildPlacementTiming(int place, double? finishTimeSeconds, double? winnerTimeSeconds)
    {
        if (finishTimeSeconds is not double resolvedFinishTime)
        {
            return (null, null, null, null);
        }

        double? gapSeconds = winnerTimeSeconds is double winnerTime
            ? Math.Max(0, resolvedFinishTime - winnerTime)
            : place == 1
                ? 0
                : null;
        string timeLabel = FormatFinishTime(resolvedFinishTime);
        string? gapLabel = gapSeconds is double resolvedGap
            ? place == 1 && resolvedGap == 0
                ? "—"
                : FormatGap(resolvedGap)
            : null;
        return (resolvedFinishTime, gapSeconds, timeLabel, gapLabel);
    }
}
