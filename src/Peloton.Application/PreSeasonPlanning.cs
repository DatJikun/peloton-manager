using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record PreSeasonRaceEntryProjection(
    string RaceContentId,
    int DayNumber,
    string Title,
    bool Entered,
    WorldEntityId? DesignatedLeaderId,
    string? DesignatedLeaderName);

public sealed record PreSeasonPlanningProjection(
    IReadOnlyList<PreSeasonRaceEntryProjection> Races);

internal sealed record PreSeasonPlanningDraft(
    Dictionary<string, bool> EntriesByRaceContentId,
    Dictionary<string, WorldEntityId?> LeadersByRaceContentId);
