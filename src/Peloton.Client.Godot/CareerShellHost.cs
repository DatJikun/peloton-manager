using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public sealed class CareerShellHost
{
    public const long SkeletonSeed = 91234;
    public const string SkeletonScenarioId = "scenario.peloton.skeleton";
    public const string WorldTourScenarioId = "scenario.peloton.wt-2026";
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

    public IReadOnlyList<PersonNameProjection> People =>
        application.World is null
            ? Array.Empty<PersonNameProjection>()
            : application.World.Persons
                .Select(person => new PersonNameProjection(person.Id, person.Name))
                .ToArray();

    public IReadOnlyList<OrganizationNameProjection> Organizations =>
        application.World is null
            ? Array.Empty<OrganizationNameProjection>()
            : application.World.Organizations
                .Select(organization => new OrganizationNameProjection(organization.Id, organization.Name))
                .ToArray();

    public RacePreparationProjection? Preparation => application.RacePreparation;

    public RaceResultProjection? Result => application.RaceResult;

    public ClassificationProjection? Classifications => application.RaceClassifications;

    public RaceDebriefProjection? Debrief => application.RaceDebrief;

    public PreSeasonPlanningProjection? PreSeasonPlanning => application.PreSeasonPlanning;

    public ClubRosterProjection? ClubRoster => application.ClubRoster;

    public ClubFinanceProjection? ClubFinance => application.ClubFinance;

    public ContractNegotiationProjection? ContractNegotiation => application.ContractNegotiation;

    public bool IsWorldTourWorld =>
        string.Equals(
            application.World?.ContentIdentity.ScenarioId,
            WorldTourScenarioId,
            StringComparison.Ordinal);

    public WorldEntityId? ResultTeamFilter => resultTeamFilter;

    public IReadOnlyList<OrganizationNameProjection> ResultTeams =>
        Result is null
            ? Array.Empty<OrganizationNameProjection>()
            : Result.FinishOrder
                .Where(row => row.OrganizationId is { })
                .GroupBy(row => row.OrganizationId!.Value)
                .Select(group => new OrganizationNameProjection(
                    group.Key,
                    group.First().OrganizationName))
                .OrderBy(team => team.Name, StringComparer.Ordinal)
                .ToArray();

    public IReadOnlyList<RaceResultPlacement> VisibleResultTable =>
        Result is null
            ? Array.Empty<RaceResultPlacement>()
            : resultTeamFilter is { } organizationId
                ? RaceOutcomeQueries.FilterFinishOrderByOrganization(Result.FinishOrder, organizationId)
                : Result.FinishOrder;

    public WatchRaceHost? Watch { get; private set; }

    public CommandResult OpenSkeleton(long seed = SkeletonSeed)
    {
        resultTeamFilter = null;
        Watch = null;
        return application.Execute(new CreateWorldCommand(SkeletonScenarioId, seed));
    }

    public CommandResult OpenWorldTour(string employerOriginId, long seed = SkeletonSeed)
    {
        resultTeamFilter = null;
        Watch = null;
        return application.Execute(new CreateWorldCommand(WorldTourScenarioId, seed, employerOriginId));
    }

    public IReadOnlyList<NewGameClubProjection> ListNewGameClubs(string scenarioId) =>
        application.ListNewGameClubs(scenarioId);

    public CommandResult BeginPreSeasonPlanning() =>
        application.Execute(new BeginPreSeasonPlanningCommand());

    public CommandResult SetSeasonRaceEntry(string raceContentId, bool entered) =>
        application.Execute(new SetSeasonRaceEntryCommand(raceContentId, entered));

    public CommandResult SetSeasonRaceLeader(string raceContentId, WorldEntityId leaderId) =>
        application.Execute(new SetSeasonRaceLeaderCommand(raceContentId, leaderId));

    public CommandResult ConfirmPreSeasonPlan() =>
        application.Execute(new ConfirmPreSeasonPlanCommand());

    public CommandResult CancelPreSeasonPlanning() =>
        application.Execute(new CancelPreSeasonPlanningCommand());

    public CommandResult BeginContractNegotiation(WorldEntityId riderCareerId) =>
        application.Execute(new BeginContractNegotiationCommand(riderCareerId));

    public CommandResult SetContractOffer(int annualWage, int contractEndDay) =>
        application.Execute(new SetContractOfferCommand(annualWage, contractEndDay));

    public CommandResult ConfirmContractOffer() =>
        application.Execute(new ConfirmContractOfferCommand());

    public CommandResult CancelContractNegotiation() =>
        application.Execute(new CancelContractNegotiationCommand());

    public string RiderDisplayName(WorldEntityId riderId)
    {
        if (application.World is not WorldState world)
        {
            return riderId.Value.ToString(CultureInfo.InvariantCulture);
        }

        RiderCareer? career = world.TryGetRiderCareer(riderId);
        WorldEntityId personId = career?.PersonId ?? riderId;
        Person? person = world.Persons.FirstOrDefault(item => item.Id == personId);
        if (person is not null && !string.IsNullOrWhiteSpace(person.Name))
        {
            return person.Name;
        }

        return career?.OriginDefinitionId ?? riderId.Value.ToString(CultureInfo.InvariantCulture);
    }

    public void SetResultTeamFilter(WorldEntityId? teamId)
    {
        if (teamId is { } id && ResultTeams.All(team => team.Id != id))
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

    public CommandResult SetLeader(WorldEntityId riderId)
    {
        if (Preparation is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        WorldEntityId support = Preparation.SupportId is { } current && current != riderId
            ? current
            : Preparation.Squad.FirstOrDefault(id => id != riderId);
        if (support.Value == 0)
        {
            return CommandResult.Reject("PREP_STRATEGY_RIDERS_INVALID");
        }

        return application.Execute(new SetRacePreparationStrategyCommand(
            riderId,
            support,
            Preparation.ObjectiveKind ?? RaceObjective.StageWin,
            Preparation.BriefingKind ?? RaceBriefingKind.Chase));
    }

    public CommandResult ConfirmPreparation()
    {
        if (Preparation is { StrategySet: false })
        {
            return RacePreparationSupport.ConfirmWithDefaultStrategy(application);
        }

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
        if (resultTeamFilter is { } id && ResultTeams.All(team => team.Id != id))
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
