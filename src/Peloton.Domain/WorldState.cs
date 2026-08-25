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

public sealed record StubRaceSummary(
    string RouteId,
    WorldEntityId WinnerId,
    IReadOnlyList<WorldEntityId> FinishOrder);

public sealed class WorldState
{
    private readonly WorldEntityIdAllocator entityIdAllocator;
    private readonly List<Person> persons;
    private readonly List<ManagerCareer> managerCareers;
    private readonly List<Employment> employments;
    private readonly List<Organization> organizations;
    private readonly List<DecisionAuthority> decisionAuthorities;
    private readonly List<RulesModuleIdentity> rulesModules;

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
        StubRaceSummary? lastRace = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worldId);
        ArgumentNullException.ThrowIfNull(contentIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesIdentity);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rngContractVersion);
        ArgumentOutOfRangeException.ThrowIfNegative(raceCount);

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

    public StubRaceSummary? LastRace { get; private set; }

    public WorldEntityId AllocateEntityId() => entityIdAllocator.Allocate();

    public void AdvanceOneDay()
    {
        foreach (Organization organization in organizations)
        {
            organization.AdvanceOneDay();
        }

        CurrentDate = CurrentDate.NextDay();
    }

    public void RecordStubRace(StubRaceSummary result)
    {
        ArgumentNullException.ThrowIfNull(result);
        LastRace = result;
        RaceCount = checked(RaceCount + 1);
    }
}
