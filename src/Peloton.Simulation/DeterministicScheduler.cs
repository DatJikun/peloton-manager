using System;
using Peloton.Domain;

namespace Peloton.Simulation;

public sealed class DeterministicScheduler
{
    public static void AdvanceDay(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.AdvanceOneDay();
    }
}
