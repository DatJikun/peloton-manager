using System;
using System.Collections.Generic;
using System.IO;
using Peloton.Application;
using Peloton.Domain;

namespace Peloton.Client.Godot;

public sealed class CareerHubHost
{
    private readonly GameApplication application;
    private readonly string autosavePath;

    public CareerHubHost(GameApplication application, string autosavePath)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        ArgumentException.ThrowIfNullOrWhiteSpace(autosavePath);
        this.autosavePath = autosavePath;
    }

    public GameState State => application.State;

    public CareerDayProjection? Day => application.CareerDay;

    public IReadOnlyList<CalendarEntryProjection> Calendar => application.Calendar;

    public IReadOnlyList<InboxItemProjection> Inbox => application.Inbox;

    public RacePreparationProjection? Preparation => application.RacePreparation;

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

    public CommandResult OpenWatch()
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
            CommandResult confirmed = ConfirmPreparation();
            if (!confirmed.Succeeded)
            {
                return confirmed;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(autosavePath) ?? ".");
        Watch = new WatchRaceHost(application, autosavePath);
        return Watch.StartWatch();
    }

    public CommandResult TickWatch(double realDeltaSeconds)
    {
        return Watch is null ? CommandResult.Success : Watch.Tick(realDeltaSeconds);
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
}
