using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public enum RaceBriefingKind
{
    Protect,
    Chase,
}

public sealed record RaceBriefing(RaceBriefingKind Kind, bool ConsultManager);

public sealed record ChaseDecision(
    RaceDecisionOption SelectedOption,
    IReadOnlyList<DecisionOptionEvaluation> OptionEvaluations,
    IReadOnlyList<string> SelectionReasons,
    IReadOnlyDictionary<string, string> Interpretations);

public sealed record RaceTacticalPlan(
    int TriggerSecond,
    WorldEntityId SupportRiderId,
    TeamRaceObservation Observation,
    RaceBriefing Briefing);

public static class ChaseDecisionEvaluator
{
    public static ChaseDecision Evaluate(TeamRaceObservation observation, RaceBriefing briefing)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(briefing);

        RaceDecisionOption selected = observation.Objective == RaceObjective.StageWin &&
                                      observation.ResourceEstimate == RaceResourceEstimate.Strong &&
                                      briefing.Kind == RaceBriefingKind.Chase
            ? RaceDecisionOption.CommitSupport
            : RaceDecisionOption.WaitForRivals;
        DecisionOptionEvaluation[] evaluations =
        {
            new(
                RaceDecisionOption.CommitSupport.ToString(),
                observation.ThreatEstimate == RaceThreatEstimate.High ? "High" : "Medium",
                "Low",
                observation.ResourceEstimate == RaceResourceEstimate.Strong ? "Medium" : "High",
                "Support unavailable later",
                RuleLegal: observation.ResourceEstimate != RaceResourceEstimate.Depleted,
                Defensible: observation.ResourceEstimate != RaceResourceEstimate.Depleted),
            new(
                RaceDecisionOption.WaitForRivals.ToString(),
                "Medium",
                observation.ThreatEstimate == RaceThreatEstimate.High ? "High" : "Medium",
                "Low",
                "Gap may grow",
                RuleLegal: true,
                Defensible: true),
            new(
                RaceDecisionOption.ProtectSecondLeader.ToString(),
                observation.Objective == RaceObjective.GeneralClassification ? "High" : "Low",
                "Medium",
                "Low",
                "Less chase capacity",
                RuleLegal: true,
                Defensible: briefing.Kind == RaceBriefingKind.Protect),
            new(
                RaceDecisionOption.TrustDs.ToString(),
                "Medium",
                "Medium",
                "Context dependent",
                "Delegates control",
                RuleLegal: true,
                Defensible: true),
        };
        string[] reasons = selected == RaceDecisionOption.CommitSupport
            ? new[] { "Stage objective is threatened", "Support resources are estimated strong" }
            : new[] { "Preserve team resources", "Rivals also have reason to react" };
        Dictionary<string, string> interpretations = new(StringComparer.Ordinal)
        {
            ["Threat"] = observation.ThreatEstimate.ToString(),
            ["ResourceState"] = observation.ResourceEstimate.ToString(),
            ["BriefingPolicy"] = briefing.Kind.ToString(),
            ["Objective"] = observation.Objective.ToString(),
        };
        return new ChaseDecision(selected, evaluations, reasons, interpretations);
    }
}

internal sealed record RaceDecisionGateResult(
    bool CreateRequest,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<RaceDecisionOption> DefensibleOptions);

internal static class RaceDecisionGate
{
    public static RaceDecisionGateResult Evaluate(
        TeamRaceObservation observation,
        RaceBriefing briefing,
        ChaseDecision decision,
        bool wasRecentlyAsked)
    {
        bool material = observation.ThreatEstimate != RaceThreatEstimate.Low;
        RaceDecisionOption[] defensibleOptions = decision.OptionEvaluations
            .Where(option => option.RuleLegal && option.Defensible)
            .Select(option => Enum.Parse<RaceDecisionOption>(option.Option))
            .Distinct()
            .ToArray();
        bool choice = defensibleOptions.Length >= 2;
        bool delegation = briefing.ConsultManager;
        bool information = observation.Confidence != RaceInformationConfidence.Low &&
                           observation.OfficialGapSeconds >= 0;
        bool novelty = !wasRecentlyAsked;
        string[] diagnostics =
        {
            $"MaterialityGate: {(material ? "PASS" : "FAIL")}",
            $"ChoiceGate: {(choice ? "PASS" : "FAIL")}",
            $"DelegationGate: {(delegation ? "PASS" : "FAIL")}",
            $"InformationGate: {(information ? "PASS" : "FAIL")}",
            $"Novelty/CooldownGate: {(novelty ? "PASS" : "FAIL")}",
        };
        return new RaceDecisionGateResult(
            material && choice && delegation && information && novelty,
            diagnostics,
            defensibleOptions);
    }
}
