using System;
using System.Collections.Generic;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public sealed class WatchRaceHost
{
    public const double WatchSecondDuration = 1.0;

    private readonly GameApplication application;
    private readonly string autosavePath;
    private readonly string raceScenarioId;
    private readonly List<long> squadIds = new();
    private RaceWatchFrame? previousFrame;
    private double watchAccumulator;
    private bool presentationPaused;

    public WatchRaceHost(
        GameApplication application,
        string autosavePath,
        string? raceScenarioId = null)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        ArgumentException.ThrowIfNullOrWhiteSpace(autosavePath);
        this.autosavePath = autosavePath;
        this.raceScenarioId = string.IsNullOrWhiteSpace(raceScenarioId)
            ? RacePreparationDefaults.PrototypeScenarioId
            : raceScenarioId;
        SelectedFilmSeconds = WatchFilmDuration.DefaultSeconds;
        ExpectedFilmSeconds = WatchFilmDuration.DefaultSeconds;
        DsAutonomy = false;
    }

    public int SelectedFilmSeconds { get; private set; }

    public int ExpectedFilmSeconds { get; private set; }

    public bool DsAutonomy { get; private set; }

    public IReadOnlyList<long> SquadIds => squadIds;

    public int SelectedRate { get; private set; }

    public GameState State => application.State;

    public RaceWatchFrame? OfficialFrame => application.RaceWatch;

    public RaceWatchCourse? Course => application.RaceWatchCourse;

    public PendingRaceDecision? PendingDecision =>
        DsAutonomy ? null : application.PendingRaceDecision;

    public RaceResultProjection? Result => application.RaceResult;

    public RaceDebriefProjection? Debrief => application.RaceDebrief;

    public string? LastChecksum => application.LastOfficialChecksum;

    public bool PresentationPaused => presentationPaused;

    public InterpolatedWatchView? Interpolated
    {
        get
        {
            if (application.RaceWatch is null)
            {
                return null;
            }

            return WatchMotionInterpolator.Project(
                previousFrame,
                application.RaceWatch,
                application.PendingRaceDecision is null && !presentationPaused
                    ? watchAccumulator / WatchSecondDuration
                    : 1.0,
                squadIds);
        }
    }

    public CommandResult OpenPrototype(long seed)
    {
        CommandResult created = application.Execute(
            new CreateWorldCommand("scenario.peloton.skeleton", seed));
        if (!created.Succeeded)
        {
            return created;
        }

        CommandResult prepared = application.Execute(new PrepareRaceCommand());
        if (prepared.Succeeded)
        {
            CaptureSquad();
        }

        return prepared;
    }

    public CommandResult ConfirmPreparation()
    {
        if (application.RacePreparation is { PlanConfirmed: true })
        {
            return CommandResult.Success;
        }

        return application.Execute(new ConfirmRacePreparationPlanCommand());
    }

    public CommandResult SelectFilmDuration(int seconds)
    {
        if (application.State == GameState.RaceLive)
        {
            return CommandResult.Reject("WATCH_FILM_LOCKED");
        }

        if (!WatchFilmDuration.IsChoice(seconds))
        {
            return CommandResult.Reject("WATCH_FILM_INVALID");
        }

        SelectedFilmSeconds = seconds;
        return CommandResult.Success;
    }

    public CommandResult SelectDsAutonomy(bool enabled)
    {
        if (application.State == GameState.RaceLive)
        {
            return CommandResult.Reject("WATCH_AUTONOMY_LOCKED");
        }

        DsAutonomy = enabled;
        return CommandResult.Success;
    }

    public CommandResult StartWatch()
    {
        CommandResult confirmed = ConfirmPreparation();
        if (!confirmed.Succeeded)
        {
            return confirmed;
        }

        CommandResult started = application.Execute(
            new StartRaceCommand(autosavePath, raceScenarioId));
        if (!started.Succeeded)
        {
            return started;
        }

        CaptureSquad();
        SelectedRate = WatchFilmDuration.RateFor(application.RaceWatchCourse, SelectedFilmSeconds);
        ExpectedFilmSeconds = WatchFilmDuration.EstimateFilmSeconds(
            application.RaceWatchCourse,
            SelectedFilmSeconds);
        CommandResult watching = application.Execute(new BeginRaceWatchCommand(SelectedRate));
        if (!watching.Succeeded)
        {
            return watching;
        }

        previousFrame = application.RaceWatch;
        watchAccumulator = 0.0;
        presentationPaused = false;
        return ApplyDsAutonomy();
    }

    public CommandResult SetPresentationPaused(bool paused)
    {
        if (application.State != GameState.RaceLive)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        if (application.PendingRaceDecision is not null && !DsAutonomy)
        {
            presentationPaused = true;
            return CommandResult.Success;
        }

        presentationPaused = paused;
        return CommandResult.Success;
    }

    public CommandResult Tick(double realDeltaSeconds)
    {
        if (application.State != GameState.RaceLive || application.RaceWatch is null)
        {
            return CommandResult.Success;
        }

        CommandResult autonomy = ApplyDsAutonomy();
        if (!autonomy.Succeeded)
        {
            return autonomy;
        }

        if (application.PendingRaceDecision is not null && !DsAutonomy)
        {
            presentationPaused = true;
            watchAccumulator = WatchSecondDuration;
            return CommandResult.Success;
        }

        if (presentationPaused)
        {
            return CommandResult.Success;
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(realDeltaSeconds, 0.0);
        watchAccumulator += realDeltaSeconds;
        while (watchAccumulator >= WatchSecondDuration &&
               application.State == GameState.RaceLive)
        {
            CommandResult drained = ApplyDsAutonomy();
            if (!drained.Succeeded)
            {
                return drained;
            }

            if (application.PendingRaceDecision is not null && !DsAutonomy)
            {
                presentationPaused = true;
                watchAccumulator = WatchSecondDuration;
                break;
            }

            watchAccumulator -= WatchSecondDuration;
            previousFrame = application.RaceWatch;
            CommandResult advanced = application.Execute(new AdvanceRaceWatchCommand());
            if (!advanced.Succeeded)
            {
                return advanced;
            }

            if (application.State != GameState.RaceLive)
            {
                watchAccumulator = 0.0;
                break;
            }

            CommandResult after = ApplyDsAutonomy();
            if (!after.Succeeded)
            {
                return after;
            }

            if (application.PendingRaceDecision is not null && !DsAutonomy)
            {
                presentationPaused = true;
                watchAccumulator = WatchSecondDuration;
                break;
            }
        }

        return CommandResult.Success;
    }

    public CommandResult Respond(RaceDecisionOption option)
    {
        if (application.PendingRaceDecision is not PendingRaceDecision decision)
        {
            return CommandResult.Reject("RACE_DECISION_NOT_PENDING");
        }

        CommandResult responded = application.Execute(new RespondToRaceDecisionCommand(
            decision.RequestId,
            decision.AuthorityId,
            option));
        if (!responded.Succeeded)
        {
            return responded;
        }

        presentationPaused = false;
        watchAccumulator = 0.0;
        previousFrame = application.RaceWatch;
        return CommandResult.Success;
    }

    public CommandResult RespondDelegatedDefault()
    {
        if (application.PendingRaceDecision is not PendingRaceDecision decision)
        {
            return CommandResult.Reject("RACE_DECISION_NOT_PENDING");
        }

        return Respond(decision.DelegatedDefaultOption);
    }

    public CommandResult Abandon()
    {
        CommandResult abandoned = application.Execute(new AbandonRaceLiveCommand(autosavePath));
        previousFrame = null;
        watchAccumulator = 0.0;
        presentationPaused = false;
        return abandoned;
    }

    public CommandResult AcknowledgeResults()
    {
        return application.Execute(new AcknowledgeRaceResultsCommand());
    }

    public CommandResult CompleteDebrief()
    {
        return application.Execute(new CompleteRaceDebriefCommand());
    }

    private CommandResult ApplyDsAutonomy()
    {
        if (!DsAutonomy || application.PendingRaceDecision is not PendingRaceDecision decision)
        {
            return CommandResult.Success;
        }

        CommandResult responded = application.Execute(new RespondToRaceDecisionCommand(
            decision.RequestId,
            decision.AuthorityId,
            decision.DelegatedDefaultOption));
        if (!responded.Succeeded)
        {
            return responded;
        }

        presentationPaused = false;
        previousFrame = application.RaceWatch;
        return CommandResult.Success;
    }

    private void CaptureSquad()
    {
        if (application.RacePreparation is not RacePreparationProjection prep)
        {
            return;
        }

        squadIds.Clear();
        foreach (WorldEntityId id in prep.Squad)
        {
            squadIds.Add(id.Value);
        }
    }
}
