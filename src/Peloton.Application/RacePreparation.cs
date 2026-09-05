using System;
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
    bool CanSimulate,
    int RequiredStartersCount = 4,
    IReadOnlyList<WorldEntityId>? SelectedRiderIds = null);

public sealed record RacePreparationCheckpoint(
    string RaceScenarioId,
    bool PlanConfirmed,
    WorldEntityId? LeaderId = null,
    WorldEntityId? SupportId = null,
    RaceObjective? Objective = null,
    RaceBriefingKind? BriefingKind = null,
    IReadOnlyList<WorldEntityId>? SelectedRiderIds = null)
{
    public bool StrategySet =>
        LeaderId is not null &&
        SupportId is not null &&
        Objective is not null &&
        BriefingKind is not null;

    public bool Equals(RacePreparationCheckpoint? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return RaceScenarioId == other.RaceScenarioId &&
               PlanConfirmed == other.PlanConfirmed &&
               LeaderId == other.LeaderId &&
               SupportId == other.SupportId &&
               Objective == other.Objective &&
               BriefingKind == other.BriefingKind &&
               SequenceEqual(SelectedRiderIds, other.SelectedRiderIds);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(RaceScenarioId);
        hash.Add(PlanConfirmed);
        hash.Add(LeaderId);
        hash.Add(SupportId);
        hash.Add(Objective);
        hash.Add(BriefingKind);
        if (SelectedRiderIds is not null)
        {
            foreach (WorldEntityId id in SelectedRiderIds)
            {
                hash.Add(id);
            }
        }

        return hash.ToHashCode();
    }

    private static bool SequenceEqual(IReadOnlyList<WorldEntityId>? first, IReadOnlyList<WorldEntityId>? second)
    {
        if (first is null && second is null)
        {
            return true;
        }

        if (first is null || second is null || first.Count != second.Count)
        {
            return false;
        }

        for (int i = 0; i < first.Count; i++)
        {
            if (first[i] != second[i])
            {
                return false;
            }
        }

        return true;
    }
}
