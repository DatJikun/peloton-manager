using System;
using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public static class RacePreparationDefaults
{
    public const string PrototypeScenarioId = "race-scenario.peloton.prototype-v0";
    public const string Title = "Skeleton race";
    public const string Objective = "StageWin";
}

public static class SquadRoles
{
    public const string Leader = "Leader";
    public const string Card = "Card";
    public const string Worker = "Worker";

    public static bool IsKnown(string role) =>
        role is Leader or Card or Worker;

    public static string Why(string role) => role switch
    {
        Leader => "Leads the finale.",
        Card => "The result rider.",
        _ => "Keeps the leader in the wheels.",
    };
}

public sealed record SquadSeat(
    WorldEntityId RiderId,
    string Name,
    string Role,
    string Why);

public sealed record SquadAssignment(WorldEntityId RiderId, string Role);

public sealed record RacePreparationProjection(
    string Title,
    string Objective,
    IReadOnlyList<WorldEntityId> Squad,
    IReadOnlyList<SquadSeat> Seats,
    bool PlanConfirmed,
    bool CanStart,
    bool CanSimulate);

public sealed record RacePreparationCheckpoint(
    string RaceScenarioId,
    bool PlanConfirmed,
    IReadOnlyList<SquadAssignment>? Assignments = null)
{
    public bool Equals(RacePreparationCheckpoint? other)
    {
        if (other is null)
        {
            return false;
        }

        if (!string.Equals(RaceScenarioId, other.RaceScenarioId, StringComparison.Ordinal) ||
            PlanConfirmed != other.PlanConfirmed)
        {
            return false;
        }

        return AssignmentsEqual(Assignments, other.Assignments);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(RaceScenarioId, StringComparer.Ordinal);
        hash.Add(PlanConfirmed);
        foreach (SquadAssignment assignment in Assignments ?? Array.Empty<SquadAssignment>())
        {
            hash.Add(assignment);
        }

        return hash.ToHashCode();
    }

    private static bool AssignmentsEqual(
        IReadOnlyList<SquadAssignment>? left,
        IReadOnlyList<SquadAssignment>? right)
    {
        IReadOnlyList<SquadAssignment> first = left ?? Array.Empty<SquadAssignment>();
        IReadOnlyList<SquadAssignment> second = right ?? Array.Empty<SquadAssignment>();
        if (first.Count != second.Count)
        {
            return false;
        }

        for (int index = 0; index < first.Count; index++)
        {
            if (first[index] != second[index])
            {
                return false;
            }
        }

        return true;
    }
}
