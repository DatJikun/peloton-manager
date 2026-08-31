using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Domain;

public sealed record ContentIdentity(
    string PackId,
    string PackVersion,
    int ContentSchemaVersion,
    string ScenarioId,
    string HistoryMode,
    string Difficulty,
    string AttributeVisibility,
    string AggregateHash);

public sealed record RulesModuleIdentity(
    string Slot,
    string Id,
    string Contract,
    int ContractVersion,
    string ParameterIdentity);

public sealed record RaceSummary(
    string RouteId,
    WorldEntityId WinnerId,
    IReadOnlyList<WorldEntityId> FinishOrder);

public sealed class WorldState
{
    private readonly List<string> lastDayNotes;
    private readonly WorldEntityIdAllocator entityIdAllocator;
    private readonly List<Person> persons;
    private readonly List<ManagerCareer> managerCareers;
    private readonly List<Employment> employments;
    private readonly List<Organization> organizations;
    private readonly List<DecisionAuthority> decisionAuthorities;
    private readonly List<RulesModuleIdentity> rulesModules;
    private readonly List<CalendarEntry> calendarEntries;
    private readonly List<RosterRider> rosterRiders;

    public WorldState(
        string worldId,
        long masterSeed,
        int rngContractVersion,
        WorldDate currentDate,
        ContentIdentity contentIdentity,
        string rulesIdentity,
        IEnumerable<RulesModuleIdentity> rulesModules,
        long entityIdHighWaterMark,
        IEnumerable<Person> persons,
        IEnumerable<ManagerCareer> managerCareers,
        IEnumerable<Employment> employments,
        IEnumerable<Organization> organizations,
        IEnumerable<DecisionAuthority> decisionAuthorities,
        int raceCount = 0,
        RaceSummary? lastRace = null,
        int calendarPeriodDays = 12,
        int lastCompletedRaceDay = 0,
        IEnumerable<string>? lastDayNotes = null,
        IEnumerable<CalendarEntry>? calendarEntries = null,
        IEnumerable<RosterRider>? rosterRiders = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worldId);
        ArgumentNullException.ThrowIfNull(contentIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesIdentity);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rngContractVersion);
        ArgumentOutOfRangeException.ThrowIfNegative(raceCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(calendarPeriodDays);
        ArgumentOutOfRangeException.ThrowIfNegative(lastCompletedRaceDay);

        WorldId = worldId;
        MasterSeed = masterSeed;
        RngContractVersion = rngContractVersion;
        CurrentDate = currentDate;
        ContentIdentity = contentIdentity;
        RulesIdentity = rulesIdentity;
        this.rulesModules = rulesModules.OrderBy(module => module.Slot, StringComparer.Ordinal).ToList();
        entityIdAllocator = new WorldEntityIdAllocator(entityIdHighWaterMark);
        this.persons = persons.OrderBy(person => person.Id.Value).ToList();
        this.managerCareers = managerCareers.OrderBy(career => career.Id.Value).ToList();
        this.employments = employments.OrderBy(employment => employment.Id.Value).ToList();
        this.organizations = organizations.OrderBy(organization => organization.Id.Value).ToList();
        this.decisionAuthorities = decisionAuthorities.OrderBy(authority => authority.Id.Value).ToList();
        RaceCount = raceCount;
        LastRace = lastRace;
        CalendarPeriodDays = calendarPeriodDays;
        LastCompletedRaceDay = lastCompletedRaceDay;
        this.lastDayNotes = (lastDayNotes ?? Array.Empty<string>()).ToList();
        this.calendarEntries = SortCalendarEntries(calendarEntries ?? Array.Empty<CalendarEntry>());
        this.rosterRiders = (rosterRiders ?? Array.Empty<RosterRider>())
            .OrderBy(rider => rider.PersonId.Value)
            .ToList();
    }

    public string WorldId { get; }

    public long MasterSeed { get; }

    public int RngContractVersion { get; }

    public WorldDate CurrentDate { get; private set; }

    public ContentIdentity ContentIdentity { get; }

    public string RulesIdentity { get; }

    public IReadOnlyList<RulesModuleIdentity> RulesModules => rulesModules;

    public long EntityIdHighWaterMark => entityIdAllocator.HighWaterMark;

    public IReadOnlyList<Person> Persons => persons;

    public IReadOnlyList<ManagerCareer> ManagerCareers => managerCareers;

    public IReadOnlyList<Employment> Employments => employments;

    public IReadOnlyList<Organization> Organizations => organizations;

    public IReadOnlyList<DecisionAuthority> DecisionAuthorities => decisionAuthorities;

    public int RaceCount { get; private set; }

    public RaceSummary? LastRace { get; private set; }

    public int CalendarPeriodDays { get; }

    public int LastCompletedRaceDay { get; private set; }

    public IReadOnlyList<string> LastDayNotes => lastDayNotes;

    public IReadOnlyList<CalendarEntry> CalendarEntries => calendarEntries;

    public IReadOnlyList<RosterRider> RosterRiders => rosterRiders;

    public bool IsRaceDue =>
        CurrentDate.DayNumber > 0 &&
        CurrentDate.DayNumber % CalendarPeriodDays == 0 &&
        LastCompletedRaceDay != CurrentDate.DayNumber;

    public int NextRaceDayNumber
    {
        get
        {
            if (IsRaceDue)
            {
                return CurrentDate.DayNumber;
            }

            return ((CurrentDate.DayNumber / CalendarPeriodDays) + 1) * CalendarPeriodDays;
        }
    }

    public int DaysUntilNextRace => NextRaceDayNumber - CurrentDate.DayNumber;

    public WorldEntityId AllocateEntityId() => entityIdAllocator.Allocate();

    public void AdvanceOneDay()
    {
        foreach (Organization organization in organizations)
        {
            organization.AdvanceOneDay();
        }

        CurrentDate = CurrentDate.NextDay();
    }

    public void RecordRace(RaceSummary result)
    {
        ArgumentNullException.ThrowIfNull(result);
        LastRace = result;
        RaceCount = checked(RaceCount + 1);
        LastCompletedRaceDay = CurrentDate.DayNumber;

        string officialResult = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"Winner {result.WinnerId.Value}");
        int entryIndex = calendarEntries.FindIndex(entry =>
            entry.DayNumber == CurrentDate.DayNumber && entry.Kind == CalendarEntryKind.Race);
        if (entryIndex >= 0)
        {
            CalendarEntry existing = calendarEntries[entryIndex];
            calendarEntries[entryIndex] = new CalendarEntry(
                existing.Id,
                existing.DayNumber,
                existing.Kind,
                existing.Title,
                officialResult,
                ResultAcknowledged: false);
        }

        EnsureUpcomingRaceEntry();
    }

    public bool AcknowledgeRaceResult(WorldEntityId entryId)
    {
        int entryIndex = calendarEntries.FindIndex(entry => entry.Id == entryId);
        if (entryIndex < 0)
        {
            return false;
        }

        CalendarEntry existing = calendarEntries[entryIndex];
        if (existing.OfficialResult is null || existing.ResultAcknowledged)
        {
            return false;
        }

        calendarEntries[entryIndex] = existing with { ResultAcknowledged = true };
        return true;
    }

    public void EnsureUpcomingRaceEntry()
    {
        int nextRaceDay = NextRaceDayNumber;
        if (calendarEntries.Any(entry => entry.DayNumber == nextRaceDay))
        {
            return;
        }

        calendarEntries.Add(new CalendarEntry(
            AllocateEntityId(),
            nextRaceDay,
            CalendarEntryKind.Race,
            "Skeleton race"));
        calendarEntries.Sort(CompareCalendarEntries);
    }

    private static List<CalendarEntry> SortCalendarEntries(IEnumerable<CalendarEntry> entries)
    {
        return entries.OrderBy(entry => entry.DayNumber).ThenBy(entry => entry.Id.Value).ToList();
    }

    private static int CompareCalendarEntries(CalendarEntry left, CalendarEntry right)
    {
        int dayComparison = left.DayNumber.CompareTo(right.DayNumber);
        return dayComparison != 0 ? dayComparison : left.Id.Value.CompareTo(right.Id.Value);
    }

    public void CaptureDayNotes(AccessContext access)
    {
        lastDayNotes.Clear();
        Organization? employer = access.CurrentOrganizationId is WorldEntityId orgId
            ? organizations.FirstOrDefault(organization => organization.Id == orgId)
            : null;
        if (employer is null)
        {
            lastDayNotes.Add("You are unemployed.");
        }
        else
        {
            lastDayNotes.Add($"Your organization {employer.Name} worked the day.");
        }

        if (organizations.Count > 1)
        {
            lastDayNotes.Add("The rest of the world advanced.");
        }

        if (IsRaceDue)
        {
            lastDayNotes.Add("A race is due today.");
        }
    }
}
