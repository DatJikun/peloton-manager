using System;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record RiderRatingSet(
    int Climb,
    int Hills,
    int Flat,
    int TimeTrial,
    int Sprint,
    int Cobbles,
    int Ovr,
    int PotentialOvr);

public static class RiderRatingQueries
{
    public static RiderRatingSet FromPhysiology(RiderCareer career, int potentialOvr) =>
        FromPhysiology(
            career.CriticalPowerW,
            career.WPrimeCapacityJ,
            career.PeakPowerW,
            career.LowIntensityDurability,
            career.HighIntensityDurability,
            career.BodyMassKg,
            career.CdARoadM2,
            career.BaseCrr,
            career.Positioning,
            career.Handling,
            potentialOvr,
            career.CdATtM2);

    public static RiderRatingSet FromPhysiology(
        double criticalPowerW,
        double wPrimeCapacityJ,
        double peakPowerW,
        double lowIntensityDurability,
        double highIntensityDurability,
        double bodyMassKg,
        double cdAM2,
        double baseCrr,
        double positioning,
        double handling,
        int potentialOvr,
        double? cdATtM2 = null)
    {
        double cpPerKg = criticalPowerW / bodyMassKg;
        double pmaxPerKg = peakPowerW / bodyMassKg;
        double timeTrialCdA = cdATtM2 ?? cdAM2;

        int climb = ClampRating((int)Math.Round(
            0.55 * Score(cpPerKg, 4.80, 6.55) +
            0.20 * Score(lowIntensityDurability, 0.70, 0.98) +
            0.15 * Score(1.0 / bodyMassKg, 1.0 / 82.0, 1.0 / 56.0) +
            0.10 * Score(wPrimeCapacityJ, 18000, 32000)));

        int hills = ClampRating((int)Math.Round(
            0.35 * Score(cpPerKg, 5.00, 6.30) +
            0.30 * Score(wPrimeCapacityJ, 20000, 35000) +
            0.20 * Score(pmaxPerKg, 12.0, 22.0) +
            0.15 * Score(highIntensityDurability, 0.70, 0.96)));

        int flat = ClampRating((int)Math.Round(
            0.40 * Score(criticalPowerW, 340, 430) +
            0.25 * Score(-cdAM2, -0.34, -0.24) +
            0.20 * Score(positioning, 0.55, 0.95) +
            0.15 * Score(peakPowerW, 850, 1600)));

        int timeTrial = ClampRating((int)Math.Round(
            0.45 * Score(criticalPowerW, 350, 440) +
            0.40 * Score(-timeTrialCdA, -0.34, -0.155) +
            0.15 * Score(-baseCrr, -0.0055, -0.0034)));

        int sprint = ClampRating((int)Math.Round(
            0.40 * Score(peakPowerW, 900, 1800) +
            0.25 * Score(wPrimeCapacityJ, 20000, 38000) +
            0.20 * Score(positioning, 0.55, 0.95) +
            0.15 * Score(pmaxPerKg, 13.0, 24.0)));

        int cobbles = ClampRating((int)Math.Round(
            0.30 * Score(handling, 0.50, 0.95) +
            0.25 * Score(positioning, 0.55, 0.95) +
            0.20 * Score(bodyMassKg, 62, 82) +
            0.15 * Score(highIntensityDurability, 0.70, 0.96) +
            0.10 * Score(peakPowerW, 900, 1600)));

        int[] sortedDesc = new[] { climb, hills, flat, timeTrial, sprint, cobbles }
            .OrderByDescending(value => value)
            .ToArray();
        int ovr = ClampRating((int)Math.Round(
            0.55 * sortedDesc[0] + 0.45 * ((sortedDesc[0] + sortedDesc[1] + sortedDesc[2]) / 3.0)));

        int pot = Math.Max(potentialOvr, ovr);
        pot = ClampRating(pot);

        return new RiderRatingSet(climb, hills, flat, timeTrial, sprint, cobbles, ovr, pot);
    }

    public static int ResolveStoredPotentialOvr(int? contentPotentialOvr, RiderRatingSet derivedWithoutPot)
    {
        int pot = contentPotentialOvr ?? Math.Max(derivedWithoutPot.Ovr, 70);
        return Math.Max(pot, derivedWithoutPot.Ovr);
    }

    private static double Scale01(double x, double min, double max) =>
        Math.Clamp((x - min) / (max - min), 0.0, 1.0);

    private static double Score(double x, double min, double max) =>
        1.0 + 98.0 * Scale01(x, min, max);

    private static int ClampRating(int value) => Math.Clamp(value, 1, 99);
}
