using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public sealed record RacePreparationStrategy(
    WorldEntityId LeaderId,
    WorldEntityId SupportId,
    RaceObjective Objective,
    RaceBriefingKind BriefingKind);
