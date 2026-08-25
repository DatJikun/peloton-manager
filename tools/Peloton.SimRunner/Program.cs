using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Peloton.Application;
using Peloton.Infrastructure;

namespace Peloton.SimRunner;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            RunnerOptions options = RunnerOptions.Parse(args);
            GameApplication application = ApplicationFactory.Create(options.ContentRoot);
            CommandResult create = application.Execute(new CreateWorldCommand(options.ScenarioId, options.Seed));
            if (!create.Succeeded)
            {
                Console.Error.WriteLine($"crashed=true reason={create.ReasonCode}");
                return 1;
            }

            string runDirectory = Path.Combine(
                Path.GetTempPath(),
                $"peloton-simrunner-{Guid.NewGuid():N}");
            try
            {
                SkeletonRunReport report = new SkeletonCareerRunner(application).Run(options.Years, runDirectory);
                Console.WriteLine($"crashed={report.Crashed.ToString().ToLowerInvariant()}");
                Console.WriteLine($"worldDate={report.WorldDay}");
                Console.WriteLine($"checksum={report.Checksum}");
                Console.WriteLine($"raceCount={report.RaceCount}");
                if (!string.IsNullOrWhiteSpace(report.FailureReason))
                {
                    Console.Error.WriteLine($"reason={report.FailureReason}");
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
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine(
                "Usage: peloton-sim run --scenario <id> --years <n> --seed <n> [--content-root <path>]");
            return 2;
        }
    }

    private sealed record RunnerOptions(string ScenarioId, int Years, long Seed, string ContentRoot)
    {
        public static RunnerOptions Parse(string[] args)
        {
            if (args.Length == 0 || !string.Equals(args[0], "run", StringComparison.Ordinal))
            {
                throw new ArgumentException("The first argument must be 'run'.", nameof(args));
            }

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

            string contentRoot = values.TryGetValue("--content-root", out string? configuredRoot)
                ? configuredRoot
                : Path.Combine(Environment.CurrentDirectory, "content");
            return new RunnerOptions(scenario, years, seed, Path.GetFullPath(contentRoot));
        }

        private static string Required(Dictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Required option '{key}' is missing.", nameof(values));
            }

            return value;
        }
    }
}
