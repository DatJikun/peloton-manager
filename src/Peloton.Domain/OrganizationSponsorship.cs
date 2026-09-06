using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Domain;

public enum BoardObjectiveKind
{
    WinRace,
    PodiumMonument,
    TopFinishGC,
    SeasonWinsCount,
    YouthDevelopment,
}

public sealed class BoardObjective
{
    public BoardObjective(
        string id,
        string title,
        BoardObjectiveKind kind,
        int targetValue,
        int currentValue = 0,
        bool isCompleted = false,
        bool isFailed = false,
        long bonusRewardEur = 100_000,
        int trustImpactPercent = 15,
        string? targetRaceContentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetValue);
        ArgumentOutOfRangeException.ThrowIfNegative(currentValue);
        ArgumentOutOfRangeException.ThrowIfNegative(bonusRewardEur);

        Id = id;
        Title = title;
        Kind = kind;
        TargetValue = targetValue;
        CurrentValue = currentValue;
        IsCompleted = isCompleted;
        IsFailed = isFailed;
        BonusRewardEur = bonusRewardEur;
        TrustImpactPercent = trustImpactPercent;
        TargetRaceContentId = targetRaceContentId;
    }

    public string Id { get; }
    public string Title { get; }
    public BoardObjectiveKind Kind { get; }
    public int TargetValue { get; }
    public int CurrentValue { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsFailed { get; private set; }
    public long BonusRewardEur { get; }
    public int TrustImpactPercent { get; }
    public string? TargetRaceContentId { get; }

    public bool AddProgress(int amount)
    {
        if (IsCompleted || IsFailed)
        {
            return false;
        }

        CurrentValue = Math.Min(TargetValue, CurrentValue + amount);
        if (CurrentValue >= TargetValue)
        {
            IsCompleted = true;
            return true;
        }

        return false;
    }

    public bool MarkCompleted()
    {
        if (IsCompleted || IsFailed)
        {
            return false;
        }

        CurrentValue = TargetValue;
        IsCompleted = true;
        return true;
    }

    public void MarkFailed()
    {
        IsFailed = true;
    }
}

public sealed class SponsorAgreement
{
    public SponsorAgreement(
        string sponsorName,
        string tier,
        long annualFeeEur,
        int endSeasonYear,
        double trust01 = 0.75,
        IEnumerable<BoardObjective>? objectives = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sponsorName);
        ArgumentOutOfRangeException.ThrowIfNegative(annualFeeEur);
        if (trust01 is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(trust01));
        }

        SponsorName = sponsorName;
        Tier = tier;
        AnnualFeeEur = annualFeeEur;
        EndSeasonYear = endSeasonYear;
        Trust01 = trust01;
        Objectives = (objectives ?? Array.Empty<BoardObjective>()).ToList();
    }

    public string SponsorName { get; }
    public string Tier { get; }
    public long AnnualFeeEur { get; private set; }
    public int EndSeasonYear { get; private set; }
    public double Trust01 { get; private set; }
    public IReadOnlyList<BoardObjective> Objectives { get; }

    public void AdjustTrust(double delta)
    {
        Trust01 = Math.Clamp(Trust01 + delta, 0.0, 1.0);
    }

    public void ExtendContract(int additionalYears, double feeMultiplier = 1.15)
    {
        EndSeasonYear += additionalYears;
        AnnualFeeEur = (long)Math.Round(AnnualFeeEur * feeMultiplier);
        Trust01 = Math.Clamp(Trust01 + 0.10, 0.0, 1.0);
    }
}
