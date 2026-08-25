using System;

namespace Peloton.Domain;

public sealed record Person(WorldEntityId Id, string Name);

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
        int daysSimulated = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(daysSimulated);

        Id = id;
        OriginDefinitionId = originDefinitionId;
        Name = name;
        DaysSimulated = daysSimulated;
    }

    public WorldEntityId Id { get; }

    public string OriginDefinitionId { get; }

    public string Name { get; }

    public int DaysSimulated { get; private set; }

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

public readonly record struct AccessContext(
    WorldEntityId? ViewerPersonId,
    WorldEntityId? CurrentOrganizationId,
    WorldEntityId? DecisionAuthorityId,
    string PermissionScope);
