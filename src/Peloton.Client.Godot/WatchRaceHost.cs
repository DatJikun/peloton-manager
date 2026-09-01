using System;
using System.Linq;
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
    private RaceWatchFrame? previousFrame;
    private double watchAccumulator;
    private bool presentationPaused;
    private int? rateOverride;

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
    }

    public int SelectedFilmSeconds { get; private set; }

    public int SelectedRate { get; private set; }

    public GameState State => application.State;

    public RaceWatchFrame? OfficialFrame => application.RaceWatch;

    public RaceWatchCourse? Course => application.RaceWatchCourse;

    public PendingRaceDecision? PendingDecision => application.PendingRaceDecision;

    public RaceResultProjection? Result => application.RaceResult;

    public RaceDebriefProjection? Debrief => application.RaceDebrief;

    public string? LastChecksum => application.LastOfficialChecksum;

    public bool PresentationPaused => presentationPaused;

    public string RiderLabel(WorldEntityId riderId)
    {
        if (application.World is not WorldState world)
        {
            return riderId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        RiderCareer? career = world.TryGetRiderCareer(riderId);
        WorldEntityId personId = career?.PersonId ?? riderId;
        Person? person = world.Persons.FirstOrDefault(item => item.Id == personId);
        if (person is not null && !string.IsNullOrWhiteSpace(person.Name))
        {
            return person.Name;
        }

        return career?.OriginDefinitionId ?? riderId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public InterpolatedWatchView? Interpolated
    {
        get
        {
            if (application.RaceWatch is null)
            {
                return null;
            }

            InterpolatedWatchView projected = WatchMotionInterpolator.Project(
                previousFrame,
                application.RaceWatch,
                application.PendingRaceDecision is null && !presentationPaused
                    ? watchAccumulator / WatchSecondDuration
                    : 1.0);
            InterpolatedRiderView[] named = projected.Riders
                .Select(rider => rider with { Name = RiderLabel(new WorldEntityId(rider.RiderId)) })
                .ToArray();
            return projected with { Riders = named };
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

        return application.Execute(new PrepareRaceCommand());
    }

    public CommandResult SetDefaultStrategy()
    {
        return RacePreparationSupport.SetDefaultStrategy(application);
    }

    public CommandResult ConfirmPreparation()
    {
        return application.Execute(new ConfirmRacePreparationPlanCommand());
    }

    public CommandResult CancelPreparation()
    {
        return application.Execute(new CancelRacePreparationCommand());
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

    public CommandResult SelectRate(int rate)
    {
        if (application.State == GameState.RaceLive)
        {
            return CommandResult.Reject("WATCH_RATE_LOCKED");
        }

        if (rate < 1)
        {
            return CommandResult.Reject("WATCH_RATE_INVALID");
        }

        rateOverride = rate;
        return CommandResult.Success;
    }

    public CommandResult StartWatch()
    {
        CommandResult started = application.Execute(
            new StartRaceCommand(autosavePath, raceScenarioId));
        if (!started.Succeeded)
        {
            return started;
        }

        double routeLengthM = application.RaceWatchCourse?.TotalLengthM ?? 0.0;
        SelectedRate = rateOverride ?? WatchFilmDuration.RateFor(routeLengthM, SelectedFilmSeconds);
        CommandResult watching = application.Execute(new BeginRaceWatchCommand(SelectedRate));
        if (!watching.Succeeded)
        {
            return watching;
        }

        previousFrame = application.RaceWatch;
        watchAccumulator = 0.0;
        presentationPaused = false;
        return CommandResult.Success;
    }

    public CommandResult SetPresentationPaused(bool paused)
    {
        if (application.State != GameState.RaceLive)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        presentationPaused = paused || application.PendingRaceDecision is not null;
        return CommandResult.Success;
    }

    public CommandResult Tick(double realDeltaSeconds)
    {
        if (application.State != GameState.RaceLive || application.RaceWatch is null)
        {
            return CommandResult.Success;
        }

        if (application.PendingRaceDecision is not null)
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
               application.State == GameState.RaceLive &&
               application.PendingRaceDecision is null)
        {
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

            if (application.PendingRaceDecision is not null)
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
}
