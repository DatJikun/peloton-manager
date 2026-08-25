using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record OrganizationDefinition(string Id, string Name);

public sealed record WorldRecipe(
    ContentIdentity ContentIdentity,
    IReadOnlyList<RulesModuleIdentity> RulesModules,
    string RulesIdentity,
    IReadOnlyList<OrganizationDefinition> Organizations);

public interface IScenarioCatalog
{
    WorldRecipe Resolve(string scenarioId);
}

public sealed record WorldCheckpoint(GameState GameState, WorldState World);

public interface IWorldSaveStore
{
    void Save(string path, WorldCheckpoint checkpoint);

    WorldCheckpoint Load(string path);
}
