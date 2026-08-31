using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record OrganizationDefinition(string Id, string Name, string RacePrototypeTeamId);

public sealed record RiderDefinition(
    string Id,
    string Name,
    string OrganizationId,
    string RacePrototypeRiderId);

public sealed record ManagerDefinition(string Id, string Name);

public sealed record WorldRecipe(
    ContentIdentity ContentIdentity,
    IReadOnlyList<RulesModuleIdentity> RulesModules,
    string RulesIdentity,
    IReadOnlyList<OrganizationDefinition> Organizations,
    IReadOnlyList<RiderDefinition> Riders,
    ManagerDefinition Manager);

public interface IScenarioCatalog
{
    WorldRecipe Resolve(string scenarioId);
}

public sealed record WorldCheckpoint(
    GameState GameState,
    WorldState World,
    RacePreparationCheckpoint? RacePreparation = null);

public interface IWorldSaveStore
{
    void Save(string path, WorldCheckpoint checkpoint);

    WorldCheckpoint Load(string path);
}
