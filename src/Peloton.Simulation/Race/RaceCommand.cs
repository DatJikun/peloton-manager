using System;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public enum RaceCommandKind
{
    HoldPosition,
    ForcePace,
    Attack,
    Conserve,
    LaunchSprint,
}

public sealed record RaceCommand
{
    public RaceCommand(
        int simulationSecond,
        WorldEntityId organizationId,
        WorldEntityId riderId,
        RaceCommandKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(simulationSecond);
        SimulationSecond = simulationSecond;
        OrganizationId = organizationId;
        RiderId = riderId;
        Kind = kind;
    }

    public int SimulationSecond { get; }

    public WorldEntityId OrganizationId { get; }

    public WorldEntityId RiderId { get; }

    public RaceCommandKind Kind { get; }
}

public sealed record RaceStartingPosition
{
    public RaceStartingPosition(WorldEntityId riderId, double distanceM)
    {
        if (!double.IsFinite(distanceM) || distanceM < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceM));
        }

        RiderId = riderId;
        DistanceM = distanceM;
    }

    public WorldEntityId RiderId { get; }

    public double DistanceM { get; }
}
