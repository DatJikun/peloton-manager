using System.Collections.Generic;
using Peloton.Domain;
using Peloton.Simulation.Race;

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
    WorldEntityId? LeaderId,
    WorldEntityId? SupportId,
    RaceObjective? ObjectiveKind,
    RaceBriefingKind? BriefingKind,
    bool StrategySet,
    bool PlanConfirmed,
    bool CanStart,
    bool CanSimulate);

public sealed record RacePreparationCheckpoint(
    string RaceScenarioId,
    bool PlanConfirmed,
    WorldEntityId? LeaderId = null,
    WorldEntityId? SupportId = null,
    RaceObjective? Objective = null,
    RaceBriefingKind? BriefingKind = null)
{
    public bool StrategySet =>
        LeaderId is not null &&
        SupportId is not null &&
        Objective is not null &&
        BriefingKind is not null;
}
