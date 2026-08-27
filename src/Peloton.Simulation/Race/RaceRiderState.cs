using Peloton.Domain;

namespace Peloton.Simulation.Race;

public sealed record RaceRiderSnapshot
{
    public RaceRiderSnapshot(
        WorldEntityId riderId,
        double distanceM,
        double speedMps,
        double positioning)
    {
        RiderId = riderId;
        DistanceM = distanceM;
        SpeedMps = speedMps;
        Positioning = positioning;
    }

    public WorldEntityId RiderId { get; }

    public double DistanceM { get; }

    public double SpeedMps { get; }

    public double Positioning { get; }
}

public sealed record ResolvedRaceRiderPosition(
    WorldEntityId RiderId,
    int PositionSlot,
    int GroupId,
    double GapAheadM,
    double ShelterMultiplier);
