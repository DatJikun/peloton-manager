using System.Collections.Generic;

namespace Peloton.Application;

public sealed record CareerDayProjection(
    int DayNumber,
    string ManagerName,
    string? EmployerName,
    int DaysUntilNextRace,
    int NextRaceDayNumber,
    bool RaceDueToday,
    IReadOnlyList<string> TodayNotes,
    int RaceCount);
