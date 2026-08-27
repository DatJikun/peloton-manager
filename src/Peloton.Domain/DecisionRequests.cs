using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Domain;

public readonly record struct RaceDecisionRequestId
{
    public RaceDecisionRequestId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public enum RaceDecisionOption
{
    CommitSupport,
    WaitForRivals,
    ProtectSecondLeader,
    TrustDs,
}

public enum RaceDecisionRequestStatus
{
    Pending,
    Resolved,
}

public sealed record RaceDecisionResolution(
    RaceDecisionRequestId RequestId,
    WorldEntityId AuthorityId,
    RaceDecisionOption SelectedOption);

public sealed class RaceDecisionRequest
{
    private readonly RaceDecisionOption[] defensibleOptions;

    public RaceDecisionRequest(
        RaceDecisionRequestId id,
        WorldEntityId authorityId,
        int raceSecond,
        string trigger,
        IEnumerable<RaceDecisionOption> defensibleOptions,
        RaceDecisionOption delegatedDefaultOption)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(raceSecond);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);
        ArgumentNullException.ThrowIfNull(defensibleOptions);
        this.defensibleOptions = defensibleOptions.Distinct().ToArray();
        if (this.defensibleOptions.Length < 2)
        {
            throw new ArgumentException(
                "A strategic RaceDecisionRequest needs at least two defensible options.",
                nameof(defensibleOptions));
        }

        if (!this.defensibleOptions.Contains(delegatedDefaultOption))
        {
            throw new ArgumentException(
                "The delegated/default option must be one of the defensible options.",
                nameof(delegatedDefaultOption));
        }

        Id = id;
        AuthorityId = authorityId;
        RaceSecond = raceSecond;
        Trigger = trigger;
        DelegatedDefaultOption = delegatedDefaultOption;
    }

    public RaceDecisionRequestId Id { get; }

    public WorldEntityId AuthorityId { get; }

    public int RaceSecond { get; }

    public int ResolutionBarrierSecond => RaceSecond;

    public string Trigger { get; }

    public IReadOnlyList<RaceDecisionOption> DefensibleOptions => defensibleOptions;

    public RaceDecisionOption DelegatedDefaultOption { get; }

    public RaceDecisionRequestStatus Status { get; private set; }

    public RaceDecisionOption? ResolvedOption { get; private set; }

    public void Resolve(RaceDecisionResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        if (Status != RaceDecisionRequestStatus.Pending)
        {
            throw new InvalidOperationException("Race decision has already been resolved.");
        }

        if (resolution.RequestId != Id)
        {
            throw new InvalidOperationException("Race decision resolution targets a different request.");
        }

        if (resolution.AuthorityId != AuthorityId)
        {
            throw new InvalidOperationException("Race decision resolution came from the wrong authority.");
        }

        if (!defensibleOptions.Contains(resolution.SelectedOption))
        {
            throw new InvalidOperationException("Race decision resolution selected an illegal option.");
        }

        ResolvedOption = resolution.SelectedOption;
        Status = RaceDecisionRequestStatus.Resolved;
    }
}
