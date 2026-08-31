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
    IReadOnlyList<SquadAssignment>? Assignments = null);
