using System.Collections.Generic;

namespace Peloton.Domain;

public sealed record RaceIdentityConstraints(
    string RaceContentId,
    string Kind,
    int RacingStageCount,
    int IttMin,
    int IttMax,
    int TttMin,
    int TttMax,
    int MountainMin,
    int MountainMax,
    int HillyMin,
    int HillyMax,
    int FlatMin,
    int FlatMax,
    int SummitFinishMin,
    int SummitFinishMax,
    int TotalKmMin,
    int TotalKmMax,
    int CobbleKmMin,
    int CobbleKmMax,
    IReadOnlyList<string> TerrainPalette);

public sealed record CalendarRaceDetail(
    string Id,
    string Name,
    string Country,
    string Kind,
    int StartDayNumber,
    int EndDayNumber);
