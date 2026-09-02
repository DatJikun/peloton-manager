using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Domain;

public sealed record RiderCareerResult(
    string RaceContentId,
    int DayNumber,
    int Place,
    bool DidNotFinish);

public sealed class RiderCareer
{
    public RiderCareer(
        WorldEntityId id,
        WorldEntityId personId,
        WorldEntityId? organizationId,
        string originDefinitionId,
        double criticalPowerW,
        double wPrimeCapacityJ,
        double peakPowerW,
        double wPrimeRecoveryJPerSecond,
        double lowIntensityDurability,
        double highIntensityDurability,
        double bodyMassKg,
        double systemMassKg,
        double cdAM2,
        double baseCrr,
        double positioning,
        double handling,
        double tacticalAwareness,
        double form01 = 1.0,
        double freshness01 = 1.0,
        double fatigue01 = 0.0,
        double loyalty01 = 0.5,
        int potentialOvr = 70,
        IEnumerable<RiderCareerResult>? results = null,
        double? cdATtM2 = null,
        bool isRetired = false,
        WorldEntityId? retiredFromOrganizationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originDefinitionId);
        RequireUnitInterval(form01, nameof(form01));
        RequireUnitInterval(freshness01, nameof(freshness01));
        RequireUnitInterval(fatigue01, nameof(fatigue01));
        RequireUnitInterval(loyalty01, nameof(loyalty01));
        if (potentialOvr < 1 || potentialOvr > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(potentialOvr));
        }

        Id = id;
        PersonId = personId;
        OrganizationId = organizationId;
        OriginDefinitionId = originDefinitionId;
        CriticalPowerW = criticalPowerW;
        WPrimeCapacityJ = wPrimeCapacityJ;
        PeakPowerW = peakPowerW;
        WPrimeRecoveryJPerSecond = wPrimeRecoveryJPerSecond;
        LowIntensityDurability = lowIntensityDurability;
        HighIntensityDurability = highIntensityDurability;
        BodyMassKg = bodyMassKg;
        SystemMassKg = systemMassKg;
        RequirePositive(cdAM2, nameof(cdAM2));
        double timeTrialCdA = cdATtM2 ?? cdAM2;
        RequirePositive(timeTrialCdA, nameof(cdATtM2));
        CdARoadM2 = cdAM2;
        CdATtM2 = timeTrialCdA;
        BaseCrr = baseCrr;
        Positioning = positioning;
        Handling = handling;
        TacticalAwareness = tacticalAwareness;
        Form01 = form01;
        Freshness01 = freshness01;
        Fatigue01 = fatigue01;
        Loyalty01 = loyalty01;
        PotentialOvr = potentialOvr;
        IsRetired = isRetired;
        RetiredFromOrganizationId = retiredFromOrganizationId;
        this.results = (results ?? Array.Empty<RiderCareerResult>()).ToList();
    }

    private readonly List<RiderCareerResult> results;

    public WorldEntityId Id { get; }

    public WorldEntityId PersonId { get; }

    public WorldEntityId? OrganizationId { get; private set; }

    public bool IsRetired { get; private set; }

    public WorldEntityId? RetiredFromOrganizationId { get; private set; }

    public void Retire()
    {
        if (IsRetired)
        {
            return;
        }

        IsRetired = true;
        RetiredFromOrganizationId = OrganizationId;
        DetachFromClub();
    }

    public void DetachFromClub() => OrganizationId = null;

    public void AttachToClub(WorldEntityId organizationId) => OrganizationId = organizationId;

    public string OriginDefinitionId { get; }

    public double CriticalPowerW { get; private set; }

    public double WPrimeCapacityJ { get; private set; }

    public double PeakPowerW { get; private set; }

    public double WPrimeRecoveryJPerSecond { get; }

    public double LowIntensityDurability { get; private set; }

    public double HighIntensityDurability { get; private set; }

    public double BodyMassKg { get; }

    public double SystemMassKg { get; }

    public double CdARoadM2 { get; }

    public double CdATtM2 { get; }

    public double CdAM2 => CdARoadM2;

    public double BaseCrr { get; }

    public double Positioning { get; private set; }

    public double Handling { get; private set; }

    public double TacticalAwareness { get; private set; }

    public double Form01 { get; private set; }

    public double Freshness01 { get; private set; }

    public double Fatigue01 { get; private set; }

    public double Loyalty01 { get; }

    public int PotentialOvr { get; private set; }

    public void EnsurePotentialOvrAtLeast(int minimum) =>
        PotentialOvr = Math.Clamp(Math.Max(PotentialOvr, minimum), 1, 99);

    public IReadOnlyList<RiderCareerResult> Results => results;

    public void AppendResult(RiderCareerResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        results.Add(result);
    }

    public void ApplyRestTick()
    {
        Fatigue01 = Clamp01(Fatigue01 * 0.82);
        Freshness01 = Clamp01(Freshness01 + (0.12 * (1.0 - Freshness01)));
        Form01 = Clamp01(Form01 + (0.05 * (0.90 - Form01)));
    }

    public void ApplyWinterFormReset()
    {
        Form01 = 1.0;
        Freshness01 = 1.0;
        Fatigue01 = 0.0;
    }

    public void ApplyAgingPhysiology(
        double criticalPowerW,
        double wPrimeCapacityJ,
        double peakPowerW,
        double lowIntensityDurability,
        double highIntensityDurability,
        double positioning,
        double handling,
        double tacticalAwareness)
    {
        RequirePositive(criticalPowerW, nameof(criticalPowerW));
        RequirePositive(wPrimeCapacityJ, nameof(wPrimeCapacityJ));
        RequirePositive(peakPowerW, nameof(peakPowerW));
        RequireUnitInterval(lowIntensityDurability, nameof(lowIntensityDurability));
        RequireUnitInterval(highIntensityDurability, nameof(highIntensityDurability));
        RequireUnitInterval(positioning, nameof(positioning));
        RequireUnitInterval(handling, nameof(handling));
        RequireUnitInterval(tacticalAwareness, nameof(tacticalAwareness));
        CriticalPowerW = criticalPowerW;
        WPrimeCapacityJ = wPrimeCapacityJ;
        PeakPowerW = peakPowerW;
        LowIntensityDurability = lowIntensityDurability;
        HighIntensityDurability = highIntensityDurability;
        Positioning = positioning;
        Handling = handling;
        TacticalAwareness = tacticalAwareness;
    }

    public void ApplyRaceLoad()
    {
        Fatigue01 = Clamp01(Fatigue01 + 0.30);
        Freshness01 = Clamp01(Freshness01 - 0.25);
        Form01 = Clamp01(Form01 - 0.08);
    }

    public double ComputeReadiness() =>
        (0.70 + (0.30 * Form01)) * (0.85 + (0.15 * Freshness01)) * (1.0 - (0.25 * Fatigue01));

    private static double Clamp01(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        return Math.Clamp(value, 0.0, 1.0);
    }

    private static void RequireUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequirePositive(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
