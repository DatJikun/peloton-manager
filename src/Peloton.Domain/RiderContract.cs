namespace Peloton.Domain;

public sealed record RiderContract(
    WorldEntityId Id,
    WorldEntityId RiderCareerId,
    WorldEntityId OrganizationId,
    int AnnualWage,
    WorldDate StartDate,
    WorldDate EndDate);
