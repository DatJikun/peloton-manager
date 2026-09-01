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
    private readonly List<RiderCareer> riderCareers;
    private readonly List<OrganizationRaceEntry> organizationRaceEntries;
    private readonly List<RiderContract> riderContracts;
    private readonly List<RiderCareer> ridersExpiredThisAdvance = new();

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
        IEnumerable<RiderCareer>? riderCareers = null,
        IEnumerable<OrganizationRaceEntry>? organizationRaceEntries = null,
        IEnumerable<RiderContract>? riderContracts = null,
        bool generatePeriodicRaces = true,
        int? financialYearDays = null)
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
        this.riderCareers = (riderCareers ?? Array.Empty<RiderCareer>())
            .OrderBy(career => career.Id.Value)
            .ToList();
        this.organizationRaceEntries = (organizationRaceEntries ?? Array.Empty<OrganizationRaceEntry>())
            .OrderBy(entry => entry.OrganizationId.Value)
            .ThenBy(entry => entry.RaceContentId, StringComparer.Ordinal)
            .ToList();
        this.riderContracts = (riderContracts ?? Array.Empty<RiderContract>())
            .OrderBy(contract => contract.Id.Value)
            .ToList();
        GeneratePeriodicRaces = generatePeriodicRaces;
        FinancialYearDays = financialYearDays ?? (generatePeriodicRaces ? calendarPeriodDays : 365);
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

    public IReadOnlyList<RiderCareer> RiderCareers => riderCareers;

    public IReadOnlyList<OrganizationRaceEntry> OrganizationRaceEntries => organizationRaceEntries;

    public IReadOnlyList<RiderContract> RiderContracts => riderContracts;

    public bool GeneratePeriodicRaces { get; }

    public int FinancialYearDays { get; }

    public RiderCareer? TryGetRiderCareer(WorldEntityId riderCareerId) =>
        riderCareers.FirstOrDefault(career => career.Id == riderCareerId);

    public RiderContract? TryGetActiveContract(WorldEntityId riderCareerId)
    {
        return riderContracts
            .Where(contract =>
                contract.RiderCareerId == riderCareerId &&
                contract.StartDate.DayNumber <= CurrentDate.DayNumber &&
                contract.EndDate.DayNumber >= CurrentDate.DayNumber)
            .OrderByDescending(contract => contract.StartDate.DayNumber)
            .ThenByDescending(contract => contract.Id.Value)
            .FirstOrDefault();
    }

    public bool TryTerminateActiveContract(WorldEntityId riderCareerId, WorldDate endDate)
    {
        RiderContract? active = TryGetActiveContract(riderCareerId);
        if (active is null)
        {
            return false;
        }

        int index = riderContracts.FindIndex(contract => contract.Id == active.Id);
        riderContracts[index] = active with { EndDate = endDate };
        return true;
    }

    public void AddRiderContract(RiderContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        riderContracts.Add(contract);
        riderContracts.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));
    }

    public IReadOnlyList<RiderCareer> GetRiderCareersForOrganization(WorldEntityId organizationId) =>
        riderCareers
            .Where(career => career.OrganizationId == organizationId)
            .OrderBy(career => career.OriginDefinitionId, StringComparer.Ordinal)
            .ToArray();

    public bool IsCalendarRaceDue =>
        CurrentDate.DayNumber > 0 &&
        calendarEntries.Any(entry =>
            entry.Kind == CalendarEntryKind.Race &&
            entry.DayNumber == CurrentDate.DayNumber &&
            LastCompletedRaceDay != CurrentDate.DayNumber);

    public bool IsRaceDue => IsCalendarRaceDue;

    public string? TryGetTodaysRaceContentId()
    {
        if (!IsCalendarRaceDue)
        {
            return null;
        }

        CalendarEntry? entry = calendarEntries.FirstOrDefault(
            item => item.DayNumber == CurrentDate.DayNumber && item.Kind == CalendarEntryKind.Race);
        return entry?.RaceContentId;
    }

    public bool IsOrganizationEnteredForRace(WorldEntityId organizationId, string raceContentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceContentId);
        OrganizationRaceEntry? entry = organizationRaceEntries.FirstOrDefault(
            item => item.OrganizationId == organizationId &&
                    string.Equals(item.RaceContentId, raceContentId, StringComparison.Ordinal));
        return entry?.Entered ?? false;
    }

    public bool IsRaceDueForOrganization(WorldEntityId organizationId)
    {
        return TryGetTodaysRaceContentId() is string raceContentId &&
               IsOrganizationEnteredForRace(organizationId, raceContentId);
    }

    public bool HasEnteredTeamsForTodaysRace()
    {
        if (TryGetTodaysRaceContentId() is not string raceContentId)
        {
            return false;
        }

        return organizationRaceEntries.Any(
            entry => string.Equals(entry.RaceContentId, raceContentId, StringComparison.Ordinal) && entry.Entered);
    }

    public void SetOrganizationRaceEntry(WorldEntityId organizationId, string raceContentId, bool entered)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceContentId);
        int index = organizationRaceEntries.FindIndex(
            entry => entry.OrganizationId == organizationId &&
                     string.Equals(entry.RaceContentId, raceContentId, StringComparison.Ordinal));
        if (index >= 0)
        {
            organizationRaceEntries[index] = new OrganizationRaceEntry(organizationId, raceContentId, entered);
            return;
        }

        organizationRaceEntries.Add(new OrganizationRaceEntry(organizationId, raceContentId, entered));
        organizationRaceEntries.Sort(CompareOrganizationRaceEntries);
    }

    public int NextRaceDayNumber
    {
        get
        {
            if (IsCalendarRaceDue)
            {
                return CurrentDate.DayNumber;
            }

            CalendarEntry? nextEntry = calendarEntries
                .Where(entry =>
                    entry.Kind == CalendarEntryKind.Race &&
                    entry.DayNumber >= CurrentDate.DayNumber &&
                    LastCompletedRaceDay < entry.DayNumber)
                .OrderBy(entry => entry.DayNumber)
                .ThenBy(entry => entry.Id.Value)
                .FirstOrDefault();
            return nextEntry?.DayNumber ?? CurrentDate.DayNumber;
        }
    }

    public int DaysUntilNextRace => NextRaceDayNumber - CurrentDate.DayNumber;

    public WorldEntityId AllocateEntityId() => entityIdAllocator.Allocate();

    public void AdvanceOneDay()
    {
        ridersExpiredThisAdvance.Clear();

        foreach (Organization organization in organizations)
        {
            organization.AdvanceOneDay();
        }

        foreach (RiderCareer career in riderCareers)
        {
            career.ApplyRestTick();
        }

        CurrentDate = CurrentDate.NextDay();
        ExpireContracts();
        ApplyFinanceTick();
    }

    private void ApplyFinanceTick()
    {
        foreach (Organization organization in organizations)
        {
            long activeWageBill = ComputeActiveWageBill(organization.Id);
            long dailySponsor = organization.TitleSponsorAnnualFeeEur / FinancialYearDays;
            long dailyWages = activeWageBill / FinancialYearDays;
            organization.ApplyFinanceTick(dailySponsor, dailyWages);
        }
    }

    private long ComputeActiveWageBill(WorldEntityId organizationId)
    {
        long wageBill = 0;
        foreach (RiderCareer career in riderCareers)
        {
            if (career.OrganizationId != organizationId)
            {
                continue;
            }

            RiderContract? contract = TryGetActiveContract(career.Id);
            if (contract is not null)
            {
                wageBill = checked(wageBill + contract.AnnualWage);
            }
        }

        return wageBill;
    }

    private void ExpireContracts()
    {
        foreach (RiderContract contract in riderContracts)
        {
            if (contract.EndDate.DayNumber >= CurrentDate.DayNumber)
            {
                continue;
            }

            RiderCareer? career = TryGetRiderCareer(contract.RiderCareerId);
            if (career is null || career.OrganizationId != contract.OrganizationId)
            {
                continue;
            }

            career.DetachFromClub();
            ridersExpiredThisAdvance.Add(career);
        }
    }

    public void RecordRace(RaceSummary result, string raceContentId, IReadOnlyList<WorldEntityId> starters)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(raceContentId);
        ArgumentNullException.ThrowIfNull(starters);

        LastRace = result;
        RaceCount = checked(RaceCount + 1);
        LastCompletedRaceDay = CurrentDate.DayNumber;

        Dictionary<WorldEntityId, int> placeByRider = new();
        for (int index = 0; index < result.FinishOrder.Count; index++)
        {
            placeByRider[result.FinishOrder[index]] = index + 1;
        }

        foreach (WorldEntityId riderId in starters.OrderBy(id => id.Value))
        {
            RiderCareer? career = TryGetRiderCareer(riderId);
            if (career is null)
            {
                throw new InvalidOperationException(
                    $"Official race result references unknown RiderCareer '{riderId.Value}'.");
            }

            bool didNotFinish = !placeByRider.TryGetValue(riderId, out int place);
            career.ApplyRaceLoad();
            career.AppendResult(new RiderCareerResult(
                raceContentId,
                CurrentDate.DayNumber,
                didNotFinish ? 0 : place,
                didNotFinish));
        }

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
                ResultAcknowledged: false,
                raceContentId);
        }

        if (GeneratePeriodicRaces)
        {
            EnsureUpcomingRaceEntry(raceContentId);
        }
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

    public void EnsureUpcomingRaceEntry(string? raceContentId = null)
    {
        if (!GeneratePeriodicRaces)
        {
            return;
        }

        int nextRaceDay = calendarEntries
            .Where(entry => entry.Kind == CalendarEntryKind.Race && entry.DayNumber > CurrentDate.DayNumber)
            .Select(entry => entry.DayNumber)
            .DefaultIfEmpty(((CurrentDate.DayNumber / CalendarPeriodDays) + 1) * CalendarPeriodDays)
            .Min();
        if (calendarEntries.Any(entry => entry.DayNumber == nextRaceDay))
        {
            return;
        }

        calendarEntries.Add(new CalendarEntry(
            AllocateEntityId(),
            nextRaceDay,
            CalendarEntryKind.Race,
            "Skeleton race",
            RaceContentId: raceContentId));
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

        if (access.CurrentOrganizationId is WorldEntityId organizationId &&
            IsRaceDueForOrganization(organizationId))
        {
            lastDayNotes.Add("A race is due today.");
        }

        foreach (RiderCareer career in ridersExpiredThisAdvance
                     .OrderBy(item => item.OriginDefinitionId, StringComparer.Ordinal))
        {
            Person? person = persons.FirstOrDefault(item => item.Id == career.PersonId);
            if (person is not null)
            {
                lastDayNotes.Add($"{person.Name}'s contract expired.");
            }
        }

        if (employer is not null && employer.CashEur < 0)
        {
            lastDayNotes.Add("The club is overdrawn.");
        }
    }

    private static int CompareOrganizationRaceEntries(OrganizationRaceEntry left, OrganizationRaceEntry right)
    {
        int organizationComparison = left.OrganizationId.Value.CompareTo(right.OrganizationId.Value);
        return organizationComparison != 0
            ? organizationComparison
            : string.Compare(left.RaceContentId, right.RaceContentId, StringComparison.Ordinal);
    }
}
