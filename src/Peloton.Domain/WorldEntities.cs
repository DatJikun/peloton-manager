using System;

namespace Peloton.Domain;

public sealed record Person(
    WorldEntityId Id,
    string Name,
    string? OriginDefinitionId = null,
    string? Nationality = null,
    int? BirthYear = null);

public sealed record ManagerCareer(
    WorldEntityId Id,
    WorldEntityId PersonId,
    WorldEntityId? ActiveEmploymentId);

public sealed record Employment(
    WorldEntityId Id,
    WorldEntityId ManagerCareerId,
    WorldEntityId OrganizationId,
    WorldDate StartDate,
    WorldDate? EndDate);

public sealed class Organization
{
    public Organization(
        WorldEntityId id,
        string originDefinitionId,
        string name,
        int daysSimulated = 0,
        string country = "",
        string division = "Skeleton",
        int licenceYearsRemaining = 0,
        string titleSponsor = "",
        string bike = "",
        string groupset = "",
        long estimatedBudgetEur = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(daysSimulated);
        ArgumentOutOfRangeException.ThrowIfNegative(licenceYearsRemaining);

        Id = id;
        OriginDefinitionId = originDefinitionId;
        Name = name;
        DaysSimulated = daysSimulated;
        Country = country;
        Division = division;
        LicenceYearsRemaining = licenceYearsRemaining;
        TitleSponsor = titleSponsor;
        Bike = bike;
        Groupset = groupset;
        EstimatedBudgetEur = estimatedBudgetEur;
    }

    public WorldEntityId Id { get; }

    public string OriginDefinitionId { get; }

    public string Name { get; }

    public int DaysSimulated { get; private set; }

    public string Country { get; }

    public string Division { get; }

    public int LicenceYearsRemaining { get; }

    public string TitleSponsor { get; }

    public string Bike { get; }

    public string Groupset { get; }

    public long EstimatedBudgetEur { get; }

    public void AdvanceOneDay()
    {
        DaysSimulated = checked(DaysSimulated + 1);
    }
}

public enum DecisionAuthorityKind
{
    HumanInput,
    AIInput,
}

public sealed record DecisionAuthority(WorldEntityId Id, DecisionAuthorityKind Kind);

public sealed record OrganizationRaceEntry(
    WorldEntityId OrganizationId,
    string RaceContentId,
    bool Entered);

public readonly record struct AccessContext(
    WorldEntityId? ViewerPersonId,
    WorldEntityId? CurrentOrganizationId,
    WorldEntityId? DecisionAuthorityId,
    string PermissionScope);
