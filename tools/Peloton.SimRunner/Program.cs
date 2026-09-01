using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Peloton.Application;
using Peloton.Infrastructure;

namespace Peloton.SimRunner;

public static class Program
{
    internal const string UsageText =
        "Usage: peloton-sim run --scenario <id> --years <n> --seed <n> [--content-root <path>]"
        + "\n       peloton-sim race --scenario <id> --seed <n> [--trace-json <path>] [--trace-markdown <path>] [--content-root <path>]"
        + "\n       peloton-sim watch --scenario <id> --seed <n> [--rate <1|2|5|20>] [--trace-markdown <path>] [--content-root <path>]"
        + "\n       peloton-sim day --scenario <id> --seed <n> --days <n> [--employer <organization.wt2026.*>] [--through-races] [--follow-hub] [--simulate-from-prep] [--through-results] [--watch-from-prep] [--rate <1|2|5|20>] [--content-root <path>]"
        + "\n       peloton-sim compare --scenario <id> --seed <n> [--content-root <path>]";

    public static int Main(string[] args)
    {
        return Run(args, Console.Out, Console.Error);
    }

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        try
        {
            if (args.Length == 0 ||
                (!string.Equals(args[0], "run", StringComparison.Ordinal) &&
                 !string.Equals(args[0], "race", StringComparison.Ordinal) &&
                 !string.Equals(args[0], "watch", StringComparison.Ordinal) &&
                 !string.Equals(args[0], "day", StringComparison.Ordinal) &&
                 !string.Equals(args[0], "compare", StringComparison.Ordinal)))
            {
                throw new ArgumentException("The first argument must be 'run', 'race', 'watch', 'day', or 'compare'.", nameof(args));
            }

            if (string.Equals(args[0], "run", StringComparison.Ordinal))
            {
                return RunCareer(CareerOptions.Parse(args), output, error);
            }

            if (string.Equals(args[0], "day", StringComparison.Ordinal))
            {
                return CareerDayCommand.Execute(CareerDayOptions.Parse(args), output, error);
            }

            if (string.Equals(args[0], "compare", StringComparison.Ordinal))
            {
                return HistoricalCompareCommand.Execute(args, output, error);
            }

            return string.Equals(args[0], "race", StringComparison.Ordinal)
                ? RunRace(RaceOptions.Parse(args), output, error)
                : RunWatch(RaceOptions.Parse(args), output, error);
        }
        catch (ArgumentException exception)
        {
            error.WriteLine(exception.Message);
            error.WriteLine(UsageText);
            return 2;
        }
    }

    private static int RunCareer(CareerOptions options, TextWriter output, TextWriter error)
    {
        GameApplication application = ApplicationFactory.Create(options.ContentRoot);
        CommandResult create = application.Execute(new CreateWorldCommand(options.ScenarioId, options.Seed));
        if (!create.Succeeded)
        {
            error.WriteLine($"crashed=true reason={create.ReasonCode}");
            return 1;
        }

        string runDirectory = Path.Combine(
            Path.GetTempPath(),
            $"peloton-simrunner-{Guid.NewGuid():N}");
        try
        {
            SkeletonRunReport report = new SkeletonCareerRunner(application).Run(options.Years, runDirectory);
            output.WriteLine($"crashed={report.Crashed.ToString().ToLowerInvariant()}");
            output.WriteLine($"worldDate={report.WorldDay}");
            output.WriteLine($"checksum={report.Checksum}");
            output.WriteLine($"raceCount={report.RaceCount}");
            if (!string.IsNullOrWhiteSpace(report.FailureReason))
            {
                error.WriteLine($"reason={report.FailureReason}");
            }

            return report.Crashed ? 1 : 0;
        }
        finally
        {
            if (Directory.Exists(runDirectory))
            {
                Directory.Delete(runDirectory, recursive: true);
            }
        }
    }

    private static int RunRace(RaceOptions options, TextWriter output, TextWriter error)
    {
        RacePrototypeReport report = RacePrototypeCommand.Execute(new RacePrototypeOptions(
            options.ScenarioId,
            options.Seed,
            options.ContentRoot,
            options.TraceJsonPath,
            options.TraceMarkdownPath));
        RacePrototypeCommand.Write(output, report);
        if (!string.IsNullOrWhiteSpace(report.FailureReason))
        {
            error.WriteLine($"reason={report.FailureReason}");
        }

        return report.Crashed ? 1 : 0;
    }

    private static int RunWatch(RaceOptions options, TextWriter output, TextWriter error)
    {
        RaceWatchCliReport report = RaceWatchCommand.Execute(new RacePrototypeOptions(
            options.ScenarioId,
            options.Seed,
            options.ContentRoot,
            options.TraceJsonPath,
            options.TraceMarkdownPath), options.WatchRate);
        RaceWatchCommand.Write(output, report);
        if (!string.IsNullOrWhiteSpace(report.FailureReason))
        {
            error.WriteLine($"reason={report.FailureReason}");
        }

        return report.Crashed ? 1 : 0;
    }

    private static Dictionary<string, string> ParsePairs(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int index = 1; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Options must use '--name value' pairs.", nameof(args));
            }

            if (!values.TryAdd(args[index], args[index + 1]))
            {
                throw new ArgumentException($"Duplicate option '{args[index]}'.", nameof(args));
            }
        }

        return values;
    }

    private static string Required(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Required option '{key}' is missing.", nameof(values));
        }

        return value;
    }

    private static string DefaultContentRoot(Dictionary<string, string> values)
    {
        string contentRoot = values.TryGetValue("--content-root", out string? configuredRoot)
            ? configuredRoot
            : Path.Combine(Environment.CurrentDirectory, "content");
        return Path.GetFullPath(contentRoot);
    }

    private sealed record CareerOptions(string ScenarioId, int Years, long Seed, string ContentRoot)
    {
        public static CareerOptions Parse(string[] args)
        {
            Dictionary<string, string> values = ParsePairs(args);
            string scenario = Required(values, "--scenario");
            if (!int.TryParse(Required(values, "--years"), NumberStyles.None, CultureInfo.InvariantCulture, out int years) ||
                years <= 0)
            {
                throw new ArgumentException("--years must be a positive integer.", nameof(args));
            }

            if (!long.TryParse(Required(values, "--seed"), NumberStyles.Integer, CultureInfo.InvariantCulture, out long seed))
            {
                throw new ArgumentException("--seed must be a signed integer.", nameof(args));
            }

            return new CareerOptions(scenario, years, seed, DefaultContentRoot(values));
        }
    }

    private sealed record RaceOptions(
        string ScenarioId,
        long Seed,
        string ContentRoot,
        string? TraceJsonPath,
        string? TraceMarkdownPath,
        int WatchRate)
    {
        public static RaceOptions Parse(string[] args)
        {
            Dictionary<string, string> values = ParsePairs(args);
            string scenario = Required(values, "--scenario");
            if (!long.TryParse(Required(values, "--seed"), NumberStyles.Integer, CultureInfo.InvariantCulture, out long seed))
            {
                throw new ArgumentException("--seed must be a signed integer.", nameof(args));
            }

            int rate = 5;
            if (values.TryGetValue("--rate", out string? configuredRate) &&
                (!int.TryParse(configuredRate, NumberStyles.None, CultureInfo.InvariantCulture, out rate) ||
                 (rate != 1 && rate != 2 && rate != 5 && rate != 20)))
            {
                throw new ArgumentException("--rate must be 1, 2, 5, or 20.", nameof(args));
            }

            return new RaceOptions(
                scenario,
                seed,
                DefaultContentRoot(values),
                OptionalPath(values, "--trace-json"),
                OptionalPath(values, "--trace-markdown"),
                rate);
        }

        private static string? OptionalPath(Dictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Path.GetFullPath(value);
        }
    }
}
