using System;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class BunchSprintTests
{
    [Fact]
    public void HighPeakSprinterBeatsHighCpClimberOnFlatBunchFinish()
    {
        RaceScenario scenario = RaceScenarioFactory.BunchSprintFinish();
        PrototypeRaceEngine engine = new();

        RaceResult result = engine.RunBatch(scenario, 91234);

        int sprinterPlace = PlaceOf(result, RaceScenarioFactory.BunchSprinterId);
        int climberPlace = PlaceOf(result, RaceScenarioFactory.BunchClimberId);
        Assert.True(
            sprinterPlace > 0 && sprinterPlace < climberPlace,
            $"sprinter={sprinterPlace} climber={climberPlace} winner={result.WinnerId.Value}");
    }

    [Fact]
    public void BunchSprintSpyOnAndOffMatchFinishOrder()
    {
        RaceScenario scenario = RaceScenarioFactory.BunchSprintFinish();
        PrototypeRaceEngine engine = new();

        RaceResult off = engine.RunBatch(scenario, 91234, NullWorldSpySink.Instance);
        CollectingWorldSpySink spy = new();
        RaceResult on = engine.RunBatch(scenario, 91234, spy);

        Assert.Equal(off.FinishOrder, on.FinishOrder);
        Assert.Equal(off.Checksum, on.Checksum);
    }

    [Fact]
    public void UnclassifiedShortCourseDoesNotUseBunchSprintGate()
    {
        Assert.False(BunchSprintResolver.IsClassifiedEligible(null, 5_400.0));
        Assert.True(BunchSprintResolver.IsClassifiedEligible(ClassifiedStageType.Flat, 2_500.0));
        Assert.False(BunchSprintResolver.IsClassifiedEligible(ClassifiedStageType.Mountain, 180_000.0));
        Assert.False(BunchSprintResolver.IsClassifiedEligible(ClassifiedStageType.CobbleClassic, 260_000.0));
        Assert.True(BunchSprintResolver.MeanGradientOfLastWindow(
            RaceScenarioFactory.BunchSprintFinish().Definition) < 0.015);
    }

    private static int PlaceOf(RaceResult result, WorldEntityId riderId)
    {
        for (int index = 0; index < result.FinishOrder.Count; index++)
        {
            if (result.FinishOrder[index] == riderId)
            {
                return index + 1;
            }
        }

        return -1;
    }
}
