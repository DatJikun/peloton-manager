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
        WorldEntityId organizationId,
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
        IEnumerable<RiderCareerResult>? results = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originDefinitionId);
        RequireUnitInterval(form01, nameof(form01));
        RequireUnitInterval(freshness01, nameof(freshness01));
        RequireUnitInterval(fatigue01, nameof(fatigue01));
        RequireUnitInterval(loyalty01, nameof(loyalty01));

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
        CdAM2 = cdAM2;
        BaseCrr = baseCrr;
        Positioning = positioning;
        Handling = handling;
        TacticalAwareness = tacticalAwareness;
        Form01 = form01;
        Freshness01 = freshness01;
        Fatigue01 = fatigue01;
        Loyalty01 = loyalty01;
        this.results = (results ?? Array.Empty<RiderCareerResult>()).ToList();
    }

    private readonly List<RiderCareerResult> results;

    public WorldEntityId Id { get; }

    public WorldEntityId PersonId { get; }

    public WorldEntityId OrganizationId { get; }

    public string OriginDefinitionId { get; }

    public double CriticalPowerW { get; }

    public double WPrimeCapacityJ { get; }

    public double PeakPowerW { get; }

    public double WPrimeRecoveryJPerSecond { get; }

    public double LowIntensityDurability { get; }

    public double HighIntensityDurability { get; }

    public double BodyMassKg { get; }

    public double SystemMassKg { get; }

    public double CdAM2 { get; }

    public double BaseCrr { get; }

    public double Positioning { get; }

    public double Handling { get; }

    public double TacticalAwareness { get; }

    public double Form01 { get; }

    public double Freshness01 { get; }

    public double Fatigue01 { get; }

    public double Loyalty01 { get; }

    public IReadOnlyList<RiderCareerResult> Results => results;

    public void AppendResult(RiderCareerResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        results.Add(result);
    }

    private static void RequireUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
