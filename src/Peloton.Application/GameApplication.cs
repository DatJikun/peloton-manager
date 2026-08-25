using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation;

namespace Peloton.Application;

public sealed class GameApplication
{
    private readonly IScenarioCatalog scenarioCatalog;
    private readonly IWorldSaveStore saveStore;
    private readonly StubRaceEngine raceEngine;
    private PendingRace? pendingRace;

    public GameApplication(
        IScenarioCatalog scenarioCatalog,
        IWorldSaveStore saveStore,
        StubRaceEngine raceEngine)
    {
        this.scenarioCatalog = scenarioCatalog ?? throw new ArgumentNullException(nameof(scenarioCatalog));
        this.saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
        this.raceEngine = raceEngine ?? throw new ArgumentNullException(nameof(raceEngine));
    }

    public GameState State { get; private set; } = GameState.MainMenu;

    public WorldState? World { get; private set; }

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

        DeterministicScheduler.AdvanceDay(World);
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
            pendingRace = null;
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

        pendingRace = new PendingRace(command.RouteId, command.StartList.ToArray());
        State = GameState.RaceLive;
        return CommandResult.Success;
    }

    public CommandResult Execute(CompleteStubRaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (State != GameState.RaceLive || World is null || pendingRace is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        StubRaceResult result = raceEngine.Run(
            World.MasterSeed,
            pendingRace.RouteId,
            pendingRace.StartList,
            checked(World.RaceCount + 1));
        World.RecordStubRace(new StubRaceSummary(result.RouteId, result.WinnerId, result.FinishOrder));
        pendingRace = null;
        State = GameState.RaceResultsFlow;
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
            new[] { authority });
    }

    private sealed record PendingRace(string RouteId, IReadOnlyList<WorldEntityId> StartList);
}
