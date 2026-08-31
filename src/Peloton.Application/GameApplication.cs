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
    private string? lastOfficialChecksum;
    private int? lastCommittedDecisionCount;

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
            if (State != GameState.RacePreparationFlow || racePreparation is null)
            {
                return null;
            }

            WorldEntityId[] squad = World is null
                ? Array.Empty<WorldEntityId>()
                : CareerRaceBinder.PlayerSquad(World);
            IReadOnlyList<SquadSeat> seats = World is null
                ? Array.Empty<SquadSeat>()
                : CareerRaceBinder.Seats(World, racePreparation.Assignments);
            return new RacePreparationProjection(
                CurrentRaceTitle(World),
                RacePreparationDefaults.Objective,
                Array.AsReadOnly(squad),
                seats,
                racePreparation.PlanConfirmed,
                racePreparation.PlanConfirmed,
                racePreparation.PlanConfirmed);
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
            Organization? employer = access.CurrentOrganizationId is WorldEntityId organizationId
                ? World.Organizations.FirstOrDefault(organization => organization.Id == organizationId)
                : null;
            string primaryAction;
            string primaryLabel;
            if (State == GameState.Management && World.IsRaceDue)
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
                World.IsRaceDue,
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

            return RaceOutcomeQueries.BuildResult(
                World,
                racePreparation,
                raceScenarioCatalog,
                lastCommittedDecisionCount);
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
        World is null ? Array.Empty<CalendarEntryProjection>() : CareerProjectionQueries.BuildCalendar(World);

    public IReadOnlyList<InboxItemProjection> Inbox =>
        World is null ? Array.Empty<InboxItemProjection>() : CareerProjectionQueries.BuildInbox(World);

    public IReadOnlyList<PersonNameProjection> People =>
        World is null
            ? Array.Empty<PersonNameProjection>()
            : World.Persons
                .Select(person => new PersonNameProjection(person.Id, person.Name))
                .ToArray();

    public IReadOnlyList<OrganizationNameProjection> Organizations =>
        World is null
            ? Array.Empty<OrganizationNameProjection>()
            : World.Organizations
                .Select(organization => new OrganizationNameProjection(organization.Id, organization.Name))
                .ToArray();

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
            watchClock = null;
            lastOfficialChecksum = null;
            lastCommittedDecisionCount = null;
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

        if (World.IsRaceDue)
        {
            return CommandResult.Reject("RACE_DAY_PENDING");
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
            lastCommittedDecisionCount = null;
            LastWatchSecond = 0;
            LastSimSecond = 0;
            racePreparation = checkpoint.RacePreparation;
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

        State = GameState.RacePreparationFlow;
        racePreparation = new RacePreparationCheckpoint(
            RacePreparationDefaults.PrototypeScenarioId,
            PlanConfirmed: false,
            CareerRaceBinder.DefaultAssignments(World));
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

        if (World is null ||
            !CareerRaceBinder.HasLeaderAndCard(World, racePreparation.Assignments))
        {
            return CommandResult.Reject("PREP_ROLES_INCOMPLETE");
        }

        racePreparation = racePreparation with { PlanConfirmed = true };
        return CommandResult.Success;
    }

    public CommandResult Execute(AssignSquadRoleCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RacePreparationFlow || racePreparation is null || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        if (!SquadRoles.IsKnown(command.Role))
        {
            return CommandResult.Reject("PREP_ROLE_INVALID");
        }

        WorldEntityId[] squad = CareerRaceBinder.PlayerSquad(World);
        if (!squad.Contains(command.RiderId))
        {
            return CommandResult.Reject("PREP_RIDER_NOT_IN_SQUAD");
        }

        racePreparation = racePreparation with
        {
            PlanConfirmed = false,
            Assignments = CareerRaceBinder.AssignRole(World, racePreparation.Assignments, command.RiderId, command.Role),
        };
        return CommandResult.Success;
    }

    public CommandResult Execute(FollowHubPrimaryActionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.Management || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        if (World.IsRaceDue)
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
            RaceScenario scenario = ResolveCareerScenario(command.RaceScenarioId);
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
            RaceScenario scenario = ResolveCareerScenario(command.RaceScenarioId);
            RaceResult result = raceEngine.RunBatch(scenario, DeriveRaceSeed(World, scenario));
            CommitOfficialResult(result);
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
                    CommitOfficialResult(result);
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
                CommitOfficialResult(result);
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
        lastCommittedDecisionCount = null;
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

    private void CommitOfficialResult(RaceResult result)
    {
        ArgumentNullException.ThrowIfNull(World);
        World.RecordRace(new RaceSummary(result.RouteId, result.WinnerId, result.FinishOrder));
        lastOfficialChecksum = result.Checksum;
        lastCommittedDecisionCount = result.DecisionCount;
        activeRaceSession = null;
        watchClock = null;
        State = GameState.RaceResultsFlow;
    }

    private static bool IsLegalSaveState(GameState state)
    {
        return state is GameState.Management or
            GameState.PreSeasonPlanningFlow or
            GameState.RacePreparationFlow or
            GameState.RaceResultsFlow or
            GameState.RaceDebriefFlow;
    }

    private RaceScenario ResolveCareerScenario(string scenarioId)
    {
        RaceScenario fixture = raceScenarioCatalog.Resolve(scenarioId);
        return World is null ? fixture : CareerRaceBinder.Bind(fixture, World);
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
        List<Organization> organizations = new(recipe.Organizations.Count);
        Dictionary<string, WorldEntityId> organizationIds = new(StringComparer.Ordinal);
        foreach (OrganizationDefinition definition in recipe.Organizations)
        {
            WorldEntityId organizationId = allocator.Allocate();
            organizationIds.Add(definition.Id, organizationId);
            organizations.Add(new Organization(
                organizationId,
                definition.Id,
                definition.Name,
                daysSimulated: 0,
                definition.RacePrototypeTeamId));
        }

        List<RiderDefinition> orderedRiders = recipe.Riders
            .OrderBy(rider => rider.RacePrototypeRiderId, StringComparer.Ordinal)
            .ToList();
        List<Person> persons = new(orderedRiders.Count + 1);
        List<RosterRider> roster = new(orderedRiders.Count);
        foreach (RiderDefinition rider in orderedRiders)
        {
            WorldEntityId personId = allocator.Allocate();
            persons.Add(new Person(personId, rider.Name));
            roster.Add(new RosterRider(
                personId,
                organizationIds[rider.OrganizationId],
                rider.Id,
                rider.RacePrototypeRiderId));
        }

        WorldEntityId managerPersonId = allocator.Allocate();
        persons.Add(new Person(managerPersonId, recipe.Manager.Name));
        WorldEntityId managerCareerId = allocator.Allocate();
        WorldEntityId employmentId = allocator.Allocate();
        WorldEntityId humanAuthorityId = allocator.Allocate();
        WorldEntityId firstAiAuthorityId = allocator.Allocate();
        WorldEntityId secondAiAuthorityId = allocator.Allocate();
        ManagerCareer managerCareer = new(managerCareerId, managerPersonId, employmentId);
        Employment employment = new(
            employmentId,
            managerCareerId,
            organizations[0].Id,
            new WorldDate(0),
            null);
        DecisionAuthority[] authorities =
        {
            new(humanAuthorityId, DecisionAuthorityKind.HumanInput),
            new(firstAiAuthorityId, DecisionAuthorityKind.AIInput),
            new(secondAiAuthorityId, DecisionAuthorityKind.AIInput),
        };
        int calendarPeriodDays = ReadCalendarPeriodDays(recipe);
        IReadOnlyList<CalendarEntry> calendarEntries = SkeletonCalendar.CreateSeason(
            allocator,
            seasonIndex: 0,
            calendarPeriodDays);

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
            authorities,
            raceCount: 0,
            lastRace: null,
            calendarPeriodDays: calendarPeriodDays,
            calendarEntries: calendarEntries,
            rosterRiders: roster);
    }

    private static string CurrentRaceTitle(WorldState? world)
    {
        if (world is null)
        {
            return RacePreparationDefaults.Title;
        }

        CalendarEntry? today = world.CalendarEntries.FirstOrDefault(entry =>
            entry.Kind == CalendarEntryKind.Race &&
            entry.DayNumber == world.CurrentDate.DayNumber);
        if (today is not null)
        {
            return today.Title;
        }

        CalendarEntry? next = world.CalendarEntries
            .Where(entry =>
                entry.Kind == CalendarEntryKind.Race &&
                entry.OfficialResult is null &&
                entry.DayNumber >= world.CurrentDate.DayNumber)
            .OrderBy(entry => entry.DayNumber)
            .FirstOrDefault();
        return next?.Title ?? RacePreparationDefaults.Title;
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
