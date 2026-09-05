using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public sealed record RaceResultPlacement(
    int Place,
    WorldEntityId RiderId,
    string Label,
    WorldEntityId? OrganizationId,
    string OrganizationName,
    double? FinishTimeSeconds = null,
    double? GapSeconds = null);

public sealed record RaceResultProjection(
    string Title,
    string RouteId,
    WorldEntityId WinnerId,
    string WinnerLabel,
    IReadOnlyList<RaceResultPlacement> FinishOrder);

public sealed record RaceDebriefProjection(
    string Objective,
    IReadOnlyList<string> Notes);

public static class RaceOutcomeQueries
{
    public const string UncertainStaffNote = "sztab nie ma pewności";

    public static RaceResultProjection? BuildResult(
        WorldState world,
        RacePreparationCheckpoint? racePreparation,
        IRaceScenarioCatalog raceScenarioCatalog)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(raceScenarioCatalog);
        if (world.LastRace is null)
        {
            return null;
        }

        RaceScenario? scenario = TryResolve(racePreparation, raceScenarioCatalog);
        (string RaceContentId, int StageIndex)? raceContext = TryResolveCompletedRaceContext(world);
        double? winnerTime = null;
        if (raceContext is { } resolvedRace &&
            world.LastRace.FinishOrder.Count > 0)
        {
            winnerTime = TryGetFinishTime(
                world,
                resolvedRace.RaceContentId,
                resolvedRace.StageIndex,
                world.LastRace.FinishOrder[0]);
        }

