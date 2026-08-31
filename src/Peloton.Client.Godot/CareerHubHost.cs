using System;
using System.Collections.Generic;
using System.IO;
using Peloton.Application;
using Peloton.Domain;

namespace Peloton.Client.Godot;

public sealed class CareerHubHost
{
    private const string SettingsFileName = "presentation-settings.txt";

    private readonly GameApplication application;
    private readonly string autosavePath;
    private readonly string settingsPath;

    public CareerHubHost(GameApplication application, string autosavePath)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        ArgumentException.ThrowIfNullOrWhiteSpace(autosavePath);
        this.autosavePath = autosavePath;
        string directory = Path.GetDirectoryName(autosavePath) ?? ".";
        settingsPath = Path.Combine(directory, SettingsFileName);
        Settings = LoadSettings(settingsPath);
    }

    public GameState State => application.State;

    public PresentationSettings Settings { get; private set; }

    public CareerDayProjection? Day => application.CareerDay;

    public IReadOnlyList<CalendarEntryProjection> Calendar => application.Calendar;

    public IReadOnlyList<InboxItemProjection> Inbox => application.Inbox;

    public RacePreparationProjection? Preparation => application.RacePreparation;

    public RaceResultProjection? Result => application.RaceResult;

    public RaceDebriefProjection? Debrief => application.RaceDebrief;

    public WatchRaceHost? Watch { get; private set; }

    public CommandResult Open(long seed)
    {
        return application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", seed));
    }

    public CommandResult AdvanceDay()
    {
        return application.Execute(new AdvanceDayCommand());
    }

    public CommandResult FollowPrimary()
    {
        return application.Execute(new FollowHubPrimaryActionCommand());
    }

    public CommandResult ArchiveInbox(string identity)
    {
        return application.Execute(new ArchiveInboxItemCommand(identity));
    }

    public CommandResult AssignRole(WorldEntityId riderId, string role)
    {
        return application.Execute(new AssignSquadRoleCommand(riderId, role));
    }

    public CommandResult ConfirmPreparation()
    {
        return application.Execute(new ConfirmRacePreparationPlanCommand());
    }

    public void SetWatchFilmEnabled(bool enabled)
    {
        Settings = new PresentationSettings(enabled);
        string? directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(settingsPath, enabled ? "true" : "false");
    }

    public CommandResult RunRace()
    {
        return Settings.WatchFilmEnabled ? OpenWatch() : SimulateRace();
    }

    public CommandResult SimulateRace()
    {
        CommandResult prepared = EnsureConfirmedPreparation();
        if (!prepared.Succeeded)
        {
            return prepared;
        }

        return application.Execute(new SimulateRaceCommand(RacePreparationDefaults.PrototypeScenarioId));
    }

    public CommandResult OpenWatch()
    {
        CommandResult prepared = EnsureConfirmedPreparation();
        if (!prepared.Succeeded)
        {
            return prepared;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(autosavePath) ?? ".");
        Watch = new WatchRaceHost(application, autosavePath);
        return Watch.StartWatch();
    }

    public CommandResult TickWatch(double realDeltaSeconds)
    {
        return Watch is null ? CommandResult.Success : Watch.Tick(realDeltaSeconds);
    }

    public CommandResult ContinueOutcome()
    {
        if (Watch is not null)
        {
            return FinishWatchResults();
        }

        if (application.State == GameState.RaceResultsFlow)
        {
            return application.Execute(new AcknowledgeRaceResultsCommand());
        }

        if (application.State == GameState.RaceDebriefFlow)
        {
            return application.Execute(new CompleteRaceDebriefCommand());
        }

        return CommandResult.Reject("GAME_STATE_INVALID");
    }

    public CommandResult FinishWatchResults()
    {
        if (Watch is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        if (application.State == GameState.RaceResultsFlow)
        {
            CommandResult acknowledged = Watch.AcknowledgeResults();
            if (!acknowledged.Succeeded)
            {
                return acknowledged;
            }
        }

        if (application.State == GameState.RaceDebriefFlow)
        {
            CommandResult done = Watch.CompleteDebrief();
            Watch = null;
            return done;
        }

        return CommandResult.Reject("GAME_STATE_INVALID");
    }

    private CommandResult EnsureConfirmedPreparation()
    {
        if (application.State == GameState.Management)
        {
            CommandResult entered = FollowPrimary();
            if (!entered.Succeeded)
            {
                return entered;
            }
        }

        if (application.State != GameState.RacePreparationFlow)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        if (application.RacePreparation is { PlanConfirmed: false })
        {
            return ConfirmPreparation();
        }

        return CommandResult.Success;
    }

    private static PresentationSettings LoadSettings(string path)
    {
        if (!File.Exists(path))
        {
            return PresentationSettings.Default;
        }

        string text = File.ReadAllText(path).Trim();
        return new PresentationSettings(string.Equals(text, "true", StringComparison.OrdinalIgnoreCase));
    }
}
