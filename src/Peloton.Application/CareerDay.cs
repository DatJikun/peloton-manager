using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public static class HubPrimaryActionIds
{
    public const string AdvanceDay = "advance-day";
    public const string RaceNext = "race-next";
}

public static class HubPrimaryActionLabels
{
    public const string AdvanceDay = "Advance Day";
    public const string RaceNext = "Race next";
}

public sealed record CareerDayProjection(
    int DayNumber,
    string ManagerName,
    string? EmployerName,
    int DaysUntilNextRace,
    int NextRaceDayNumber,
    bool RaceDueToday,
    IReadOnlyList<string> TodayNotes,
    int RaceCount,
    string PrimaryAction,
    string PrimaryLabel);

public sealed record PersonNameProjection(WorldEntityId Id, string Name);

public sealed record OrganizationNameProjection(WorldEntityId Id, string Name);
