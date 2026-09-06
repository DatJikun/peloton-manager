using System;
using System.Collections.Generic;

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
        long estimatedBudgetEur = 0,
        long cashEur = 0,
        long titleSponsorAnnualFeeEur = 0,
        SponsorAgreement? sponsorAgreement = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(daysSimulated);
        ArgumentOutOfRangeException.ThrowIfNegative(licenceYearsRemaining);
        ArgumentOutOfRangeException.ThrowIfNegative(titleSponsorAnnualFeeEur);

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
        CashEur = cashEur;
        TitleSponsorAnnualFeeEur = titleSponsorAnnualFeeEur;
        SponsorAgreement = sponsorAgreement ?? CreateDefaultSponsorship(name, titleSponsor, titleSponsorAnnualFeeEur);
    }

    public WorldEntityId Id { get; }

    public string OriginDefinitionId { get; }

    public string Name { get; }

    public int DaysSimulated { get; private set; }

    public string Country { get; }

    /// <summary>
    /// Content label (WorldTour / ProTeam / Continental / …), not a C# enum.
    /// A future UCI licence module changes this string on the same OrganizationId.
    /// </summary>
    public string Division { get; }

    /// <summary>
    /// Years left on the current licence. WT 2026–2028 is a 3-year cycle in content;
    /// living promotion/relegation does not tick this yet.
    /// </summary>
    public int LicenceYearsRemaining { get; }

    public string TitleSponsor { get; }

    public string Bike { get; }

    public string Groupset { get; }

    public long EstimatedBudgetEur { get; }

    public long CashEur { get; private set; }

    public long TitleSponsorAnnualFeeEur { get; }

    public SponsorAgreement SponsorAgreement { get; private set; }

    public OrganizationScoutingStore ScoutingStore { get; } = new();

    public void AdvanceOneDay()
    {
        DaysSimulated = checked(DaysSimulated + 1);
        ScoutingStore.AdvanceOneDay();
    }

    public void ApplyFinanceTick(long dailySponsor, long dailyWages)
    {
        CashEur = checked(CashEur + dailySponsor - dailyWages);
    }

    public void SetSponsorAgreement(SponsorAgreement agreement)
    {
        ArgumentNullException.ThrowIfNull(agreement);
        SponsorAgreement = agreement;
    }

    public void AddCash(long amountEur)
    {
        CashEur = checked(CashEur + amountEur);
    }

    private static SponsorAgreement CreateDefaultSponsorship(string teamName, string sponsorName, long fee)
    {
        string actualSponsor = string.IsNullOrWhiteSpace(sponsorName) ? $"{teamName} Partner" : sponsorName;
        long annualFee = fee > 0 ? fee : 4_500_000;
        List<BoardObjective> objectives =
        [
            new BoardObjective("obj.wins", "Zwycięstwa w wyścigach sezonu", BoardObjectiveKind.SeasonWinsCount, 3, bonusRewardEur: 150_000, trustImpactPercent: 20),
            new BoardObjective("obj.monument", "Podium w prestiżowym wyścigu klasycznym", BoardObjectiveKind.PodiumMonument, 1, bonusRewardEur: 250_000, trustImpactPercent: 25),
            new BoardObjective("obj.youth", "Ukończenie wyścigów przez młodzieżowców U23", BoardObjectiveKind.YouthDevelopment, 4, bonusRewardEur: 100_000, trustImpactPercent: 15),
        ];
        return new SponsorAgreement(actualSponsor, "Sponsor Tytularny", annualFee, 2027, 0.75, objectives);
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
    bool Entered,
    WorldEntityId? DesignatedLeaderId = null);

public readonly record struct AccessContext(
    WorldEntityId? ViewerPersonId,
    WorldEntityId? CurrentOrganizationId,
    WorldEntityId? DecisionAuthorityId,
    string PermissionScope);
