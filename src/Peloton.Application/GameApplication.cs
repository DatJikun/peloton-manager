using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public sealed class GameApplication
{
    private readonly IScenarioCatalog scenarioCatalog;
    private readonly IRaceScenarioCatalog raceScenarioCatalog;
    private readonly IWorldSaveStore saveStore;
    private readonly IRaceEngine raceEngine;
    private RaceSession? activeRaceSession;
    private RaceWatchClock? watchClock;
    private RacePreparationCheckpoint? racePreparation;
    private PreSeasonPlanningDraft? preSeasonDraft;
    private string? lastOfficialChecksum;

    public GameApplication(
        IScenarioCatalog scenarioCatalog,
        IRaceScenarioCatalog raceScenarioCatalog,
        IWorldSaveStore saveStore,
        IRaceEngine raceEngine)
    {
        this.scenarioCatalog = scenarioCatalog ?? throw new ArgumentNullException(nameof(scenarioCatalog));
        this.raceScenarioCatalog = raceScenarioCatalog ?? throw new ArgumentNullException(nameof(raceScenarioCatalog));
        this.saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
        this.raceEngine = raceEngine ?? throw new ArgumentNullException(nameof(raceEngine));
    }

    public GameState State { get; private set; } = GameState.MainMenu;

    public WorldState? World { get; private set; }

    public RacePreparationProjection? RacePreparation
    {
        get
        {
            if (State != GameState.RacePreparationFlow || racePreparation is null || World is null)
            {
                return null;
            }

            AccessContext access = GetAccessContext();
            if (access.CurrentOrganizationId is not WorldEntityId organizationId)
            {
                return null;
            }

            WorldEntityId[] squad = World.GetRiderCareersForOrganization(organizationId)
                .Select(career => career.Id)
                .ToArray();
            string objective = ResolvePreparationObjective(World, organizationId, racePreparation.RaceScenarioId);
            bool canRun = racePreparation.PlanConfirmed;
            return new RacePreparationProjection(
                RacePreparationDefaults.Title,
                objective,
                Array.AsReadOnly(squad),
                racePreparation.LeaderId,
                racePreparation.SupportId,
                racePreparation.Objective,
                racePreparation.BriefingKind,
                racePreparation.StrategySet,
                racePreparation.PlanConfirmed,
                canRun,
                canRun);
        }
    }

    public PreSeasonPlanningProjection? PreSeasonPlanning
    {
        get
        {
            if (State != GameState.PreSeasonPlanningFlow || World is null || preSeasonDraft is null)
            {
                return null;
            }

            AccessContext access = GetAccessContext();
            if (access.CurrentOrganizationId is not WorldEntityId organizationId)
            {
                return null;
            }

            PreSeasonRaceEntryProjection[] races = World.CalendarEntries
                .Where(entry => entry.Kind == CalendarEntryKind.Race && entry.OfficialResult is null)
                .OrderBy(entry => entry.DayNumber)
                .ThenBy(entry => entry.Id.Value)
                .Select(entry =>
                {
                    string raceContentId = entry.RaceContentId ?? RacePreparationDefaults.PrototypeScenarioId;
                    bool entered = preSeasonDraft.EntriesByRaceContentId.TryGetValue(raceContentId, out bool draftEntered)
                        ? draftEntered
                        : World.IsOrganizationEnteredForRace(organizationId, raceContentId);
                    return new PreSeasonRaceEntryProjection(
                        raceContentId,
                        entry.DayNumber,
                        entry.Title,
                        entered);
                })
                .ToArray();
            return new PreSeasonPlanningProjection(races);
        }
    }

    public PendingRaceDecision? PendingRaceDecision
    {
        get
        {
            RaceDecisionRequest? request = activeRaceSession?.PendingDecision;
            return request is null
                ? null
                : new PendingRaceDecision(
                    request.Id,
                    request.AuthorityId,
                    request.RaceSecond,
                    request.Trigger,
                    Array.AsReadOnly(request.DefensibleOptions.ToArray()),
                    request.DelegatedDefaultOption);
        }
    }

    public RaceWatchFrame? RaceWatch =>
        State == GameState.RaceLive && watchClock is not null ? watchClock.Current : null;

    public RaceWatchCourse? RaceWatchCourse =>
        State == GameState.RaceLive && activeRaceSession is not null ? activeRaceSession.Course : null;

    public string? LastOfficialChecksum => lastOfficialChecksum;

    public int LastWatchSecond { get; private set; }

    public int LastSimSecond { get; private set; }

    public CareerDayProjection? CareerDay
    {
        get
        {
            if (World is null || State != GameState.Management)
            {
                return null;
            }

            AccessContext access = GetAccessContext();
            Person? manager = access.ViewerPersonId is WorldEntityId personId
                ? World.Persons.FirstOrDefault(person => person.Id == personId)
                : null;
            Organization? employer = access.CurrentOrganizationId is WorldEntityId employerId
                ? World.Organizations.FirstOrDefault(organization => organization.Id == employerId)
                : null;
            bool raceDueToday = access.CurrentOrganizationId is WorldEntityId raceOrganizationId &&
                                World.IsRaceDueForOrganization(raceOrganizationId);
            string primaryAction;
            string primaryLabel;
            if (State == GameState.Management && raceDueToday)
            {
                primaryAction = HubPrimaryActionIds.RaceNext;
                primaryLabel = HubPrimaryActionLabels.RaceNext;
            }
            else
            {
                primaryAction = HubPrimaryActionIds.AdvanceDay;
                primaryLabel = HubPrimaryActionLabels.AdvanceDay;
            }

            return new CareerDayProjection(
                World.CurrentDate.DayNumber,
                manager?.Name ?? string.Empty,
                employer?.Name,
                World.DaysUntilNextRace,
                World.NextRaceDayNumber,
                raceDueToday,
                World.LastDayNotes,
                World.RaceCount,
                primaryAction,
                primaryLabel);
        }
    }

    public RaceResultProjection? RaceResult
    {
        get
        {
            if (State != GameState.RaceResultsFlow || World is null)
            {
                return null;
            }

            return RaceOutcomeQueries.BuildResult(World, racePreparation, raceScenarioCatalog);
        }
    }

    public RaceDebriefProjection? RaceDebrief
    {
        get
        {
            if (State != GameState.RaceDebriefFlow)
            {
                return null;
            }

            return RaceOutcomeQueries.BuildDebrief(World, racePreparation, raceScenarioCatalog);
        }
    }

    public IReadOnlyList<CalendarEntryProjection> Calendar =>
        World is null ? Array.Empty<CalendarEntryProjection>() : CareerProjectionQueries.BuildCalendar(World, GetAccessContext());

    public IReadOnlyList<InboxItemProjection> Inbox =>
        World is null ? Array.Empty<InboxItemProjection>() : CareerProjectionQueries.BuildInbox(World, GetAccessContext());

    public CommandResult Execute(CreateWorldCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State is not (GameState.MainMenu or GameState.NewGameFlow))
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        State = GameState.LoadingWorld;
        try
        {
            WorldRecipe recipe = scenarioCatalog.Resolve(command.ScenarioId);
            World = CreateWorld(recipe, command.Seed);
            racePreparation = null;
            preSeasonDraft = null;
            watchClock = null;
            lastOfficialChecksum = null;
            LastWatchSecond = 0;
            LastSimSecond = 0;
            State = GameState.Management;
            return CommandResult.Success;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException)
        {
            World = null;
            State = GameState.MainMenu;
            return CommandResult.Reject("WORLD_CREATE_FAILED");
        }
    }

    public CommandResult Execute(AdvanceDayCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.Management || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        AccessContext access = GetAccessContext();
        if (access.CurrentOrganizationId is WorldEntityId organizationId &&
            World.IsRaceDueForOrganization(organizationId))
        {
            return CommandResult.Reject("RACE_DAY_PENDING");
        }

        if (World.IsCalendarRaceDue && World.HasEnteredTeamsForTodaysRace())
        {
            CommandResult delegated = AutoSimulateDelegatedRace();
            if (!delegated.Succeeded)
            {
                return delegated;
            }
        }

        DeterministicScheduler.AdvanceDay(World);
        World.CaptureDayNotes(GetAccessContext());
        return CommandResult.Success;
    }

    public CommandResult Execute(SaveGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State == GameState.RaceLive)
        {
            return CommandResult.Reject("SAVE_FORBIDDEN_IN_RACE_LIVE");
        }

        if (World is null || !IsLegalSaveState(State))
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        try
        {
            saveStore.Save(command.Path, new WorldCheckpoint(State, World, racePreparation));
            return CommandResult.Success;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            return CommandResult.Reject("SAVE_FAILED");
        }
    }

    public CommandResult Execute(LoadGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State == GameState.RaceLive)
        {
            return CommandResult.Reject("LOAD_FORBIDDEN_IN_RACE_LIVE");
        }

        GameState previousState = State;
        WorldState? previousWorld = World;
        State = GameState.LoadingWorld;
        try
        {
            WorldCheckpoint checkpoint = saveStore.Load(command.Path);
            World = checkpoint.World;
            State = checkpoint.GameState;
            activeRaceSession = null;
            watchClock = null;
            lastOfficialChecksum = null;
            LastWatchSecond = 0;
            LastSimSecond = 0;
            racePreparation = checkpoint.RacePreparation;
            preSeasonDraft = null;
            return CommandResult.Success;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            World = previousWorld;
            State = previousWorld is null ? GameState.MainMenu : previousState;
            return CommandResult.Reject("LOAD_FAILED");
        }
    }

    public CommandResult Execute(PrepareRaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.Management || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        string raceScenarioId = ResolveCurrentRaceScenarioId();
        State = GameState.RacePreparationFlow;
        racePreparation = new RacePreparationCheckpoint(
            raceScenarioId,
            PlanConfirmed: false);
        return CommandResult.Success;
    }

    public CommandResult Execute(BeginPreSeasonPlanningCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.Management || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        AccessContext access = GetAccessContext();
        if (access.CurrentOrganizationId is not WorldEntityId organizationId)
        {
            return CommandResult.Reject("EMPLOYER_REQUIRED");
        }

        Dictionary<string, bool> entries = new(StringComparer.Ordinal);
        foreach (CalendarEntry entry in World.CalendarEntries)
        {
            if (entry.Kind != CalendarEntryKind.Race || entry.OfficialResult is not null)
            {
                continue;
            }

            string raceContentId = entry.RaceContentId ?? RacePreparationDefaults.PrototypeScenarioId;
            if (!entries.ContainsKey(raceContentId))
            {
                entries[raceContentId] = World.IsOrganizationEnteredForRace(organizationId, raceContentId);
            }
        }

        preSeasonDraft = new PreSeasonPlanningDraft(entries);
        State = GameState.PreSeasonPlanningFlow;
        return CommandResult.Success;
    }

    public CommandResult Execute(SetSeasonRaceEntryCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.PreSeasonPlanningFlow || World is null || preSeasonDraft is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(command.RaceContentId);
        if (!preSeasonDraft.EntriesByRaceContentId.ContainsKey(command.RaceContentId))
        {
            return CommandResult.Reject("RACE_CONTENT_NOT_PLANNABLE");
        }

        preSeasonDraft.EntriesByRaceContentId[command.RaceContentId] = command.Entered;
        return CommandResult.Success;
    }

    public CommandResult Execute(ConfirmPreSeasonPlanCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.PreSeasonPlanningFlow || World is null || preSeasonDraft is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        AccessContext access = GetAccessContext();
        if (access.CurrentOrganizationId is not WorldEntityId organizationId)
        {
            return CommandResult.Reject("EMPLOYER_REQUIRED");
        }

        foreach ((string raceContentId, bool entered) in preSeasonDraft.EntriesByRaceContentId)
        {
            World.SetOrganizationRaceEntry(organizationId, raceContentId, entered);
        }

        preSeasonDraft = null;
        State = GameState.Management;
        return CommandResult.Success;
    }

    public CommandResult Execute(CancelPreSeasonPlanningCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.PreSeasonPlanningFlow || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        preSeasonDraft = null;
        State = GameState.Management;
        return CommandResult.Success;
    }

    public CommandResult Execute(SetRacePreparationStrategyCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RacePreparationFlow || World is null || racePreparation is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        if (command.LeaderId == command.SupportId)
        {
            return CommandResult.Reject("PREP_STRATEGY_RIDERS_INVALID");
        }

        AccessContext access = GetAccessContext();
        if (access.CurrentOrganizationId is not WorldEntityId organizationId)
        {
            return CommandResult.Reject("EMPLOYER_REQUIRED");
        }

        WorldEntityId[] squad = World.GetRiderCareersForOrganization(organizationId)
            .Select(career => career.Id)
            .ToArray();
        if (!squad.Contains(command.LeaderId) || !squad.Contains(command.SupportId))
        {
            return CommandResult.Reject("PREP_STRATEGY_RIDERS_INVALID");
        }

        racePreparation = racePreparation with
        {
            LeaderId = command.LeaderId,
            SupportId = command.SupportId,
            Objective = command.Objective,
            BriefingKind = command.BriefingKind,
        };
        return CommandResult.Success;
    }

    public CommandResult Execute(CancelRacePreparationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RacePreparationFlow || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        racePreparation = null;
        State = GameState.Management;
        return CommandResult.Success;
    }

    public CommandResult Execute(ConfirmRacePreparationPlanCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RacePreparationFlow || racePreparation is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        if (!racePreparation.StrategySet)
        {
            return CommandResult.Reject("PREP_STRATEGY_INCOMPLETE");
        }

        racePreparation = racePreparation with { PlanConfirmed = true };
        return CommandResult.Success;
    }

    public CommandResult Execute(FollowHubPrimaryActionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.Management || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        AccessContext access = GetAccessContext();
        if (access.CurrentOrganizationId is WorldEntityId organizationId &&
            World.IsRaceDueForOrganization(organizationId))
        {
            return Execute(new PrepareRaceCommand());
        }

        return Execute(new AdvanceDayCommand());
    }

    public CommandResult Execute(StartRaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RacePreparationFlow || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        if (racePreparation is null || !racePreparation.PlanConfirmed)
        {
            return CommandResult.Reject("PREP_PLAN_INCOMPLETE");
        }

        try
        {
            saveStore.Save(command.PreRaceAutosavePath, new WorldCheckpoint(State, World, racePreparation));
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            return CommandResult.Reject("PRE_RACE_AUTOSAVE_FAILED");
        }

        try
        {
            RaceScenario scenario = BuildOfficialRaceScenario(command.RaceScenarioId);
            long raceSeed = DeriveRaceSeed(World, scenario);
            activeRaceSession = raceEngine.CreateSession(scenario, raceSeed);
            watchClock = null;
            State = GameState.RaceLive;
            return CommandResult.Success;
        }
        catch (Exception exception) when (
            exception is IOException or ArgumentException or InvalidOperationException)
        {
            activeRaceSession = null;
            return CommandResult.Reject("RACE_START_FAILED");
        }
    }

    public CommandResult Execute(SimulateRaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RacePreparationFlow || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        if (racePreparation is null || !racePreparation.PlanConfirmed)
        {
            return CommandResult.Reject("PREP_PLAN_INCOMPLETE");
        }

        try
        {
            RaceScenario scenario = BuildOfficialRaceScenario(command.RaceScenarioId);
            RaceResult result = raceEngine.RunBatch(scenario, DeriveRaceSeed(World, scenario));
            CommitOfficialResult(result, command.RaceScenarioId, scenario);
            return CommandResult.Success;
        }
        catch (Exception exception) when (
            exception is IOException or ArgumentException or InvalidOperationException)
        {
            return CommandResult.Reject("RACE_SIMULATION_FAILED");
        }
    }

    public CommandResult Execute(AdvanceRaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RaceLive || World is null || activeRaceSession is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        try
        {
            while (true)
            {
                RaceStepResult step = activeRaceSession.Step();
                if (step.Status == RaceStepStatus.Advanced)
                {
                    continue;
                }

                if (step.Status == RaceStepStatus.Completed)
                {
                    RaceResult result = step.Result
                        ?? throw new InvalidOperationException("A completed race step must carry its result.");
                    CommitOfficialResult(
                        result,
                        racePreparation?.RaceScenarioId ?? RacePreparationDefaults.PrototypeScenarioId,
                        activeRaceSession.Scenario);
                }

                return CommandResult.Success;
            }
        }
        catch (InvalidOperationException)
        {
            return CommandResult.Reject("RACE_ADVANCE_FAILED");
        }
    }

    public CommandResult Execute(BeginRaceWatchCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RaceLive || activeRaceSession is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        try
        {
            watchClock = new RaceWatchClock(activeRaceSession, command.Rate);
            CaptureWatch(watchClock.Current);
            return CommandResult.Success;
        }
        catch (ArgumentOutOfRangeException)
        {
            return CommandResult.Reject("WATCH_RATE_INVALID");
        }
    }

    public CommandResult Execute(AdvanceRaceWatchCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RaceLive || World is null || activeRaceSession is null || watchClock is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        try
        {
            watchClock.AdvanceOneWatchSecond();
            CaptureWatch(watchClock.Current);
            if (activeRaceSession.IsCompleted)
            {
                RaceResult result = activeRaceSession.Result
                    ?? throw new InvalidOperationException("A completed watch step must carry its result.");
                CommitOfficialResult(
                    result,
                    racePreparation?.RaceScenarioId ?? RacePreparationDefaults.PrototypeScenarioId,
                    activeRaceSession.Scenario);
            }

            return CommandResult.Success;
        }
        catch (InvalidOperationException)
        {
            return CommandResult.Reject("RACE_ADVANCE_FAILED");
        }
    }

    public CommandResult Execute(AbandonRaceLiveCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RaceLive)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        activeRaceSession = null;
        watchClock = null;
        LastWatchSecond = 0;
        LastSimSecond = 0;
        lastOfficialChecksum = null;
        try
        {
            WorldCheckpoint checkpoint = saveStore.Load(command.PreRaceAutosavePath);
            World = checkpoint.World;
            racePreparation = checkpoint.RacePreparation;
            State = checkpoint.GameState;
            return CommandResult.Success;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            World = null;
            racePreparation = null;
            State = GameState.MainMenu;
            return CommandResult.Reject("RACE_ABANDON_FAILED");
        }
    }

    public CommandResult Execute(RespondToRaceDecisionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RaceLive || activeRaceSession is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        RaceDecisionRequest? pending = activeRaceSession.PendingDecision;
        if (pending is null)
        {
            return CommandResult.Reject("RACE_DECISION_NOT_PENDING");
        }

        if (command.RequestId != pending.Id)
        {
            return CommandResult.Reject("RACE_DECISION_REQUEST_INVALID");
        }

        if (command.AuthorityId != pending.AuthorityId)
        {
            return CommandResult.Reject("RACE_DECISION_AUTHORITY_INVALID");
        }

        if (!pending.DefensibleOptions.Contains(command.SelectedOption))
        {
            return CommandResult.Reject("RACE_DECISION_OPTION_INVALID");
        }

        activeRaceSession.ResolveDecision(new RaceDecisionResolution(
            command.RequestId,
            command.AuthorityId,
            command.SelectedOption));
        return CommandResult.Success;
    }

    public CommandResult Execute(AcknowledgeRaceResultsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RaceResultsFlow)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        State = GameState.RaceDebriefFlow;
        return CommandResult.Success;
    }

    public CommandResult Execute(CompleteRaceDebriefCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RaceDebriefFlow)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        racePreparation = null;
        State = GameState.Management;
        return CommandResult.Success;
    }

    public CommandResult Execute(ArchiveInboxItemCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.Management || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        InboxItemProjection? item = Inbox.FirstOrDefault(
            inboxItem => string.Equals(inboxItem.Identity, command.Identity, StringComparison.Ordinal));
        if (item is null)
        {
            return CommandResult.Reject("INBOX_ITEM_NOT_FOUND");
        }

        if (string.Equals(item.Category, "race-due", StringComparison.Ordinal))
        {
            return CommandResult.Reject("INBOX_SOURCE_CANNOT_BE_DISMISSED");
        }

        if (string.Equals(item.Category, "race-result", StringComparison.Ordinal))
        {
            if (item.RelatedEntryId is not WorldEntityId entryId || !World.AcknowledgeRaceResult(entryId))
            {
                return CommandResult.Reject("INBOX_ITEM_NOT_FOUND");
            }

            return CommandResult.Success;
        }

        return CommandResult.Reject("INBOX_ITEM_NOT_FOUND");
    }

    public AccessContext GetAccessContext()
    {
        if (World is null)
        {
            return new AccessContext(null, null, null, "Public");
        }

        ManagerCareer? manager = World.ManagerCareers.Count == 0 ? null : World.ManagerCareers[0];
        Employment? employment = manager?.ActiveEmploymentId is null
            ? null
            : World.Employments.FirstOrDefault(item => item.Id == manager.ActiveEmploymentId.Value);
        DecisionAuthority? authority = World.DecisionAuthorities.Count == 0 ? null : World.DecisionAuthorities[0];
        return new AccessContext(
            manager?.PersonId,
            employment?.OrganizationId,
            authority?.Id,
            employment is null ? "PublicPersonal" : "CurrentOrganization");
    }

    private void CaptureWatch(RaceWatchFrame frame)
    {
        LastWatchSecond = frame.WatchSecond;
        LastSimSecond = frame.RaceSecond;
    }

    private void CommitOfficialResult(RaceResult result, string raceScenarioId, RaceScenario scenario)
    {
        RecordOfficialRaceResult(result, raceScenarioId, scenario);
        activeRaceSession = null;
        watchClock = null;
        State = GameState.RaceResultsFlow;
    }

    private void RecordOfficialRaceResult(RaceResult result, string raceScenarioId, RaceScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(World);
        WorldEntityId[] starters = scenario.Riders.Select(rider => rider.RiderId).ToArray();
        World.RecordRace(
            new RaceSummary(result.RouteId, result.WinnerId, result.FinishOrder),
            raceScenarioId,
            starters);
        lastOfficialChecksum = result.Checksum;
    }

    private CommandResult AutoSimulateDelegatedRace()
    {
        ArgumentNullException.ThrowIfNull(World);
        string raceScenarioId = ResolveCurrentRaceScenarioId();
        try
        {
            RaceScenario scenario = BuildOfficialRaceScenario(raceScenarioId, includePlayerStrategy: false);
            RaceResult result = raceEngine.RunBatch(scenario, DeriveRaceSeed(World, scenario));
            RecordOfficialRaceResult(result, raceScenarioId, scenario);
            return CommandResult.Success;
        }
        catch (Exception exception) when (
            exception is IOException or ArgumentException or InvalidOperationException)
        {
            return CommandResult.Reject("RACE_SIMULATION_FAILED");
        }
    }

    private string ResolveCurrentRaceScenarioId() =>
        World?.TryGetTodaysRaceContentId() ?? RacePreparationDefaults.PrototypeScenarioId;

    private RaceScenario BuildOfficialRaceScenario(string raceScenarioId, bool includePlayerStrategy = true)
    {
        ArgumentNullException.ThrowIfNull(World);
        WorldRecipe recipe = scenarioCatalog.Resolve(World.ContentIdentity.ScenarioId);
        RaceScenarioTemplate template = raceScenarioCatalog.ResolveTemplate(raceScenarioId);
        RacePreparationStrategy? strategy = null;
        WorldEntityId? playerOrganizationId = null;
        if (includePlayerStrategy &&
            racePreparation?.StrategySet == true &&
            GetAccessContext().CurrentOrganizationId is WorldEntityId organizationId)
        {
            playerOrganizationId = organizationId;
            strategy = new RacePreparationStrategy(
                racePreparation.LeaderId!.Value,
                racePreparation.SupportId!.Value,
                racePreparation.Objective!.Value,
                racePreparation.BriefingKind!.Value);
        }

        return WorldRaceScenarioAssembler.Assemble(
            World,
            recipe,
            template,
            raceScenarioId,
            strategy,
            playerOrganizationId);
    }

    private string ResolvePreparationObjective(
        WorldState world,
        WorldEntityId organizationId,
        string raceScenarioId)
    {
        Organization? employer = world.Organizations.FirstOrDefault(
            organization => organization.Id == organizationId);
        if (employer is null)
        {
            return RacePreparationDefaults.Objective;
        }

        try
        {
            WorldRecipe recipe = scenarioCatalog.Resolve(world.ContentIdentity.ScenarioId);
            TeamRaceMappingDefinition? mapping = recipe.TeamRaceMappings.FirstOrDefault(
                item => string.Equals(item.OrganizationId, employer.OriginDefinitionId, StringComparison.Ordinal));
            if (mapping is null)
            {
                return RacePreparationDefaults.Objective;
            }

            RaceScenarioTemplate template = raceScenarioCatalog.ResolveTemplate(raceScenarioId);
            if (!template.Teams.TryGetValue(mapping.RaceTeamId, out RaceTeamTemplate? team))
            {
                return RacePreparationDefaults.Objective;
            }

            return team.Objective.ToString();
        }
        catch (Exception exception) when (exception is IOException or ArgumentException)
        {
            return RacePreparationDefaults.Objective;
        }
    }

    private static bool IsLegalSaveState(GameState state)
    {
        return state is GameState.Management or
            GameState.PreSeasonPlanningFlow or
            GameState.RacePreparationFlow or
            GameState.RaceResultsFlow or
            GameState.RaceDebriefFlow;
    }

    private static long DeriveRaceSeed(WorldState world, RaceScenario scenario)
    {
        return unchecked((long)StableSeedDerivation.Derive(
            world.MasterSeed,
            $"official-race-v1:{world.RaceCount + 1}:{scenario.Id}:{scenario.TuningIdentity}"));
    }

    private static WorldState CreateWorld(WorldRecipe recipe, long seed)
    {
        WorldEntityIdAllocator allocator = new();
        Dictionary<string, WorldEntityId> organizationIds = new(StringComparer.Ordinal);
        List<Organization> organizations = new(recipe.Organizations.Count);
        foreach (OrganizationDefinition definition in recipe.Organizations.OrderBy(
                     item => item.Id,
                     StringComparer.Ordinal))
        {
            WorldEntityId organizationId = allocator.Allocate();
            organizationIds[definition.Id] = organizationId;
            organizations.Add(new Organization(organizationId, definition.Id, definition.Name));
        }

        List<Person> persons = new();
        List<RiderCareer> riderCareers = new();
        foreach (RiderDefinition definition in recipe.Riders.OrderBy(
                     rider => rider.Id,
                     StringComparer.Ordinal))
        {
            WorldEntityId personId = allocator.Allocate();
            WorldEntityId riderCareerId = allocator.Allocate();
            persons.Add(new Person(personId, definition.Name, definition.Id));
            riderCareers.Add(new RiderCareer(
                riderCareerId,
                personId,
                organizationIds[definition.OrganizationId],
                definition.Id,
                definition.CriticalPowerW,
                definition.WPrimeCapacityJ,
                definition.PeakPowerW,
                definition.WPrimeRecoveryJPerSecond,
                definition.LowIntensityDurability,
                definition.HighIntensityDurability,
                definition.BodyMassKg,
                definition.SystemMassKg,
                definition.CdAM2,
                definition.BaseCrr,
                definition.Positioning,
                definition.Handling,
                definition.TacticalAwareness));
        }

        WorldEntityId managerPersonId = allocator.Allocate();
        persons.Add(new Person(managerPersonId, recipe.Manager.Name));
        WorldEntityId managerCareerId = allocator.Allocate();
        WorldEntityId employmentId = allocator.Allocate();
        WorldEntityId authorityId = allocator.Allocate();
        ManagerCareer managerCareer = new(managerCareerId, managerPersonId, employmentId);
        Employment employment = new(
            employmentId,
            managerCareerId,
            organizationIds[recipe.Manager.OrganizationId],
            new WorldDate(0),
            null);
        DecisionAuthority authority = new(authorityId, DecisionAuthorityKind.HumanInput);
        int calendarPeriodDays = ReadCalendarPeriodDays(recipe);
        WorldEntityId initialRaceEntryId = allocator.Allocate();
        IReadOnlyList<CalendarEntry> calendarEntries = new[]
        {
            new CalendarEntry(
                initialRaceEntryId,
                calendarPeriodDays,
                CalendarEntryKind.Race,
                "Skeleton race",
                RaceContentId: RacePreparationDefaults.PrototypeScenarioId),
        };
        List<OrganizationRaceEntry> organizationRaceEntries = new();
        foreach (Organization organization in organizations)
        {
            foreach (CalendarEntry entry in calendarEntries)
            {
                string raceContentId = entry.RaceContentId ?? RacePreparationDefaults.PrototypeScenarioId;
                organizationRaceEntries.Add(new OrganizationRaceEntry(organization.Id, raceContentId, Entered: true));
            }
        }

        return new WorldState(
            $"{recipe.ContentIdentity.ScenarioId}:{seed}",
            seed,
            StableSeedDerivation.ContractVersion,
            new WorldDate(0),
            recipe.ContentIdentity,
            recipe.RulesIdentity,
            recipe.RulesModules,
            allocator.HighWaterMark,
            persons,
            new[] { managerCareer },
            new[] { employment },
            organizations,
            new[] { authority },
            raceCount: 0,
            lastRace: null,
            calendarPeriodDays: calendarPeriodDays,
            calendarEntries: calendarEntries,
            riderCareers: riderCareers,
            organizationRaceEntries: organizationRaceEntries);
    }

    private static int ReadCalendarPeriodDays(WorldRecipe recipe)
    {
        RulesModuleIdentity? calendar = recipe.RulesModules.FirstOrDefault(
            module => string.Equals(module.Slot, "calendarStructure", StringComparison.Ordinal));
        const string prefix = "days-per-season:";
        if (calendar is not null &&
            calendar.ParameterIdentity.StartsWith(prefix, StringComparison.Ordinal) &&
            int.TryParse(
                calendar.ParameterIdentity[prefix.Length..],
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out int days) &&
            days > 0)
        {
            return days;
        }

        return 12;
    }
}
