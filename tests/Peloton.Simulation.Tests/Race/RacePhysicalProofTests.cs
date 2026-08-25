using Peloton.Simulation.Race;
using System.Linq;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class RacePhysicalProofTests
{
    [Fact]
    public void BatchIsOnlyALoopOverTheCanonicalStep()
    {
        RaceScenario scenario = RaceScenarioFactory.Basic();
        PrototypeRaceEngine engine = new();

        RaceResult batch = engine.RunBatch(scenario, 404);
        RaceResult stepped = RaceScenarioFactory.RunEveryStep(engine, scenario, 404);

        Assert.Equal(batch.Checksum, stepped.Checksum);
        Assert.Equal(batch.FinishOrder, stepped.FinishOrder);
    }

    [Fact]
    public void SameSeedAndScenarioRepeatOfficialResult()
    {
        RaceScenario scenario = RaceScenarioFactory.Basic();
        PrototypeRaceEngine engine = new();

        RaceResult first = engine.RunBatch(scenario, 91234);
        RaceResult second = engine.RunBatch(scenario, 91234);

        Assert.Equal(first.Checksum, second.Checksum);
        Assert.Equal(first.FinishOrder, second.FinishOrder);
    }

    [Fact]
    public void DraftingPositionChangesEnergyCostAndPaceUpSurvival()
    {
        PrototypeRaceEngine engine = new();

        RaceResult result = engine.RunBatch(RaceScenarioFactory.DraftingPosition(), 1);
        RaceRiderMetrics shelteredWeak = Metrics(result, RaceScenarioFactory.WeakRiderId.Value);
        RaceRiderMetrics exposedWeak = Metrics(result, RaceScenarioFactory.ExposedWeakRiderId.Value);

        Assert.True(
            shelteredWeak.EnergySpentJ < exposedWeak.EnergySpentJ,
            $"sheltered={shelteredWeak}; exposed={exposedWeak}");
        Assert.True(
            shelteredWeak.WPrimeRemainingJ > exposedWeak.WPrimeRemainingJ,
            $"sheltered={shelteredWeak}; exposed={exposedWeak}");
        Assert.True(
            shelteredWeak.FinishTimeSeconds < exposedWeak.FinishTimeSeconds,
            $"sheltered={shelteredWeak}; exposed={exposedWeak}");
        Assert.True(
            shelteredWeak.MaximumGapDuringPressureM < 5.0,
            $"sheltered={shelteredWeak}; exposed={exposedWeak}");
        Assert.True(
            exposedWeak.MaximumGapDuringPressureM > 5.0,
            $"sheltered={shelteredWeak}; exposed={exposedWeak}");
    }

    [Fact]
    public void RepeatedAttacksSelectTheRiderWhoRetainsWPrimeLater()
    {
        RaceResult result = new PrototypeRaceEngine().RunBatch(
            RaceScenarioFactory.RepeatedAttacks(),
            2);
        RaceRiderMetrics highReserve = Metrics(result, 21);
        RaceRiderMetrics lowReserve = Metrics(result, 22);

        Assert.True(
            highReserve.WPrimeRemainingJ > lowReserve.WPrimeRemainingJ,
            $"high={highReserve}; low={lowReserve}");
        Assert.True(
            highReserve.FinishTimeSeconds < lowReserve.FinishTimeSeconds,
            $"high={highReserve}; low={lowReserve}");
        Assert.True(
            lowReserve.MaximumGapAheadM > 5.0,
            $"high={highReserve}; low={lowReserve}");
    }

    [Fact]
    public void LateRaceDurabilityDifferenceIsVisible()
    {
        PrototypeRaceEngine engine = new();

        RaceRiderMetrics durable = engine.RunBatch(
            RaceScenarioFactory.DurabilitySolo(true),
            3).RiderMetrics.Single();
        RaceRiderMetrics fragile = engine.RunBatch(
            RaceScenarioFactory.DurabilitySolo(false),
            3).RiderMetrics.Single();

        Assert.True(
            durable.FinishTimeSeconds + 2.0 < fragile.FinishTimeSeconds,
            $"durable={durable}; fragile={fragile}");
    }

    [Fact]
    public void PowerDeficitGapAndLostShelterDropRiderWithoutScriptedFlag()
    {
        RaceResult result = new PrototypeRaceEngine().RunBatch(
            RaceScenarioFactory.NaturalDrop(),
            4);
        RaceRiderMetrics leader = Metrics(result, 41);
        RaceRiderMetrics dropped = Metrics(result, 43);

        Assert.True(dropped.MaximumGapAheadM > 5.0);
        Assert.True(dropped.LostShelterTransitions > 0);
        Assert.True(dropped.FinishTimeSeconds > leader.FinishTimeSeconds + 2.0);
    }

    [Fact]
    public void CrosswindAndFiniteShelterSplitGroup()
    {
        RaceResult result = new PrototypeRaceEngine().RunBatch(
            RaceScenarioFactory.Crosswind(),
            5);

        Assert.True(result.MaximumGroupCount >= 2);
        Assert.Contains(result.RiderMetrics, rider => rider.LostShelterTransitions > 0);
    }

    private static RaceRiderMetrics Metrics(RaceResult result, long riderId)
    {
        return result.RiderMetrics.Single(rider => rider.RiderId.Value == riderId);
    }
}
