namespace Peloton.Domain;

public enum CalendarEntryKind
{
    Race,
}

public sealed record CalendarEntry(
    WorldEntityId Id,
    int DayNumber,
    CalendarEntryKind Kind,
    string Title);
