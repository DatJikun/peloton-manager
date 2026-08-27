using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Infrastructure;

namespace Peloton.SimRunner;

public sealed record CareerDayOptions(
    string ScenarioId,
    long Seed,
    int Days,
    string ContentRoot,
    bool ThroughRaces,
    bool FollowHub,
    bool SimulateFromPrep)
{
    public static CareerDayOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        bool throughRaces = false;
        bool followHub = false;
        bool simulateFromPrep = false;
        for (int index = 1; index < args.Length; index++)
        {
            string option = args[index];
            if (!option.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Options must start with '--'.", nameof(args));
            }

            if (string.Equals(option, "--through-races", StringComparison.Ordinal))
            {
                if (index + 1 < args.Length &&
                    !args[index + 1].StartsWith("--", StringComparison.Ordinal) &&
                    (string.Equals(args[index + 1], "true", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(args[index + 1], "false", StringComparison.OrdinalIgnoreCase)))
                {
                    throughRaces = string.Equals(args[index + 1], "true", StringComparison.OrdinalIgnoreCase);
                    index++;
                }
                else
                {
                    throughRaces = true;
                }

                continue;
            }

            if (string.Equals(option, "--follow-hub", StringComparison.Ordinal))
            {
                if (index + 1 < args.Length &&
                    !args[index + 1].StartsWith("--", StringComparison.Ordinal) &&
                    (string.Equals(args[index + 1], "true", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(args[index + 1], "false", StringComparison.OrdinalIgnoreCase)))
                {
                    followHub = string.Equals(args[index + 1], "true", StringComparison.OrdinalIgnoreCase);
                    index++;
                }
                else
                {
                    followHub = true;
                }

                continue;
            }

            if (string.Equals(option, "--simulate-from-prep", StringComparison.Ordinal))
            {
                simulateFromPrep = true;
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Option '{option}' requires a value.", nameof(args));
            }

            if (!values.TryAdd(option, args[index + 1]))
            {
                throw new ArgumentException($"Duplicate option '{option}'.", nameof(args));
            }

            index++;
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
        return new CareerDayOptions(
            scenario,
            seed,
            days,
            Path.GetFullPath(contentRoot),
            throughRaces,
            followHub,
            simulateFromPrep);
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
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";

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

        WriteHub(output, application);
        string autosaveDirectory = Path.Combine(
            Path.GetTempPath(),
            $"peloton-simrunner-day-{Guid.NewGuid():N}");
        try
        {
            for (int day = 0; day < options.Days; day++)
            {
                CommandResult advanced = application.Execute(new AdvanceDayCommand());
                if (!advanced.Succeeded &&
                    options.ThroughRaces &&
                    string.Equals(advanced.ReasonCode, "RACE_DAY_PENDING", StringComparison.Ordinal))
                {
                    CommandResult raced = RunSkeletonRace(application, autosaveDirectory);
                    if (!raced.Succeeded)
                    {
                        output.WriteLine($"stopped={raced.ReasonCode}");
                        WriteHub(output, application);
                        return 1;
                    }

                    WriteHub(output, application);
                    advanced = application.Execute(new AdvanceDayCommand());
                }

                if (!advanced.Succeeded)
                {
                    output.WriteLine($"stopped={advanced.ReasonCode}");
                    WriteHub(output, application);
                    if (string.Equals(advanced.ReasonCode, "RACE_DAY_PENDING", StringComparison.Ordinal))
                    {
                        if ((options.FollowHub || options.SimulateFromPrep) && !options.ThroughRaces)
                        {
                            CommandResult follow = application.Execute(new FollowHubPrimaryActionCommand());
                            if (!follow.Succeeded)
                            {
                                return 1;
                            }

                            output.WriteLine($"state={application.State}");
                            WritePreparation(output, application);
                            WriteHub(output, application);
                            if (options.SimulateFromPrep)
                            {
                                CommandResult confirm = application.Execute(new ConfirmRacePreparationPlanCommand());
                                if (!confirm.Succeeded)
                                {
                                    return 1;
                                }

                                CommandResult simulate = application.Execute(new SimulateRaceCommand(
                                    PrototypeRaceScenarioId));
                                if (!simulate.Succeeded)
                                {
                                    return 1;
                                }

                                output.WriteLine($"state={application.State}");
                                output.WriteLine($"winner={application.World!.LastRace!.WinnerId.Value.ToString(CultureInfo.InvariantCulture)}");
                            }
                        }

                        return 0;
                    }

                    return 1;
                }

                WriteHub(output, application);
            }
        }
        finally
        {
            if (Directory.Exists(autosaveDirectory))
            {
                Directory.Delete(autosaveDirectory, recursive: true);
            }
        }

        output.WriteLine("crashed=false");
        return 0;
    }

    private static CommandResult RunSkeletonRace(GameApplication application, string autosaveDirectory)
    {
        Directory.CreateDirectory(autosaveDirectory);
        CommandResult prepare = application.Execute(new FollowHubPrimaryActionCommand());
        if (!prepare.Succeeded)
        {
            return prepare;
        }

        CommandResult confirm = application.Execute(new ConfirmRacePreparationPlanCommand());
        if (!confirm.Succeeded)
        {
            return confirm;
        }

        CommandResult simulate = application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId));
        if (!simulate.Succeeded)
        {
            return simulate;
        }

        CommandResult acknowledge = application.Execute(new AcknowledgeRaceResultsCommand());
        if (!acknowledge.Succeeded)
        {
            return acknowledge;
        }

        return application.Execute(new CompleteRaceDebriefCommand());
    }

    private static void WritePreparation(TextWriter output, GameApplication application)
    {
        RacePreparationProjection? prep = application.RacePreparation;
        if (prep is null)
        {
            return;
        }

        string squad = string.Join(",", prep.Squad.Select(id => id.Value.ToString(CultureInfo.InvariantCulture)));
        output.WriteLine(
            $"prep=title={prep.Title} objective={prep.Objective} squad={squad} planConfirmed={prep.PlanConfirmed.ToString().ToLowerInvariant()} canStart={prep.CanStart.ToString().ToLowerInvariant()} canSimulate={prep.CanSimulate.ToString().ToLowerInvariant()}");
    }

    private static void WriteHub(TextWriter output, GameApplication application)
    {
        CareerDayProjection? hub = application.CareerDay;
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
        output.WriteLine($"primaryAction={hub.PrimaryAction}");
        output.WriteLine($"primaryLabel={hub.PrimaryLabel}");
        output.WriteLine($"raceCount={hub.RaceCount.ToString(CultureInfo.InvariantCulture)}");
        foreach (string note in hub.TodayNotes)
        {
            output.WriteLine($"note={note}");
        }

        foreach (CalendarEntryProjection entry in application.Calendar)
        {
            if (entry.OfficialResult is null)
            {
                output.WriteLine(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"calendar=day={entry.DayNumber} kind={entry.Kind} status={entry.Status} title={entry.Title}"));
            }
            else
            {
                output.WriteLine(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"calendar=day={entry.DayNumber} kind={entry.Kind} status={entry.Status} title={entry.Title} result={entry.OfficialResult}"));
            }
        }

        IReadOnlyList<InboxItemProjection> inbox = application.Inbox;
        output.WriteLine($"inboxCount={inbox.Count.ToString(CultureInfo.InvariantCulture)}");
        foreach (InboxItemProjection item in inbox)
        {
            output.WriteLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"inbox=identity={item.Identity} category={item.Category} day={item.DayNumber} {item.Body}"));
        }
    }
}
