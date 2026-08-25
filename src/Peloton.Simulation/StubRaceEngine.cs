using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Simulation;

public sealed record StubRaceResult(string RouteId, IReadOnlyList<WorldEntityId> FinishOrder)
{
    public WorldEntityId WinnerId => FinishOrder[0];
}

public sealed class StubRaceEngine
{
    private readonly int contractVersion;

    public StubRaceEngine(int contractVersion = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(contractVersion);
        this.contractVersion = contractVersion;
    }

    public StubRaceResult Run(
        long masterSeed,
        string routeId,
        IReadOnlyList<WorldEntityId> startList,
        int raceNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeId);
        ArgumentNullException.ThrowIfNull(startList);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(raceNumber);

        if (startList.Count == 0)
        {
            throw new ArgumentException("A stub race requires at least one starter.", nameof(startList));
        }

        WorldEntityId[] result = startList
            .Select(rider => new
            {
                Rider = rider,
                Score = Score(masterSeed, routeId, raceNumber, rider),
            })
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Rider.Value)
            .Select(entry => entry.Rider)
            .ToArray();

        return new StubRaceResult(routeId, result);
    }

    private ulong Score(long masterSeed, string routeId, int raceNumber, WorldEntityId rider)
    {
        string scope = $"stub-race-v{contractVersion}\u001f{routeId}\u001f{raceNumber}\u001f{rider.Value}";
        DeterministicRng stream = new(StableSeedDerivation.Derive(masterSeed, scope));
        return stream.NextUInt64();
    }
}
