using System;
using System.Collections.Generic;
using Peloton.Application;
using Peloton.Domain;

namespace Peloton.Client.Godot;

public sealed class CareerShellHost
{
    public const long SkeletonSeed = 91234;
    public const string SkeletonScenarioId = "scenario.peloton.skeleton";

    private readonly GameApplication application;
    private readonly string savePath;
    private readonly string preraceAutosavePath;

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
        PreferredWatchRate = 5;
    }

    public int PreferredWatchRate { get; private set; }

    public GameState State => application.State;

    public CareerDayProjection? Day => application.CareerDay;

    public IReadOnlyList<CalendarEntryProjection> Calendar => application.Calendar;

    public IReadOnlyList<InboxItemProjection> Inbox => application.Inbox;

    public IReadOnlyList<PersonNameProjection> People => application.People;

    public IReadOnlyList<OrganizationNameProjection> Organizations => application.Organizations;

    public RacePreparationProjection? Preparation => application.RacePreparation;

    public CommandResult OpenSkeleton(long seed = SkeletonSeed)
    {
        return application.Execute(new CreateWorldCommand(SkeletonScenarioId, seed));
    }

    public CommandResult FollowPrimary()
    {
        return application.Execute(new FollowHubPrimaryActionCommand());
    }

    public CommandResult ArchiveInbox(string identity)
    {
        return application.Execute(new ArchiveInboxItemCommand(identity));
    }

    public CommandResult Save()
    {
        return application.Execute(new SaveGameCommand(savePath));
    }

    public CommandResult Load()
    {
        return application.Execute(new LoadGameCommand(savePath));
    }

    public CommandResult SelectWatchRate(int rate)
    {
        if (rate is not (1 or 2 or 5 or 20))
        {
            return CommandResult.Reject("WATCH_RATE_INVALID");
        }

        PreferredWatchRate = rate;
        return CommandResult.Success;
    }

    public WatchRaceHost CreateWatchHost()
    {
        WatchRaceHost watch = new(application, preraceAutosavePath);
        watch.SelectRate(PreferredWatchRate);
        return watch;
    }
}
