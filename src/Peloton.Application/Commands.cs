using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public sealed record CreateWorldCommand(string ScenarioId, long Seed, string? EmployerOrganizationOriginId = null);

public sealed record AdvanceDayCommand;

public sealed record SaveGameCommand(string Path);

public sealed record LoadGameCommand(string Path);

public sealed record PrepareRaceCommand;

public sealed record CancelRacePreparationCommand;

public sealed record ConfirmRacePreparationPlanCommand;

public sealed record SetRacePreparationStrategyCommand(
    WorldEntityId LeaderId,
    WorldEntityId SupportId,
    RaceObjective Objective,
    RaceBriefingKind BriefingKind);

public sealed record BeginPreSeasonPlanningCommand;

public sealed record SetSeasonRaceEntryCommand(string RaceContentId, bool Entered);

public sealed record SetSeasonRaceLeaderCommand(string RaceContentId, WorldEntityId LeaderCareerId);

public sealed record ConfirmPreSeasonPlanCommand;

public sealed record CancelPreSeasonPlanningCommand;

public sealed record FollowHubPrimaryActionCommand;

public sealed record StartRaceCommand(
    string PreRaceAutosavePath,
    string RaceScenarioId);

public sealed record SimulateRaceCommand(string RaceScenarioId);

public sealed record AdvanceRaceCommand;

public sealed record BeginRaceWatchCommand(int Rate);

public sealed record AdvanceRaceWatchCommand;

public sealed record AbandonRaceLiveCommand(string PreRaceAutosavePath);

public sealed record RespondToRaceDecisionCommand(
    RaceDecisionRequestId RequestId,
    WorldEntityId AuthorityId,
    RaceDecisionOption SelectedOption);

public sealed record AcknowledgeRaceResultsCommand;

public sealed record CompleteRaceDebriefCommand;

public sealed record ArchiveInboxItemCommand(string Identity);

public sealed record BeginContractNegotiationCommand(WorldEntityId RiderCareerId);

public sealed record SetContractOfferCommand(int AnnualWage, int ContractEndDay);

public sealed record ConfirmContractOfferCommand;

public sealed record CancelContractNegotiationCommand;

public sealed record CommandResult(bool Succeeded, string ReasonCode)
{
    public static CommandResult Success { get; } = new(true, "OK");

    public static CommandResult Reject(string reasonCode) => new(false, reasonCode);
}
