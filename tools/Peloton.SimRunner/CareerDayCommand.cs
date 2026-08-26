using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Peloton.Application;
using Peloton.Infrastructure;

namespace Peloton.SimRunner;

public sealed record CareerDayOptions(string ScenarioId, long Seed, int Days, string ContentRoot)
{
    public static CareerDayOptions Parse(string[] args)
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

        if (!values.TryGetValue("--scenario", out string? scenario) || string.IsNullOrWhiteSpace(scenario))
        {
            throw new ArgumentException("Required option '--scenario' is missing.", nameof(args));
        }

        if (!int.TryParse(
                Required(values, "--days"),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int days) ||
            days <= 0)
        {
            throw new ArgumentException("--days must be a positive integer.", nameof(args));
        }

        if (!long.TryParse(
                Required(values, "--seed"),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long seed))
        {
            throw new ArgumentException("--seed must be a signed integer.", nameof(args));
        }

        string contentRoot = values.TryGetValue("--content-root", out string? configuredRoot)
            ? configuredRoot
            : Path.Combine(Environment.CurrentDirectory, "content");
        return new CareerDayOptions(scenario, seed, days, Path.GetFullPath(contentRoot));
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

public static class CareerDayCommand
{
    public static int Execute(CareerDayOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        GameApplication application = ApplicationFactory.Create(options.ContentRoot);
        CommandResult create = application.Execute(new CreateWorldCommand(options.ScenarioId, options.Seed));
        if (!create.Succeeded)
        {
            error.WriteLine($"crashed=true reason={create.ReasonCode}");
            return 1;
        }

        WriteHub(output, application.CareerDay);
        for (int day = 0; day < options.Days; day++)
        {
            CommandResult advanced = application.Execute(new AdvanceDayCommand());
            if (!advanced.Succeeded)
            {
                output.WriteLine($"stopped={advanced.ReasonCode}");
                WriteHub(output, application.CareerDay);
                return string.Equals(advanced.ReasonCode, "RACE_DAY_PENDING", StringComparison.Ordinal) ? 0 : 1;
            }

            WriteHub(output, application.CareerDay);
        }

        output.WriteLine("crashed=false");
        return 0;
    }

    private static void WriteHub(TextWriter output, CareerDayProjection? hub)
    {
        if (hub is null)
        {
            output.WriteLine("hub=missing");
            return;
        }

        output.WriteLine($"day={hub.DayNumber.ToString(CultureInfo.InvariantCulture)}");
        output.WriteLine($"employer={hub.EmployerName}");
        output.WriteLine($"nextRaceDay={hub.NextRaceDayNumber.ToString(CultureInfo.InvariantCulture)}");
        output.WriteLine($"daysUntilRace={hub.DaysUntilNextRace.ToString(CultureInfo.InvariantCulture)}");
        output.WriteLine($"raceDue={hub.RaceDueToday.ToString().ToLowerInvariant()}");
        output.WriteLine($"raceCount={hub.RaceCount.ToString(CultureInfo.InvariantCulture)}");
        foreach (string note in hub.TodayNotes)
        {
            output.WriteLine($"note={note}");
        }
    }
}
