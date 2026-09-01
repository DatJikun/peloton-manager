namespace Peloton.Domain;

public enum CalendarEntryKind
{
    Race,
}

public sealed record CalendarEntry(
    WorldEntityId Id,
    int DayNumber,
    CalendarEntryKind Kind,
    string Title,
    string? OfficialResult = null,
    bool ResultAcknowledged = false,
    string? RaceContentId = null,
    int StageIndex = 1,
    WorldEntityId? CourseProfileId = null);
