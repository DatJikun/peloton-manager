using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public static class RacePreparationDefaults
{
    public const string PrototypeScenarioId = "race-scenario.peloton.prototype-v0";
    public const string Title = "Skeleton race";
    public const string Objective = "StageWin";
}

public sealed record RacePreparationProjection(
    string Title,
    string Objective,
    IReadOnlyList<WorldEntityId> Squad,
    bool PlanConfirmed,
    bool CanStart,
    bool CanSimulate);

public sealed record RacePreparationCheckpoint(
    string RaceScenarioId,
    bool PlanConfirmed);
