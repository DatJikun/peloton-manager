using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class CapabilitySolverTests
{
    [Fact]
    public void SupraCriticalWorkConsumesWPrimeAndPeakPowerCapsOutput()
    {
        RaceRiderProfile profile = Profile(800.0, 0.8, 0.8);
        RiderPhysiologyState fresh = RiderPhysiologyState.Fresh(profile);

        CapabilityResult result = CapabilitySolver.Evaluate(profile, fresh, 900.0, 10.0);

        Assert.Equal(profile.PeakPowerW, result.RealizablePowerW, 8);
        Assert.True(result.NextState.WPrimeRemainingJ < fresh.WPrimeRemainingJ);
        Assert.True(result.NextState.HighIntensityWorkJ > fresh.HighIntensityWorkJ);
    }

    [Fact]
    public void SubCriticalWorkRecoversWPrimeWithoutExceedingCapacity()
    {
        RaceRiderProfile profile = Profile(800.0, 0.8, 0.8);
        RiderPhysiologyState depleted = new(
            WPrimeRemainingJ: 4_000.0,
            LowIntensityWorkJ: 0.0,
            HighIntensityWorkJ: 0.0);

        CapabilityResult result = CapabilitySolver.Evaluate(profile, depleted, 180.0, 60.0);

        Assert.True(result.NextState.WPrimeRemainingJ > depleted.WPrimeRemainingJ);
        Assert.True(result.NextState.WPrimeRemainingJ <= profile.WPrimeCapacityJ);
        Assert.True(result.NextState.LowIntensityWorkJ > depleted.LowIntensityWorkJ);
    }

    [Fact]
    public void HighIntensityLoadReducesLatePowerMoreForLowDurabilityRider()
    {
        RiderPhysiologyState late = new(
            WPrimeRemainingJ: 18_000.0,
            LowIntensityWorkJ: 4_500_000.0,
            HighIntensityWorkJ: 900_000.0);
        RaceRiderProfile durable = Profile(900.0, 0.92, 0.92);
        RaceRiderProfile fragile = Profile(900.0, 0.35, 0.35);

        CapabilityResult durableResult = CapabilitySolver.Evaluate(durable, late, 520.0, 60.0);
        CapabilityResult fragileResult = CapabilitySolver.Evaluate(fragile, late, 520.0, 60.0);

        Assert.True(durableResult.RealizablePowerW > fragileResult.RealizablePowerW);
        Assert.True(durableResult.EffectiveCriticalPowerW > fragileResult.EffectiveCriticalPowerW);
    }

    private static RaceRiderProfile Profile(
        double peakPowerW,
        double lowIntensityDurability,
        double highIntensityDurability)
    {
        return new RaceRiderProfile(
            new WorldEntityId(11),
            new WorldEntityId(101),
            criticalPowerW: 360.0,
            wPrimeCapacityJ: 24_000.0,
            peakPowerW,
            wPrimeRecoveryJPerSecond: 45.0,
            lowIntensityDurability,
            highIntensityDurability,
            bodyMassKg: 68.0,
            systemMassKg: 8.0,
            cdAM2: 0.31,
            baseCrr: 0.004,
            positioning: 0.7,
            handling: 0.7,
            tacticalAwareness: 0.7);
    }
}