        RaceResultPlacement[] finishOrder = world.LastRace.FinishOrder
            .Select((id, index) => BuildPlacement(
                world,
                scenario,
                id,
                index + 1,
                raceContext,
                winnerTime))
            .ToArray();
        return new RaceResultProjection(
            CompletedCalendarTitle(world) ?? RacePreparationDefaults.Title,
            world.LastRace.RouteId,
            world.LastRace.WinnerId,
            Label(world, scenario, world.LastRace.WinnerId),
            Array.AsReadOnly(finishOrder));
    }

    public static IReadOnlyList<RaceResultPlacement> FilterFinishOrderByOrganization(
        IReadOnlyList<RaceResultPlacement> finishOrder,
        WorldEntityId organizationId)
    {
        ArgumentNullException.ThrowIfNull(finishOrder);
        return finishOrder
            .Where(place => place.OrganizationId == organizationId)
            .ToArray();
    }

    public static string FormatTable(RaceResultProjection result, WorldEntityId? organizationId)
    {
        ArgumentNullException.ThrowIfNull(result);
        IReadOnlyList<RaceResultPlacement> rows = organizationId is { } id
            ? FilterFinishOrderByOrganization(result.FinishOrder, id)
            : result.FinishOrder;
        return string.Join(
            '\n',
            rows.Select(row => string.Create(
                CultureInfo.InvariantCulture,
                $"{row.Place}. {row.Label}  {row.OrganizationName}")));
    }

    public static string FormatClock(double seconds)
    {
        int totalSeconds = (int)Math.Round(seconds, MidpointRounding.AwayFromZero);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;
        if (hours > 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{hours}:{minutes:D2}:{secs:D2}");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{minutes}:{secs:D2}");
    }

    public static string FormatGap(double? gapSeconds)
    {
        if (gapSeconds is not double gap)
        {
            return string.Empty;
        }

        if (gap <= 0)
        {
            return "—";
        }

        return string.Create(CultureInfo.InvariantCulture, $"+{FormatClock(gap)}");
    }

    public static RaceDebriefProjection BuildDebrief(
        WorldState? world,
        RacePreparationCheckpoint? racePreparation,
        IRaceScenarioCatalog raceScenarioCatalog)
    {
        ArgumentNullException.ThrowIfNull(raceScenarioCatalog);
        List<string> notes = new();
        if (world?.LastRace is not null)
        {
            RaceScenario? scenario = TryResolve(racePreparation, raceScenarioCatalog);
            notes.Add($"Oficjalny zwycięzca: {Label(world, scenario, world.LastRace.WinnerId)}.");
        }

        if (notes.Count == 0)
        {
            notes.Add(UncertainStaffNote);
        }

        return new RaceDebriefProjection(
            RacePreparationDefaults.Objective,
            notes.Take(3).ToArray());
    }

    private static string? CompletedCalendarTitle(WorldState world)
    {
        CalendarEntry? onCompletedDay = world.CalendarEntries.FirstOrDefault(entry =>
            entry.DayNumber == world.LastCompletedRaceDay &&
            entry.Kind == CalendarEntryKind.Race);
        if (onCompletedDay is not null)
        {
            return onCompletedDay.Title;
        }

        return world.CalendarEntries
            .Where(entry => entry.OfficialResult is not null)
            .OrderByDescending(entry => entry.DayNumber)
            .ThenBy(entry => entry.Id.Value)
            .Select(entry => entry.Title)
            .FirstOrDefault();
    }

    private static RaceScenario? TryResolve(
        RacePreparationCheckpoint? racePreparation,
        IRaceScenarioCatalog raceScenarioCatalog)
    {
        if (racePreparation is null || string.IsNullOrWhiteSpace(racePreparation.RaceScenarioId))
        {
            return null;
        }

        try
        {
            return raceScenarioCatalog.Resolve(racePreparation.RaceScenarioId);
        }
        catch (Exception exception) when (exception is IOException or ArgumentException)
        {
            return null;
        }
    }

    private static (string RaceContentId, int StageIndex)? TryResolveCompletedRaceContext(WorldState world)
    {
        CalendarEntry? entry = world.CalendarEntries.FirstOrDefault(item =>
            item.DayNumber == world.LastCompletedRaceDay &&
            item.Kind == CalendarEntryKind.Race);
        if (entry is null || string.IsNullOrWhiteSpace(entry.RaceContentId))
        {
            return null;
        }

        return (entry.RaceContentId, entry.StageIndex);
    }

    private static double? TryGetFinishTime(
        WorldState world,
        string raceContentId,
        int stageIndex,
        WorldEntityId riderId)
    {
        RiderStageTime? stageTime = world.RiderStageTimes.FirstOrDefault(item =>
            string.Equals(item.RaceContentId, raceContentId, StringComparison.Ordinal) &&
            item.StageIndex == stageIndex &&
            item.RiderId == riderId);
        return stageTime?.FinishTimeSeconds;
    }

    private static RaceResultPlacement BuildPlacement(
        WorldState world,
        RaceScenario? scenario,
        WorldEntityId riderId,
        int place,
        (string RaceContentId, int StageIndex)? raceContext,
        double? winnerTime)
    {
        RiderCareer? career = world.TryGetRiderCareer(riderId);
        WorldEntityId? organizationId = career?.OrganizationId;
        string organizationName = string.Empty;
        if (organizationId is WorldEntityId resolvedOrganizationId)
        {
            Organization? organization = world.Organizations.FirstOrDefault(
                item => item.Id == resolvedOrganizationId);
            organizationName = organization?.Name ?? string.Empty;
        }

        double? finishTimeSeconds = null;
        double? gapSeconds = null;
        if (raceContext is { } resolvedRace &&
            winnerTime is double winner &&
            TryGetFinishTime(world, resolvedRace.RaceContentId, resolvedRace.StageIndex, riderId) is double riderTime)
        {
            finishTimeSeconds = riderTime;
            gapSeconds = riderTime - winner;
        }

        return new RaceResultPlacement(
            place,
            riderId,
            Label(world, scenario, riderId),
            organizationId,
            organizationName,
            finishTimeSeconds,
            gapSeconds);
    }

    private static string Label(WorldState world, RaceScenario? scenario, WorldEntityId riderId)
    {
        RiderCareer? career = world.TryGetRiderCareer(riderId);
        if (career is not null)
        {
            return career.OriginDefinitionId;
        }

        if (scenario is not null)
        {
            RaceRiderProfile? rider = scenario.Riders.FirstOrDefault(item => item.RiderId == riderId);
            if (rider is not null && !string.IsNullOrWhiteSpace(rider.ContentId))
            {
                return rider.ContentId;
            }
        }

        return string.Create(CultureInfo.InvariantCulture, $"rider.{riderId.Value}");
    }
}
