using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Infrastructure;
using Peloton.Simulation.Race;

namespace Peloton.SimRunner;

public sealed record CareerDayOptions(
    string ScenarioId,
    long Seed,
    int Days,
    string ContentRoot,
    bool ThroughRaces,
    bool FollowHub,
    bool SimulateFromPrep,
    bool ThroughResults,
    bool WatchFromPrep,
    int WatchRate)
{
    public static CareerDayOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        bool throughRaces = false;
        bool followHub = false;
        bool simulateFromPrep = false;
        bool throughResults = false;
        bool watchFromPrep = false;
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

            if (string.Equals(option, "--through-results", StringComparison.Ordinal))
            {
                throughResults = true;
                continue;
            }

            if (string.Equals(option, "--watch-from-prep", StringComparison.Ordinal))
            {
                watchFromPrep = true;
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
        int watchRate = 5;
        if (values.TryGetValue("--rate", out string? configuredRate) &&
            (!int.TryParse(configuredRate, NumberStyles.None, CultureInfo.InvariantCulture, out watchRate) ||
             (watchRate != 1 && watchRate != 2 && watchRate != 5 && watchRate != 20)))
        {
            throw new ArgumentException("--rate must be 1, 2, 5, or 20.", nameof(args));
        }

        return new CareerDayOptions(
            scenario,
            seed,
            days,
            Path.GetFullPath(contentRoot),
            throughRaces,
            followHub,
            simulateFromPrep,
            throughResults,
            watchFromPrep,
            watchRate);
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
                        if ((options.FollowHub ||
                             options.SimulateFromPrep ||
                             options.ThroughResults ||
                             options.WatchFromPrep) &&
                            !options.ThroughRaces)
                        {
                            CommandResult follow = application.Execute(new FollowHubPrimaryActionCommand());
                            if (!follow.Succeeded)
                            {
                                return 1;
                            }

                            output.WriteLine($"state={application.State}");
                            WritePreparation(output, application);
                            if (options.WatchFromPrep)
                            {
                                return WatchFromPreparation(application, options, autosaveDirectory, output);
                            }

                            if (options.SimulateFromPrep || options.ThroughResults)
                            {
                                CommandResult confirm = RacePreparationSupport.ConfirmWithDefaultStrategy(application);
                                if (!confirm.Succeeded)
                                {
                                    return 1;
                                }

                                CommandResult simulate = application.Execute(new SimulateRaceCommand(
                                    ResolveCurrentRaceContentId(application)));
                                if (!simulate.Succeeded)
                                {
                                    return 1;
                                }

                                output.WriteLine($"state={application.State}");
                                WriteResult(output, application);
                                output.WriteLine($"winner={application.World!.LastRace!.WinnerId.Value.ToString(CultureInfo.InvariantCulture)}");
                                if (options.ThroughResults)
                                {
                                    CommandResult acknowledge = application.Execute(new AcknowledgeRaceResultsCommand());
                                    if (!acknowledge.Succeeded)
                                    {
                                        return 1;
                                    }

                                    output.WriteLine($"state={application.State}");
                                    WriteDebrief(output, application);
                                }
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

    private static string ResolveCurrentRaceContentId(GameApplication application) =>
        application.World?.TryGetTodaysRaceContentId() ?? PrototypeRaceScenarioId;

    private static CommandResult RunSkeletonRace(GameApplication application, string autosaveDirectory)
    {
        Directory.CreateDirectory(autosaveDirectory);
        CommandResult prepare = application.Execute(new FollowHubPrimaryActionCommand());
        if (!prepare.Succeeded)
        {
            return prepare;
        }

        CommandResult confirm = RacePreparationSupport.ConfirmWithDefaultStrategy(application);
        if (!confirm.Succeeded)
        {
            return confirm;
        }

        CommandResult simulate = application.Execute(new SimulateRaceCommand(
            ResolveCurrentRaceContentId(application)));
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

    private static int WatchFromPreparation(
        GameApplication application,
        CareerDayOptions options,
        string autosaveDirectory,
        TextWriter output)
    {
        CommandResult confirm = RacePreparationSupport.ConfirmWithDefaultStrategy(application);
        if (!confirm.Succeeded)
        {
            return 1;
        }

        Directory.CreateDirectory(autosaveDirectory);
        CommandResult start = application.Execute(new StartRaceCommand(
            Path.Combine(autosaveDirectory, "watch-pre-race.peloton"),
            ResolveCurrentRaceContentId(application)));
        if (!start.Succeeded)
        {
            return 1;
        }

        CommandResult beginWatch = application.Execute(new BeginRaceWatchCommand(options.WatchRate));
        if (!beginWatch.Succeeded)
        {
            return 1;
        }

        output.WriteLine($"state={application.State}");
        output.WriteLine($"rate={options.WatchRate.ToString(CultureInfo.InvariantCulture)}");
        WriteWatchFrame(output, application);
        while (application.State == GameState.RaceLive)
        {
            if (application.PendingRaceDecision is PendingRaceDecision decision)
            {
                WriteWatchFrame(output, application);
                CommandResult responded = application.Execute(new RespondToRaceDecisionCommand(
                    decision.RequestId,
                    decision.AuthorityId,
                    decision.DelegatedDefaultOption));
                if (!responded.Succeeded)
                {
                    return 1;
                }

                continue;
            }

            CommandResult advanced = application.Execute(new AdvanceRaceWatchCommand());
            if (!advanced.Succeeded)
            {
                return 1;
            }

            if (application.State == GameState.RaceLive)
            {
                RaceWatchFrame? frame = application.RaceWatch;
                if (frame is not null &&
                    (frame.Paused || frame.WatchSecond == 0 || frame.WatchSecond % 60 == 0))
                {
                    WriteWatchFrame(output, application);
                }
            }
        }

        output.WriteLine($"watchSecond={application.LastWatchSecond.ToString(CultureInfo.InvariantCulture)}");
        output.WriteLine($"simSecond={application.LastSimSecond.ToString(CultureInfo.InvariantCulture)}");
        output.WriteLine($"state={application.State}");
        WriteResult(output, application);
        output.WriteLine($"winner={application.World!.LastRace!.WinnerId.Value.ToString(CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrWhiteSpace(application.LastOfficialChecksum))
        {
            output.WriteLine($"checksum={application.LastOfficialChecksum}");
        }

        return 0;
    }

    private static void WriteWatchFrame(TextWriter output, GameApplication application)
    {
        RaceWatchFrame? frame = application.RaceWatch;
        if (frame is null)
        {
            return;
        }

        output.WriteLine(
            $"frame rate={frame.Rate.ToString(CultureInfo.InvariantCulture)} watchSecond={frame.WatchSecond.ToString(CultureInfo.InvariantCulture)} simSecond={frame.RaceSecond.ToString(CultureInfo.InvariantCulture)} paused={frame.Paused.ToString().ToLowerInvariant()}");
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

    private static void WriteResult(TextWriter output, GameApplication application)
    {
        RaceResultProjection? result = application.RaceResult;
        if (result is null)
        {
            return;
        }

        string finishOrder = string.Join(",", result.FinishOrder.Select(place => place.Label));
        output.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"result=title={result.Title} winner={result.WinnerId.Value} winnerLabel={result.WinnerLabel} routeId={result.RouteId} finishOrder={finishOrder}"));

        AccessContext access = application.GetAccessContext();
        if (access.CurrentOrganizationId is WorldEntityId organizationId)
        {
            IReadOnlyList<RaceResultPlacement>? teamResults = application.RaceResultForOrganization(organizationId);
            if (teamResults is not null && teamResults.Count > 0)
            {
                string teamFinish = string.Join(
                    ",",
                    teamResults.Select(place =>
                        string.Create(CultureInfo.InvariantCulture, $"{place.Place}:{place.Label}")));
                output.WriteLine(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"resultTeam=org={organizationId.Value} finishOrder={teamFinish}"));
            }
        }
    }

    private static void WriteDebrief(TextWriter output, GameApplication application)
    {
        RaceDebriefProjection? debrief = application.RaceDebrief;
        if (debrief is null)
        {
            return;
        }

        output.WriteLine($"debrief=objective={debrief.Objective} notes={string.Join(" ", debrief.Notes)}");
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
        ClubFinanceProjection? finance = application.ClubFinance;
        if (finance is not null)
        {
            output.WriteLine($"cash={finance.CashEur.ToString(CultureInfo.InvariantCulture)}");
            output.WriteLine($"overdrawn={finance.Overdrawn.ToString().ToLowerInvariant()}");
        }

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
