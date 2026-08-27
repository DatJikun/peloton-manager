using Peloton.Domain;

namespace Peloton.Simulation.Race;

public enum RaceObjective
{
    StageWin,
    GeneralClassification,
}

public enum RaceResourceEstimate
{
    Strong,
    Limited,
    Depleted,
}

public enum RaceThreatEstimate
{
    Low,
    Medium,
    High,
}

public enum RaceInformationConfidence
{
    Low,
    Medium,
    High,
}

public enum RacePositionBand
{
    Front,
    Middle,
    Rear,
}

// This is the only tactical evaluator input: published signals and staff interpretations,
// deliberately excluding simulation physiology and rival truth objects (D-020).
public sealed record TeamRaceObservation(
    WorldEntityId OrganizationId,
    WorldEntityId DecisionAuthorityId,
    int OfficialGapSeconds,
    bool VisibleSplit,
    RacePositionBand LeaderPositionBand,
    RaceResourceEstimate ResourceEstimate,
    RaceThreatEstimate ThreatEstimate,
    RaceObjective Objective,
    RaceInformationConfidence Confidence);
