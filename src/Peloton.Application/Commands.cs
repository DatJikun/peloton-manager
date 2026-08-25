using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record CreateWorldCommand(string ScenarioId, long Seed);

public sealed record AdvanceDayCommand;

public sealed record SaveGameCommand(string Path);

public sealed record LoadGameCommand(string Path);

public sealed record PrepareRaceCommand;

public sealed record StartRaceCommand(
    string PreRaceAutosavePath,
    string RouteId,
    IReadOnlyList<WorldEntityId> StartList);

public sealed record CompleteStubRaceCommand;

public sealed record AcknowledgeRaceResultsCommand;

public sealed record CompleteRaceDebriefCommand;

public sealed record CommandResult(bool Succeeded, string ReasonCode)
{
    public static CommandResult Success { get; } = new(true, "OK");

    public static CommandResult Reject(string reasonCode) => new(false, reasonCode);
}
