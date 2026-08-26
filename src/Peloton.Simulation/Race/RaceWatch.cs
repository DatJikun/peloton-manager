using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public sealed record RaceWatchBeat(
    int WatchSecond,
    int SimulationSecond,
    string Kind,
    string Headline,
    IReadOnlyList<string> Options,
    string? Selected);

public sealed record RaceWatchReport(
    IReadOnlyList<RaceWatchBeat> Beats,
    RaceResult Result);

public static class RaceWatchProjector
{
    public static RaceWatchReport Project(RaceScenario scenario, long seed)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        RaceSession session = new PrototypeRaceEngine().CreateSession(
            scenario,
            seed,
            NullWorldSpySink.Instance);
        List<RaceWatchBeat> beats = new()
        {
            new(0, 0, "start", "Race start", Array.Empty<string>(), null),
        };
        int watchSecond = 0;
        while (!session.IsCompleted)
        {
            RaceStepResult step = session.Step();
            if (step.Status != RaceStepStatus.DecisionRequired)
            {
                continue;
            }

            RaceDecisionRequest request = session.PendingDecision
                ?? throw new InvalidOperationException("Decision pause did not expose a request.");
            watchSecond++;
            string[] options = request.DefensibleOptions
                .Select(option => option.ToString())
                .ToArray();
            beats.Add(new RaceWatchBeat(
                watchSecond,
                request.RaceSecond,
                "decision",
                request.Trigger,
                options,
                request.DelegatedDefaultOption.ToString()));
            session.ResolveDecision(new RaceDecisionResolution(
                request.Id,
                request.AuthorityId,
                request.DelegatedDefaultOption));
        }

        RaceResult result = session.Result
            ?? throw new InvalidOperationException("Watch projection completed without an official result.");
        watchSecond++;
        beats.Add(new RaceWatchBeat(
            watchSecond,
            session.SimulationSecond,
            "finish",
            $"Winner {result.WinnerId.Value.ToString(CultureInfo.InvariantCulture)}",
            Array.Empty<string>(),
            null));
        return new RaceWatchReport(beats, result);
    }

    public static string ExportMarkdown(RaceWatchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        StringBuilder markdown = new();
        markdown.AppendLine("# Scaled race watch");
        markdown.AppendLine();
        markdown.AppendLine(
            CultureInfo.InvariantCulture,
            $"Official winner {report.Result.WinnerId.Value}; checksum `{report.Result.Checksum}`.");
        markdown.AppendLine(
            "Watch seconds skip quiet physics. Simulation seconds remain the canonical race clock.");
        markdown.AppendLine();
        foreach (RaceWatchBeat beat in report.Beats)
        {
            markdown.AppendLine(
                CultureInfo.InvariantCulture,
                $"## Watch {beat.WatchSecond}s / sim {beat.SimulationSecond}s — {beat.Kind}");
            markdown.AppendLine();
            markdown.AppendLine(beat.Headline);
            if (beat.Options.Count > 0)
            {
                markdown.AppendLine();
                markdown.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"Options: {string.Join(", ", beat.Options)}");
                markdown.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"Selected: {beat.Selected}");
            }

            markdown.AppendLine();
        }

        return markdown.ToString();
    }
}
