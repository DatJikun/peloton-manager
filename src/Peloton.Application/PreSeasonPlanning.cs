using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record PreSeasonRaceEntryProjection(
    string RaceContentId,
    int DayNumber,
    string Title,
    bool Entered);

public sealed record PreSeasonPlanningProjection(
    IReadOnlyList<PreSeasonRaceEntryProjection> Races);

internal sealed record PreSeasonPlanningDraft(
    Dictionary<string, bool> EntriesByRaceContentId);
