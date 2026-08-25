using System;

namespace Peloton.Simulation.Race;

public sealed record CapabilityResult(
    double RealizablePowerW,
    double EffectiveCriticalPowerW,
    double EffectivePeakPowerW,
    RiderPhysiologyState NextState);

public static class CapabilitySolver
{
    private const double LowIntensityReferenceWorkJ = 8_000_000.0;
    private const double HighIntensityReferenceWorkJ = 1_000_000.0;

    public static CapabilityResult Evaluate(
        RaceRiderProfile profile,
        RiderPhysiologyState state,
        double desiredPowerW,
        double durationSeconds)
    {
        ArgumentNullException.ThrowIfNull(profile);
        RequireNonNegative(state.WPrimeRemainingJ, nameof(state.WPrimeRemainingJ));
        RequireNonNegative(state.LowIntensityWorkJ, nameof(state.LowIntensityWorkJ));
        RequireNonNegative(state.HighIntensityWorkJ, nameof(state.HighIntensityWorkJ));
        RequireNonNegative(desiredPowerW, nameof(desiredPowerW));
        RequirePositive(durationSeconds, nameof(durationSeconds));

        double lowLoad = state.LowIntensityWorkJ / LowIntensityReferenceWorkJ;
        double highLoad = state.HighIntensityWorkJ / HighIntensityReferenceWorkJ;
        double criticalPowerLoss = Math.Min(
            RaceTuning.MaximumCriticalPowerLossFraction,
            (lowLoad * (1.0 - profile.LowIntensityDurability) * 0.12) +
            (highLoad * (1.0 - profile.HighIntensityDurability) * 0.35));
        double peakPowerLoss = Math.Min(
            RaceTuning.MaximumPeakPowerLossFraction,
            (lowLoad * (1.0 - profile.LowIntensityDurability) * 0.08) +
            (highLoad * (1.0 - profile.HighIntensityDurability) * 0.45));
        double effectiveCriticalPowerW = profile.CriticalPowerW * (1.0 - criticalPowerLoss);
        double effectivePeakPowerW = profile.PeakPowerW * (1.0 - peakPowerLoss);
        double wPrimeAccessFraction = Math.Clamp(
            1.0 - (highLoad * (1.0 - profile.HighIntensityDurability) * 0.35),
            0.45,
            1.0);
        double durationPowerLimitW = effectiveCriticalPowerW +
            ((state.WPrimeRemainingJ * wPrimeAccessFraction) / durationSeconds);
        double realizablePowerW = Math.Min(
            desiredPowerW,
            Math.Min(effectivePeakPowerW, durationPowerLimitW));

        double wPrimeRemainingJ = state.WPrimeRemainingJ;
        double lowIntensityWorkJ = state.LowIntensityWorkJ;
        double highIntensityWorkJ = state.HighIntensityWorkJ;
        if (realizablePowerW > effectiveCriticalPowerW)
        {
            double supraCriticalWorkJ =
                (realizablePowerW - effectiveCriticalPowerW) * durationSeconds;
            wPrimeRemainingJ = Math.Max(0.0, wPrimeRemainingJ - supraCriticalWorkJ);
            highIntensityWorkJ += supraCriticalWorkJ;
        }
        else
        {
            double recoveryFraction = effectiveCriticalPowerW <= 0.0
                ? 0.0
                : 1.0 - (realizablePowerW / effectiveCriticalPowerW);
            wPrimeRemainingJ = Math.Min(
                profile.WPrimeCapacityJ,
                wPrimeRemainingJ +
                (profile.WPrimeRecoveryJPerSecond * durationSeconds * recoveryFraction));
            lowIntensityWorkJ += realizablePowerW * durationSeconds;
        }

        RiderPhysiologyState nextState = new(
            wPrimeRemainingJ,
            lowIntensityWorkJ,
            highIntensityWorkJ);
        return new CapabilityResult(
            realizablePowerW,
            effectiveCriticalPowerW,
            effectivePeakPowerW,
            nextState);
    }

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
}
