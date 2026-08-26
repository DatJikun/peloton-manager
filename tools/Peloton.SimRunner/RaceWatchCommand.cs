using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.SimRunner;

public static class RaceWatchCommand
{
    public static RaceWatchCliReport Execute(RacePrototypeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        try
        {
            string scenarioId = RacePrototypeCommand.ResolveScenarioId(options.ScenarioId);
            RaceScenario scenario = new JsonRacePrototypeCatalog(options.ContentRoot).Resolve(scenarioId);
            RaceWatchReport watch = RaceWatchProjector.Project(scenario, options.Seed);
            RaceResult traced = new PrototypeRaceEngine().RunBatch(
                scenario,
                options.Seed,
                new CollectingWorldSpySink());
            bool spyNeutral = string.Equals(watch.Result.Checksum, traced.Checksum, StringComparison.Ordinal)
                && watch.Result.FinishOrder.SequenceEqual(traced.FinishOrder);
            string markdown = RaceWatchProjector.ExportMarkdown(watch);
            if (!string.IsNullOrWhiteSpace(options.TraceMarkdownPath))
            {
                string? directory = Path.GetDirectoryName(options.TraceMarkdownPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(options.TraceMarkdownPath, markdown);
            }

            return new RaceWatchCliReport(
                watch,
                watch.Result.WinnerId.Value.ToString(CultureInfo.InvariantCulture),
                watch.Result.Checksum,
                watch.Result.DecisionCount,
                spyNeutral,
                Crashed: false,
                FailureReason: null);
        }
        catch (Exception exception) when (exception is not ArgumentException)
        {
            string reason = exception is ContentValidationException content
                ? content.IssueCode
                : exception.GetType().Name;
            return new RaceWatchCliReport(
                Watch: null,
                Winner: string.Empty,
                Checksum: string.Empty,
                DecisionCount: 0,
                SpyNeutral: false,
                Crashed: true,
                FailureReason: reason);
        }
    }

    public static void Write(TextWriter output, RaceWatchCliReport report)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(report);
        if (report.Watch is not null)
        {
            foreach (RaceWatchBeat beat in report.Watch.Beats)
            {
                output.WriteLine(
                    $"beat watchSecond={beat.WatchSecond.ToString(CultureInfo.InvariantCulture)} simSecond={beat.SimulationSecond.ToString(CultureInfo.InvariantCulture)} kind={beat.Kind} headline={beat.Headline}");
            }
        }

        output.WriteLine($"winner={report.Winner}");
        output.WriteLine($"checksum={report.Checksum}");
        output.WriteLine($"decisionCount={report.DecisionCount.ToString(CultureInfo.InvariantCulture)}");
        output.WriteLine($"spyNeutral={report.SpyNeutral.ToString().ToLowerInvariant()}");
        output.WriteLine($"crashed={report.Crashed.ToString().ToLowerInvariant()}");
        int watchBeats = 0;
        int simulationSeconds = 0;
        if (report.Watch is not null && report.Watch.Beats.Count > 0)
        {
            watchBeats = report.Watch.Beats.Count;
            simulationSeconds = report.Watch.Beats[^1].SimulationSecond;
        }
        output.WriteLine($"watchBeats={watchBeats.ToString(CultureInfo.InvariantCulture)}");
        output.WriteLine($"simulationSeconds={simulationSeconds.ToString(CultureInfo.InvariantCulture)}");
    }
}

public sealed record RaceWatchCliReport(
    RaceWatchReport? Watch,
    string Winner,
    string Checksum,
    int DecisionCount,
    bool SpyNeutral,
    bool Crashed,
    string? FailureReason);
