using System.Collections.Generic;
using Peloton.Domain;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Simulation.Tests;

public sealed class DeterministicSimulationTests
{
    [Fact]
    public void SameSeedStartListAndRouteProduceSameRaceOrder()
    {
        StubRaceEngine engine = new();
        WorldEntityId[] startList =
        {
            new(11),
            new(12),
            new(13),
        };

        StubRaceResult first = engine.Run(404, "route.skeleton.flat", startList, 1);
        StubRaceResult second = engine.Run(404, "route.skeleton.flat", startList, 1);

        Assert.Equal(new long[] { 11, 13, 12 }, Values(first.FinishOrder));
        Assert.Equal(Values(first.FinishOrder), Values(second.FinishOrder));
    }

    private static List<long> Values(IReadOnlyList<WorldEntityId> ids)
    {
        List<long> values = new(ids.Count);
        foreach (WorldEntityId id in ids)
        {
            values.Add(id.Value);
        }

        return values;
    }
}
