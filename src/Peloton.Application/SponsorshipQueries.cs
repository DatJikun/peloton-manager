using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record BoardObjectiveProjection(
    string Id,
    string Title,
    string KindLabel,
    int TargetValue,
    int CurrentValue,
    bool IsCompleted,
    long BonusRewardEur,
    int TrustImpactPercent,
    string ProgressDisplay);

public sealed record SponsorMarketOfferProjection(
    string SponsorName,
    string Tier,
    long ProposedFeeEur,
    int ContractYears,
    IReadOnlyList<string> ProposedGoals);

public sealed record SponsorOverviewProjection(
    string SponsorName,
    string Tier,
    long AnnualFeeEur,
    int EndSeasonYear,
    double Trust01,
    int TrustPercent,
    string TrustDescription,
    IReadOnlyList<BoardObjectiveProjection> Objectives,
    IReadOnlyList<SponsorMarketOfferProjection> MarketOffers);

public static class SponsorshipQueries
{
    public static SponsorOverviewProjection BuildOverview(WorldState world, AccessContext access)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (access.CurrentOrganizationId is not WorldEntityId orgId)
        {
            return EmptyOverview();
        }

        Organization? org = world.Organizations.FirstOrDefault(o => o.Id == orgId);
        if (org is null || org.SponsorAgreement is null)
        {
            return EmptyOverview();
        }

        SponsorAgreement agreement = org.SponsorAgreement;
        int trustPercent = (int)Math.Round(agreement.Trust01 * 100);
        string trustDesc = trustPercent switch
        {
            >= 85 => "Zarząd jest zachwycony wynikami",
            >= 70 => "Bardzo dobre relacje i stabilne wsparcie",
            >= 50 => "Umiarkowane zadowolenie, oczekiwanie postępów",
            >= 30 => "Niezadowolenie, groźba redukcji budżetu",
            _ => "Kryzys zaufania, ryzyko wycofania sponsora",
        };

        List<BoardObjectiveProjection> objectiveProjections = new();
        foreach (BoardObjective obj in agreement.Objectives)
        {
            string kindLabel = obj.Kind switch
            {
                BoardObjectiveKind.WinRace => "Zwycięstwo",
                BoardObjectiveKind.PodiumMonument => "Monument",
                BoardObjectiveKind.TopFinishGC => "Generalka",
                BoardObjectiveKind.SeasonWinsCount => "Sezon",
                BoardObjectiveKind.YouthDevelopment => "Młodzież",
                _ => "Cel",
            };
            string progress = obj.IsCompleted
                ? $"Zrealizowany ({obj.TargetValue}/{obj.TargetValue}) ✓"
                : $"{obj.CurrentValue} / {obj.TargetValue}";

            objectiveProjections.Add(new BoardObjectiveProjection(
                obj.Id,
                obj.Title,
                kindLabel,
                obj.TargetValue,
                obj.CurrentValue,
                obj.IsCompleted,
                obj.BonusRewardEur,
                obj.TrustImpactPercent,
                progress));
        }

        List<SponsorMarketOfferProjection> marketOffers =
        [
            new SponsorMarketOfferProjection(
                "AeroTech Dynamics",
                "Partner Techniczny",
                450_000,
                2,
                ["Top 5 w jeździe na czas", "1 wygrany etap płaski"]),
            new SponsorMarketOfferProjection(
                "VeloEnergy Nutrition",
                "Oficjalny Partner",
                300_000,
                1,
                ["Minimum 30 dni startowych kolarzy U23", "Aktywność w ucieczkach"]),
            new SponsorMarketOfferProjection(
                "Alpine Mineral Water",
                "Partner Wspierający",
                250_000,
                2,
                ["Podium w wyścigu górskim"]),
        ];

        return new SponsorOverviewProjection(
            agreement.SponsorName,
            agreement.Tier,
            agreement.AnnualFeeEur,
            agreement.EndSeasonYear,
            agreement.Trust01,
            trustPercent,
            trustDesc,
            objectiveProjections,
            marketOffers);
    }

    private static SponsorOverviewProjection EmptyOverview() => new(
        "Brak sponsora",
        "—",
        0,
        2026,
        0.50,
        50,
        "Brak danych",
        Array.Empty<BoardObjectiveProjection>(),
        Array.Empty<SponsorMarketOfferProjection>());
}
