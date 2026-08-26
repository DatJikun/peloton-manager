using System;
using System.Collections.Generic;

namespace Peloton.Domain;

public sealed record DecisionOptionEvaluation(
    string Option,
    string SportingValue,
    string Risk,
    string ResourceCost,
    string OpportunityCost,
    bool RuleLegal,
    bool Defensible);

public sealed record DecisionTrace(
    string DecisionId,
    int SimulationSecond,
    string Domain,
    string DecisionType,
    WorldEntityId? ActorPersonId,
    WorldEntityId? ActingOrganizationId,
    WorldEntityId? DecisionAuthorityId,
    string Trigger,
    IReadOnlyList<string> Goals,
    IReadOnlyList<string> Constraints,
    IReadOnlyDictionary<string, string> ActorKnownInputs,
    IReadOnlyDictionary<string, string> ActorInterpretations,
    string Confidence,
    IReadOnlyList<DecisionOptionEvaluation> ConsideredOptions,
    string SelectedOption,
    IReadOnlyList<string> SelectionReasons,
    IReadOnlyList<string> CommandsEmitted,
    IReadOnlyList<WorldEntityId> RelatedEntities,
    string? TruthDebugRef);

public interface IWorldSpySink
{
    void Emit(DecisionTrace trace);
}

public sealed class NullWorldSpySink : IWorldSpySink
{
    private NullWorldSpySink()
    {
    }

    public static NullWorldSpySink Instance { get; } = new();

    public void Emit(DecisionTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
    }
}

public sealed class CollectingWorldSpySink : IWorldSpySink
{
    private readonly List<DecisionTrace> traces = new();

    public IReadOnlyList<DecisionTrace> Traces => traces;

    public void Emit(DecisionTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        traces.Add(trace);
    }
}
