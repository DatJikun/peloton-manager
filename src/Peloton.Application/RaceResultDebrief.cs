using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public sealed record RaceResultPlacement(
    WorldEntityId RiderId,
    string Label);

public sealed record RaceResultProjection(
    string Title,
    string RouteId,
    WorldEntityId WinnerId,
    string WinnerLabel,
    IReadOnlyList<RaceResultPlacement> FinishOrder,
    IReadOnlyList<string> Headlines);

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
            .Select(id => new RaceResultPlacement(id, Label(world, scenario, id)))
            .ToArray();
        string title = CompletedCalendarTitle(world) ?? RacePreparationDefaults.Title;
        string winnerLabel = Label(world, scenario, world.LastRace.WinnerId);
        return new RaceResultProjection(
            title,
            world.LastRace.RouteId,
            world.LastRace.WinnerId,
            winnerLabel,
            Array.AsReadOnly(finishOrder),
            BuildHeadlines(world, title, world.LastRace.WinnerId, winnerLabel, racePreparation));
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

    private static IReadOnlyList<string> BuildHeadlines(
        WorldState world,
        string title,
        WorldEntityId winnerId,
        string winnerLabel,
        RacePreparationCheckpoint? racePreparation)
    {
        List<string> headlines = new()
        {
            $"{title}: wygrał {winnerLabel}.",
        };

        IReadOnlyList<SquadSeat> seats = CareerRaceBinder.Seats(world, racePreparation?.Assignments);
        foreach (SquadSeat seat in seats.OrderBy(seat => Place(world.LastRace!.FinishOrder, seat.RiderId)))
        {
            int place = Place(world.LastRace.FinishOrder, seat.RiderId);
            headlines.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{seat.Name} ({seat.Role}) — {place}. miejsce."));
        }

        bool teamWon = seats.Any(seat => seat.RiderId == winnerId);
        headlines.Add(teamWon
            ? "Cel StageWin: wasz kolarz wygrał."
            : "Cel StageWin: nie tym razem.");
        return headlines;
    }

    private static int Place(IReadOnlyList<WorldEntityId> finishOrder, WorldEntityId riderId)
    {
        for (int index = 0; index < finishOrder.Count; index++)
        {
            if (finishOrder[index] == riderId)
            {
                return index + 1;
            }
        }

        return finishOrder.Count + 1;
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

    private static string Label(WorldState? world, RaceScenario? scenario, WorldEntityId riderId)
    {
        Person? person = world?.Persons.FirstOrDefault(item => item.Id == riderId);
        if (person is not null && !string.IsNullOrWhiteSpace(person.Name))
        {
            return person.Name;
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
