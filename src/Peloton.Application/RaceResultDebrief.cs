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
    string OrganizationName);

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
        RaceResultPlacement[] finishOrder = world.LastRace.FinishOrder
            .Select((id, index) => BuildPlacement(world, scenario, id, index + 1))
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

    private static RaceResultPlacement BuildPlacement(
        WorldState world,
        RaceScenario? scenario,
        WorldEntityId riderId,
        int place)
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

        return new RaceResultPlacement(
            place,
            riderId,
            Label(world, scenario, riderId),
            organizationId,
            organizationName);
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
