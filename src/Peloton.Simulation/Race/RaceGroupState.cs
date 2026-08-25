using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public sealed record RaceGroupState(
    int GroupId,
    IReadOnlyList<WorldEntityId> OrderedRiderIds);

public sealed record GroupResolution(
    int ShelterCapacity,
    IReadOnlyList<ResolvedRaceRiderPosition> Riders,
    IReadOnlyList<RaceGroupState> Groups);
