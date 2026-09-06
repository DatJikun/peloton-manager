using Xunit;

namespace Peloton.Domain.Tests;

public sealed class RiderSeasonalFatigueTests
{
    private static RiderCareer CreateSampleCareer() => new(
        new WorldEntityId(1),
        new WorldEntityId(2),
        new WorldEntityId(3),
        "rider.test.fatigue",
        criticalPowerW: 420.0,
        wPrimeCapacityJ: 24_000.0,
        peakPowerW: 1100.0,
        wPrimeRecoveryJPerSecond: 45.0,
        lowIntensityDurability: 0.92,
        highIntensityDurability: 0.89,
        bodyMassKg: 68.0,
        systemMassKg: 7.8,
        cdAM2: 0.25,
        baseCrr: 0.0036,
        positioning: 0.85,
        handling: 0.82,
        tacticalAwareness: 0.88);

    [Fact]
    public void InitialCareerHasZeroSeasonalFatigueAndRaceDays()
    {
        RiderCareer career = CreateSampleCareer();

        Assert.Equal(0.0, career.SeasonalFatigue01);
        Assert.Equal(0, career.SeasonRaceDaysCount);
    }

    [Fact]
    public void ApplyRaceLoadIncrementsRaceDaysAndBuildsSeasonalFatigue()
    {
        RiderCareer career = CreateSampleCareer();
        double initialReadiness = career.ComputeReadiness();

        career.ApplyRaceLoad();

        Assert.Equal(1, career.SeasonRaceDaysCount);
        Assert.True(career.SeasonalFatigue01 > 0.0);
        Assert.True(career.ComputeReadiness() < initialReadiness);
    }

    [Fact]
    public void HeavyRacingAcceleratesSeasonalFatigueAndDegradesForm()
    {
        RiderCareer career = CreateSampleCareer();

        for (int i = 0; i < 40; i++)
        {
            career.ApplyRaceLoad();
        }

        Assert.Equal(40, career.SeasonRaceDaysCount);
        Assert.True(career.SeasonalFatigue01 >= 0.50);
        Assert.True(career.ComputeReadiness() < 0.70);
    }

    [Fact]
    public void RestTickDissipatesSeasonalFatigueWhenFresh()
    {
        RiderCareer career = CreateSampleCareer();
        career.ApplyRaceLoad();
        double fatigueAfterRace = career.SeasonalFatigue01;

        for (int day = 0; day < 10; day++)
        {
            career.ApplyRestTick();
        }

        Assert.True(career.SeasonalFatigue01 < fatigueAfterRace);
    }

    [Fact]
    public void WinterResetClearsSeasonalFatigueAndRaceDays()
    {
        RiderCareer career = CreateSampleCareer();
        career.ApplyRaceLoad();
        career.ApplyRaceLoad();

        career.ApplyWinterFormReset();

        Assert.Equal(0.0, career.SeasonalFatigue01);
        Assert.Equal(0, career.SeasonRaceDaysCount);
        Assert.Equal(1.0, career.Form01);
        Assert.Equal(1.0, career.Freshness01);
        Assert.Equal(0.0, career.Fatigue01);
    }
}
