using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class RaceDecisionAndSpyTests
{
    private static readonly WorldEntityId TeamA = new(701);
    private static readonly WorldEntityId TeamAAuthority = new(801);

    [Fact]
    public void TeamsCanDisagreeBecauseObjectivesAndEnergyCostDiffer()
    {
        ChaseDecision stageHunters = ChaseDecisionEvaluator.Evaluate(
            Observation(RaceObjective.StageWin, RaceResourceEstimate.Strong),
            new RaceBriefing(RaceBriefingKind.Chase, ConsultManager: true));
        ChaseDecision gcTeam = ChaseDecisionEvaluator.Evaluate(
            Observation(RaceObjective.GeneralClassification, RaceResourceEstimate.Limited),
            new RaceBriefing(RaceBriefingKind.Protect, ConsultManager: true));

        Assert.Equal(RaceDecisionOption.CommitSupport, stageHunters.SelectedOption);
        Assert.Equal(RaceDecisionOption.WaitForRivals, gcTeam.SelectedOption);
    }

    [Fact]
    public void DecisionInputContainsPublishedKnowledgeRatherThanTruthState()
    {
        string[] propertyNames = typeof(TeamRaceObservation)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, name =>
            name.Contains("Truth", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("WPrime", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Durability", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nameof(TeamRaceObservation.OfficialGapSeconds), propertyNames);
        Assert.Contains(nameof(TeamRaceObservation.ResourceEstimate), propertyNames);
        Assert.Contains(nameof(TeamRaceObservation.Confidence), propertyNames);
    }

    [Fact]
    public void ProtectAndChaseBriefingsChangeBehaviorNotPhysicsRules()
    {
        PrototypeRaceEngine engine = new();

        RaceResult protect = engine.RunBatch(Scenario(RaceBriefingKind.Protect), 404);
        RaceResult chase = engine.RunBatch(Scenario(RaceBriefingKind.Chase), 404);

        Assert.NotEqual(protect.TeamEnergyJ[TeamA], chase.TeamEnergyJ[TeamA]);
        Assert.Equal(protect.PhysicsContractVersion, chase.PhysicsContractVersion);
        Assert.Equal(1, protect.DecisionCount);
        Assert.Equal(1, chase.DecisionCount);
    }

    [Fact]
    public void PendingDecisionPausesTheSameRaceSessionUntilAuthorityResolvesIt()
    {
        RaceSession session = new PrototypeRaceEngine().CreateSession(
            Scenario(RaceBriefingKind.Chase),
            505);

        RaceStepResult barrier = StepUntilDecision(session);
        RaceDecisionRequest request = Assert.IsType<RaceDecisionRequest>(session.PendingDecision);
        int pausedAtSecond = session.SimulationSecond;

        Assert.Equal(RaceStepStatus.DecisionRequired, barrier.Status);
        Assert.False(session.IsCompleted);
        Assert.True(request.DefensibleOptions.Count >= 2);
        Assert.Contains(RaceDecisionOption.CommitSupport, request.DefensibleOptions);
        Assert.Contains(RaceDecisionOption.WaitForRivals, request.DefensibleOptions);

        RaceStepResult stillPaused = session.Step();
        Assert.Equal(RaceStepStatus.DecisionRequired, stillPaused.Status);
        Assert.Equal(pausedAtSecond, session.SimulationSecond);
        Assert.Throws<InvalidOperationException>(() => session.ResolveDecision(new RaceDecisionResolution(
            request.Id,
            new WorldEntityId(999),
            RaceDecisionOption.CommitSupport)));
        Assert.Throws<InvalidOperationException>(() => session.ResolveDecision(new RaceDecisionResolution(
            new RaceDecisionRequestId("different-request"),
            TeamAAuthority,
            RaceDecisionOption.CommitSupport)));
        Assert.Throws<InvalidOperationException>(() => session.ResolveDecision(new RaceDecisionResolution(
            request.Id,
            TeamAAuthority,
            (RaceDecisionOption)999)));
        Assert.Equal(pausedAtSecond, session.SimulationSecond);

        session.ResolveDecision(new RaceDecisionResolution(
            request.Id,
            TeamAAuthority,
            RaceDecisionOption.CommitSupport));
        Assert.Null(session.PendingDecision);
        Assert.Throws<InvalidOperationException>(() => session.ResolveDecision(new RaceDecisionResolution(
            request.Id,
            TeamAAuthority,
            RaceDecisionOption.CommitSupport)));

        while (!session.IsCompleted)
        {
            RaceStepResult step = session.Step();
            Assert.NotEqual(RaceStepStatus.DecisionRequired, step.Status);
        }

        Assert.Equal(1, session.Result!.DecisionCount);
    }

    [Fact]
    public void SpyOnAndOffProduceTheSameOfficialResultAndCompleteDecisionTrace()
    {
        PrototypeRaceEngine engine = new();
        RaceScenario scenario = Scenario(RaceBriefingKind.Chase);
        RaceResult off = engine.RunBatch(scenario, 606, NullWorldSpySink.Instance);
        CollectingWorldSpySink spy = new();

        RaceResult on = engine.RunBatch(scenario, 606, spy);

        Assert.Equal(off.Checksum, on.Checksum);
        Assert.Equal(off.FinishOrder, on.FinishOrder);
        DecisionTrace trace = Assert.Single(spy.Traces, item => item.SelectedOption.Length > 0);
        Assert.NotEmpty(trace.ActorKnownInputs);
        Assert.NotEmpty(trace.ActorInterpretations);
        Assert.True(trace.ConsideredOptions.Count >= 2);
        Assert.NotEmpty(trace.SelectionReasons);
        Assert.NotEmpty(trace.CommandsEmitted);
        Assert.False(string.IsNullOrWhiteSpace(trace.TruthDebugRef));
        Assert.DoesNotContain(trace.ActorKnownInputs.Keys, key =>
            key.Contains("WPrime", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("Durability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RaceSpyExportsCapturedStructureAsJsonAndMarkdownProjection()
    {
        CollectingWorldSpySink spy = new();
        _ = new PrototypeRaceEngine().RunBatch(Scenario(RaceBriefingKind.Chase), 707, spy);

        string json = RaceSpyReport.ExportJson(spy.Traces);
        string markdown = RaceSpyReport.ExportMarkdown(spy.Traces);

        Assert.Contains("\"domain\": \"Race\"", json, StringComparison.Ordinal);
        Assert.Contains("CommitSupport", json, StringComparison.Ordinal);
        Assert.Contains("## Race decision", markdown, StringComparison.Ordinal);
        Assert.Contains("Known inputs", markdown, StringComparison.Ordinal);
        Assert.Contains("Selected: CommitSupport", markdown, StringComparison.Ordinal);
    }

    private static TeamRaceObservation Observation(
        RaceObjective objective,
        RaceResourceEstimate resources)
    {
        return new TeamRaceObservation(
            TeamA,
            TeamAAuthority,
            OfficialGapSeconds: 42,
            VisibleSplit: true,
            LeaderPositionBand: RacePositionBand.Front,
            ResourceEstimate: resources,
            ThreatEstimate: RaceThreatEstimate.High,
            Objective: objective,
            Confidence: RaceInformationConfidence.High);
    }

    private static RaceScenario Scenario(RaceBriefingKind briefingKind)
    {
        RaceRiderProfile support = RaceScenarioFactory.Profile(71, TeamA.Value, 390, 28_000, 930, 0.85);
        RaceRiderProfile leader = RaceScenarioFactory.Profile(72, TeamA.Value, 370, 25_000, 900, 0.82);
        RaceRiderProfile rival = RaceScenarioFactory.Profile(73, 702, 375, 26_000, 910, 0.83);
        RaceRiderProfile rivalSupport = RaceScenarioFactory.Profile(74, 702, 360, 23_000, 880, 0.78);
        RaceRiderProfile[] riders = { support, leader, rival, rivalSupport };
        RaceDefinition definition = new(
            "route.proof.tactical-choice",
            1.225,
            new[]
            {
                new RaceRouteSegment(
                    "segment.proof.tactical-choice",
                    lengthM: 1_800,
                    gradient: 0,
                    roadWidthM: 5,
                    windSpeedMps: 3,
                    windYawDegrees: 25),
            });
        RaceStartingPosition[] positions = riders
            .Select((rider, index) => new RaceStartingPosition(rider.RiderId, (3 - index) * 0.7))
            .ToArray();
        RaceTacticalPlan plan = new(
            TriggerSecond: 5,
            SupportRiderId: support.RiderId,
            Observation(RaceObjective.StageWin,
                briefingKind == RaceBriefingKind.Chase
                    ? RaceResourceEstimate.Strong
                    : RaceResourceEstimate.Limited),
            new RaceBriefing(briefingKind, ConsultManager: true));
        return new RaceScenario(
            "race.proof.tactical-choice",
            definition,
            riders,
            positions,
            Array.Empty<RaceCommand>(),
            initialSpeedMps: 11,
            maximumDurationSeconds: 600,
            tacticalPlans: new[] { plan });
    }

    private static RaceStepResult StepUntilDecision(RaceSession session)
    {
        while (true)
        {
            RaceStepResult step = session.Step();
            if (step.Status == RaceStepStatus.DecisionRequired)
            {
                return step;
            }

            Assert.False(session.IsCompleted);
        }
    }
}
