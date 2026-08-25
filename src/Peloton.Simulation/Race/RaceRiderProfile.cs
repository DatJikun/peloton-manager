using System;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public sealed class RaceRiderProfile
{
    public RaceRiderProfile(
        WorldEntityId riderId,
        WorldEntityId organizationId,
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
        double tacticalAwareness)
    {
        RequirePositive(criticalPowerW, nameof(criticalPowerW));
        RequirePositive(wPrimeCapacityJ, nameof(wPrimeCapacityJ));
        RequirePositive(peakPowerW, nameof(peakPowerW));
        RequireNonNegative(wPrimeRecoveryJPerSecond, nameof(wPrimeRecoveryJPerSecond));
        RequireUnitInterval(lowIntensityDurability, nameof(lowIntensityDurability));
        RequireUnitInterval(highIntensityDurability, nameof(highIntensityDurability));
        RequirePositive(bodyMassKg, nameof(bodyMassKg));
        RequireNonNegative(systemMassKg, nameof(systemMassKg));
        RequirePositive(cdAM2, nameof(cdAM2));
        RequirePositive(baseCrr, nameof(baseCrr));
        RequireUnitInterval(positioning, nameof(positioning));
        RequireUnitInterval(handling, nameof(handling));
        RequireUnitInterval(tacticalAwareness, nameof(tacticalAwareness));
        if (peakPowerW < criticalPowerW)
        {
            throw new ArgumentOutOfRangeException(
                nameof(peakPowerW),
                "Peak power cannot be below critical power.");
        }

        RiderId = riderId;
        OrganizationId = organizationId;
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
    }

    public WorldEntityId RiderId { get; }

    public WorldEntityId OrganizationId { get; }

    public double CriticalPowerW { get; }

    public double WPrimeCapacityJ { get; }

    public double PeakPowerW { get; }

    public double WPrimeRecoveryJPerSecond { get; }

    public double LowIntensityDurability { get; }

    public double HighIntensityDurability { get; }

    public double BodyMassKg { get; }

    public double SystemMassKg { get; }

    public double TotalMassKg => BodyMassKg + SystemMassKg;

    public double CdAM2 { get; }

    public double BaseCrr { get; }

    public double Positioning { get; }

    public double Handling { get; }

    public double TacticalAwareness { get; }

    private static void RequirePositive(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public readonly record struct RiderPhysiologyState(
    double WPrimeRemainingJ,
    double LowIntensityWorkJ,
    double HighIntensityWorkJ)
{
    public static RiderPhysiologyState Fresh(RaceRiderProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new RiderPhysiologyState(profile.WPrimeCapacityJ, 0.0, 0.0);
    }
}
