using System.Collections.Generic;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public sealed record RaceTeamTemplate(
    string Id,
    RaceObjective Objective,
    RaceBriefing Briefing);

public sealed record RaceCommandTemplate(
    int SimulationSecond,
    string TeamId,
    string RiderId,
    RaceCommandKind Intent);

public sealed record RaceTacticalPlanTemplate(
    int TriggerSecond,
    string TeamId,
    string SupportRiderId,
    int OfficialGapSeconds,
    bool VisibleSplit,
    RacePositionBand LeaderPositionBand,
    RaceResourceEstimate ResourceEstimate,
    RaceThreatEstimate ThreatEstimate,
    RaceInformationConfidence Confidence);

public sealed record RaceScenarioTemplate(
    string Id,
    string TuningIdentity,
    double AirDensityKgPerM3,
    double InitialSpeedMps,
    int MaximumDurationSeconds,
    RaceDefinition Route,
    IReadOnlyDictionary<string, RaceTeamTemplate> Teams,
    IReadOnlyList<string> StartingOrderRiderIds,
    IReadOnlyList<RaceCommandTemplate> Commands,
    IReadOnlyList<RaceTacticalPlanTemplate> TacticalPlans);
