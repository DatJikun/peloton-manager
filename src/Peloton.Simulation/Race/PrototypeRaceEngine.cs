using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Simulation.Race;

public sealed class RaceScenario
{
    private readonly RaceRiderProfile[] riders;
    private readonly RaceStartingPosition[] startingPositions;
    private readonly RaceCommand[] commands;

    public RaceScenario(
        string id,
        RaceDefinition definition,
        IEnumerable<RaceRiderProfile> riders,
        IEnumerable<RaceStartingPosition> startingPositions,
        IEnumerable<RaceCommand> commands,
        double initialSpeedMps,
        int maximumDurationSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(riders);
        ArgumentNullException.ThrowIfNull(startingPositions);
        ArgumentNullException.ThrowIfNull(commands);
        if (!double.IsFinite(initialSpeedMps) || initialSpeedMps <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialSpeedMps));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumDurationSeconds);
        this.riders = riders.OrderBy(rider => rider.RiderId.Value).ToArray();
        this.startingPositions = startingPositions
            .OrderBy(position => position.RiderId.Value)
            .ToArray();
        this.commands = commands
            .OrderBy(command => command.SimulationSecond)
            .ThenBy(command => command.OrganizationId.Value)
            .ThenBy(command => command.RiderId.Value)
            .ToArray();
        if (this.riders.Length == 0 || this.startingPositions.Length != this.riders.Length)
        {
            throw new ArgumentException("Race riders and starting positions must be non-empty and aligned.");
        }

        long[] riderIds = this.riders.Select(rider => rider.RiderId.Value).ToArray();
        long[] positionIds = this.startingPositions.Select(position => position.RiderId.Value).ToArray();
        if (!riderIds.SequenceEqual(positionIds) || riderIds.Distinct().Count() != riderIds.Length)
        {
            throw new ArgumentException("Race rider IDs and starting positions must be unique and identical.");
        }

        Id = id;
        Definition = definition;
        InitialSpeedMps = initialSpeedMps;
        MaximumDurationSeconds = maximumDurationSeconds;
    }

    public string Id { get; }

    public RaceDefinition Definition { get; }

    public IReadOnlyList<RaceRiderProfile> Riders => riders;

    public IReadOnlyList<RaceStartingPosition> StartingPositions => startingPositions;

    public IReadOnlyList<RaceCommand> Commands => commands;

    public double InitialSpeedMps { get; }

    public int MaximumDurationSeconds { get; }
}

public interface IRaceEngine
{
    RaceSession CreateSession(RaceScenario scenario, long seed);

    RaceResult RunBatch(RaceScenario scenario, long seed);
}

public sealed class PrototypeRaceEngine : IRaceEngine
{
    public RaceSession CreateSession(RaceScenario scenario, long seed)
    {
        return new RaceSession(scenario, seed);
    }

    public RaceResult RunBatch(RaceScenario scenario, long seed)
    {
        RaceSession session = CreateSession(scenario, seed);
        while (!session.IsCompleted)
        {
            RaceStepResult step = session.Step();
            if (step.Status == RaceStepStatus.DecisionRequired)
            {
                throw new InvalidOperationException(
                    "The current physical prototype cannot resolve a DecisionRequest yet.");
            }
        }

        return session.Result!;
    }
}
