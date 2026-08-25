using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public sealed record RaceRiderMetrics(
    WorldEntityId RiderId,
    WorldEntityId OrganizationId,
    double FinishTimeSeconds,
    double EnergySpentJ,
    double WPrimeRemainingJ,
    int TimeAboveCriticalPowerSeconds,
    double MaximumGapAheadM,
    int LostShelterTransitions,
    int FinalGroupId);

public sealed record RaceResult(
    string ScenarioId,
    string RouteId,
    int PhysicsContractVersion,
    IReadOnlyList<WorldEntityId> FinishOrder,
    IReadOnlyList<RaceRiderMetrics> RiderMetrics,
    IReadOnlyDictionary<WorldEntityId, double> TeamEnergyJ,
    int MaximumGroupCount,
    int DecisionCount,
    string Checksum)
{
    public WorldEntityId WinnerId => FinishOrder[0];
}

public enum RaceStepStatus
{
    Advanced,
    DecisionRequired,
    Completed,
}

public sealed record RaceStepResult(RaceStepStatus Status, RaceResult? Result);
