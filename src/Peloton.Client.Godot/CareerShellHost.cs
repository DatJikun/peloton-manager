using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;

namespace Peloton.Client.Godot;

public sealed class CareerShellHost
{
    public const long SkeletonSeed = 91234;
    public const string SkeletonScenarioId = "scenario.peloton.skeleton";
    private const string SettingsFileName = "presentation-settings.txt";

    private readonly GameApplication application;
    private readonly string savePath;
    private readonly string preraceAutosavePath;
    private readonly string settingsPath;
    private WorldEntityId? resultTeamFilter;

    public CareerShellHost(
        GameApplication application,
        string savePath,
        string preraceAutosavePath)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        ArgumentException.ThrowIfNullOrWhiteSpace(savePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(preraceAutosavePath);
        this.savePath = savePath;
        this.preraceAutosavePath = preraceAutosavePath;
        string directory = Path.GetDirectoryName(savePath) ?? ".";
        settingsPath = Path.Combine(directory, SettingsFileName);
        Settings = LoadSettings(settingsPath);
    }

    public GameState State => application.State;

    public PresentationSettings Settings { get; private set; }

    public CareerDayProjection? Day => application.CareerDay;

    public IReadOnlyList<CalendarEntryProjection> Calendar => application.Calendar;

    public IReadOnlyList<InboxItemProjection> Inbox => application.Inbox;

    public IReadOnlyList<PersonNameProjection> People => application.People;

    public IReadOnlyList<OrganizationNameProjection> Organizations => application.Organizations;

    public RacePreparationProjection? Preparation => application.RacePreparation;

    public RaceResultProjection? Result => application.RaceResult;

    public RaceDebriefProjection? Debrief => application.RaceDebrief;

    public WorldEntityId? ResultTeamFilter => resultTeamFilter;

    public IReadOnlyList<RaceResultPlacement> VisibleResultTable =>
        Result is null
            ? Array.Empty<RaceResultPlacement>()
            : RaceOutcomeQueries.FilterPlacements(Result, resultTeamFilter);

    public WatchRaceHost? Watch { get; private set; }

    public CommandResult OpenSkeleton(long seed = SkeletonSeed)
    {
        resultTeamFilter = null;
        Watch = null;
        return application.Execute(new CreateWorldCommand(SkeletonScenarioId, seed));
    }

    public void SetResultTeamFilter(WorldEntityId? teamId)
    {
        if (teamId is { } id && (Result is null || Result.Teams.All(team => team.Id != id)))
        {
            return;
        }

        resultTeamFilter = teamId;
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

    public CommandResult CancelPreparation()
    {
        Watch = null;
        return application.Execute(new CancelRacePreparationCommand());
    }

    public CommandResult Save()
    {
        return application.Execute(new SaveGameCommand(savePath));
    }

    public CommandResult Load()
    {
        resultTeamFilter = null;
        Watch = null;
        return application.Execute(new LoadGameCommand(savePath));
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

        CommandResult raced = application.Execute(
            new SimulateRaceCommand(RacePreparationDefaults.PrototypeScenarioId));
        if (raced.Succeeded)
        {
            PruneResultTeamFilter();
        }

        return raced;
    }

    public CommandResult OpenWatch()
    {
        CommandResult prepared = EnsureConfirmedPreparation();
        if (!prepared.Succeeded)
        {
            return prepared;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(preraceAutosavePath) ?? ".");
        Watch = new WatchRaceHost(application, preraceAutosavePath);
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

    private void PruneResultTeamFilter()
    {
        if (resultTeamFilter is { } id && (Result is null || Result.Teams.All(team => team.Id != id)))
        {
            resultTeamFilter = null;
        }
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
