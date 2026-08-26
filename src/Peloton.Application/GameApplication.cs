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

    public CareerDayProjection? CareerDay
    {
        get
        {
            if (World is null)
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
            return new CareerDayProjection(
                World.CurrentDate.DayNumber,
                manager?.Name ?? string.Empty,
                employer?.Name,
                World.DaysUntilNextRace,
                World.NextRaceDayNumber,
                World.IsRaceDue,
                World.LastDayNotes,
                World.RaceCount);
        }
    }

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
            saveStore.Save(command.Path, new WorldCheckpoint(State, World));
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
        return CommandResult.Success;
    }

    public CommandResult Execute(StartRaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RacePreparationFlow || World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        try
        {
            saveStore.Save(command.PreRaceAutosavePath, new WorldCheckpoint(State, World));
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            return CommandResult.Reject("PRE_RACE_AUTOSAVE_FAILED");
        }

        try
        {
            RaceScenario scenario = raceScenarioCatalog.Resolve(command.RaceScenarioId);
            long raceSeed = unchecked((long)StableSeedDerivation.Derive(
                World.MasterSeed,
                $"official-race-v1:{World.RaceCount + 1}:{scenario.Id}:{scenario.TuningIdentity}"));
            activeRaceSession = raceEngine.CreateSession(scenario, raceSeed);
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
                    World.RecordRace(new RaceSummary(result.RouteId, result.WinnerId, result.FinishOrder));
                    activeRaceSession = null;
                    State = GameState.RaceResultsFlow;
                }

                return CommandResult.Success;
            }
        }
        catch (InvalidOperationException)
        {
            return CommandResult.Reject("RACE_ADVANCE_FAILED");
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

        State = GameState.Management;
        return CommandResult.Success;
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

    private static bool IsLegalSaveState(GameState state)
    {
        return state is GameState.Management or
            GameState.PreSeasonPlanningFlow or
            GameState.RacePreparationFlow or
            GameState.RaceResultsFlow or
            GameState.RaceDebriefFlow;
    }

    private static WorldState CreateWorld(WorldRecipe recipe, long seed)
    {
        WorldEntityIdAllocator allocator = new();
        List<Organization> organizations = new(recipe.Organizations.Count);
        foreach (OrganizationDefinition definition in recipe.Organizations)
        {
            organizations.Add(new Organization(
                allocator.Allocate(),
                definition.Id,
                definition.Name));
        }

        List<Person> persons = new(recipe.Organizations.Count);
        for (int index = 0; index < recipe.Organizations.Count; index++)
        {
            persons.Add(new Person(allocator.Allocate(), $"Skeleton Rider {index + 1}"));
        }

        WorldEntityId managerCareerId = allocator.Allocate();
        WorldEntityId employmentId = allocator.Allocate();
        WorldEntityId authorityId = allocator.Allocate();
        ManagerCareer managerCareer = new(managerCareerId, persons[0].Id, employmentId);
        Employment employment = new(
            employmentId,
            managerCareerId,
            organizations[0].Id,
            new WorldDate(0),
            null);
        DecisionAuthority authority = new(authorityId, DecisionAuthorityKind.HumanInput);

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
            calendarPeriodDays: ReadCalendarPeriodDays(recipe));
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
