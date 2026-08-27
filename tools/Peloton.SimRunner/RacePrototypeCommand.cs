using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.SimRunner;

public sealed record RacePrototypeOptions(
    string ScenarioId,
    long Seed,
    string ContentRoot,
    string? TraceJsonPath,
    string? TraceMarkdownPath);

public sealed record RacePrototypeReport(
    string Winner,
    string Checksum,
    int DecisionCount,
    bool SpyNeutral,
    bool Crashed,
    string? FailureReason);

public static class RacePrototypeCommand
{
    public const string GateScenarioAlias = "race.prototype.gate";

    public const string CanonicalScenarioId = "race-scenario.peloton.prototype-v0";

    public static string ResolveScenarioId(string scenarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        return string.Equals(scenarioId, GateScenarioAlias, StringComparison.Ordinal)
            ? CanonicalScenarioId
            : scenarioId;
    }

    public static RacePrototypeReport Execute(RacePrototypeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        try
        {
            string scenarioId = ResolveScenarioId(options.ScenarioId);
            RaceScenario scenario = new JsonRacePrototypeCatalog(options.ContentRoot).Resolve(scenarioId);
            PrototypeRaceEngine engine = new();
            RaceResult official = engine.RunBatch(scenario, options.Seed, NullWorldSpySink.Instance);
            CollectingWorldSpySink spy = new();
            RaceResult traced = engine.RunBatch(scenario, options.Seed, spy);
            bool spyNeutral = string.Equals(official.Checksum, traced.Checksum, StringComparison.Ordinal)
                && official.FinishOrder.SequenceEqual(traced.FinishOrder);
            WriteTrace(options.TraceJsonPath, RaceSpyReport.ExportJson(spy.Traces));
            WriteTrace(options.TraceMarkdownPath, RaceSpyReport.ExportMarkdown(spy.Traces));
            return new RacePrototypeReport(
                official.WinnerId.Value.ToString(CultureInfo.InvariantCulture),
                official.Checksum,
                official.DecisionCount,
                spyNeutral,
                Crashed: false,
                FailureReason: null);
        }
        catch (Exception exception) when (exception is not ArgumentException)
        {
            string reason = exception is ContentValidationException content
                ? content.IssueCode
                : exception.GetType().Name;
            return new RacePrototypeReport(
                Winner: string.Empty,
                Checksum: string.Empty,
                DecisionCount: 0,
                SpyNeutral: false,
                Crashed: true,
                FailureReason: reason);
        }
    }

    public static void Write(TextWriter output, RacePrototypeReport report)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(report);
        output.WriteLine($"winner={report.Winner}");
        output.WriteLine($"checksum={report.Checksum}");
        output.WriteLine($"decisionCount={report.DecisionCount.ToString(CultureInfo.InvariantCulture)}");
        output.WriteLine($"spyNeutral={report.SpyNeutral.ToString().ToLowerInvariant()}");
        output.WriteLine($"crashed={report.Crashed.ToString().ToLowerInvariant()}");
    }

    private static void WriteTrace(string? path, string contents)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, contents);
    }
}
