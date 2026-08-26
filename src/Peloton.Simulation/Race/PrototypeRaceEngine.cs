using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Simulation.Race;

public sealed class RaceScenario
{
    private readonly RaceRiderProfile[] riders;
    private readonly RaceStartingPosition[] startingPositions;
    private readonly RaceCommand[] commands;
    private readonly RaceTacticalPlan[] tacticalPlans;

    public RaceScenario(
        string id,
        RaceDefinition definition,
        IEnumerable<RaceRiderProfile> riders,
        IEnumerable<RaceStartingPosition> startingPositions,
        IEnumerable<RaceCommand> commands,
        double initialSpeedMps,
        int maximumDurationSeconds,
        IEnumerable<RaceTacticalPlan>? tacticalPlans = null)
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
        this.tacticalPlans = (tacticalPlans ?? Array.Empty<RaceTacticalPlan>())
            .OrderBy(plan => plan.TriggerSecond)
            .ThenBy(plan => plan.Observation.OrganizationId.Value)
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

        foreach (RaceTacticalPlan plan in this.tacticalPlans)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(plan.TriggerSecond);
            RaceRiderProfile support = this.riders.SingleOrDefault(
                rider => rider.RiderId == plan.SupportRiderId)
                ?? throw new ArgumentException("A tactical plan support rider must start the race.");
            if (support.OrganizationId != plan.Observation.OrganizationId)
            {
                throw new ArgumentException("A tactical plan cannot command another organization's rider.");
            }
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

    public IReadOnlyList<RaceTacticalPlan> TacticalPlans => tacticalPlans;

    public double InitialSpeedMps { get; }

    public int MaximumDurationSeconds { get; }
}

public interface IRaceEngine
{
    RaceSession CreateSession(
        RaceScenario scenario,
        long seed,
        Peloton.Domain.IWorldSpySink? spySink = null);

    RaceResult RunBatch(
        RaceScenario scenario,
        long seed,
        Peloton.Domain.IWorldSpySink? spySink = null);
}

public sealed class PrototypeRaceEngine : IRaceEngine
{
    public RaceSession CreateSession(
        RaceScenario scenario,
        long seed,
        Peloton.Domain.IWorldSpySink? spySink = null)
    {
        return new RaceSession(scenario, seed, spySink ?? Peloton.Domain.NullWorldSpySink.Instance);
    }

    public RaceResult RunBatch(
        RaceScenario scenario,
        long seed,
        Peloton.Domain.IWorldSpySink? spySink = null)
    {
        RaceSession session = CreateSession(scenario, seed, spySink);
        while (!session.IsCompleted)
        {
            RaceStepResult step = session.Step();
            if (step.Status == RaceStepStatus.DecisionRequired)
            {
                Peloton.Domain.RaceDecisionRequest request = session.PendingDecision!;
                session.ResolveDecision(new Peloton.Domain.RaceDecisionResolution(
                    request.Id,
                    request.AuthorityId,
                    request.DelegatedDefaultOption));
            }
        }

        return session.Result!;
    }
}
