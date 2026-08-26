using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public static class RaceSpyReport
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string ExportJson(IReadOnlyList<DecisionTrace> traces)
    {
        ArgumentNullException.ThrowIfNull(traces);
        return JsonSerializer.Serialize(traces, JsonOptions);
    }

    public static string ExportMarkdown(IReadOnlyList<DecisionTrace> traces)
    {
        ArgumentNullException.ThrowIfNull(traces);
        StringBuilder report = new();
        foreach (DecisionTrace trace in traces.Where(item => item.SelectedOption.Length > 0))
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"## Race decision {trace.DecisionId}");
            report.AppendLine();
            report.AppendLine(CultureInfo.InvariantCulture, $"Trigger: {trace.Trigger}");
            report.AppendLine(CultureInfo.InvariantCulture, $"Selected: {trace.SelectedOption}");
            report.AppendLine(CultureInfo.InvariantCulture, $"Confidence: {trace.Confidence}");
            report.AppendLine();
            report.AppendLine("Known inputs:");
            foreach (KeyValuePair<string, string> input in trace.ActorKnownInputs)
            {
                report.AppendLine(CultureInfo.InvariantCulture, $"- {input.Key}: {input.Value}");
            }

            report.AppendLine();
            report.AppendLine(
                CultureInfo.InvariantCulture,
                $"Reasons: {string.Join("; ", trace.SelectionReasons)}");
            report.AppendLine();
        }

        return report.ToString();
    }
}
