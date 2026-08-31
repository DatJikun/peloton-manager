using System.Collections.Generic;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record OrganizationDefinition(string Id, string Name);

public sealed record TeamRaceMappingDefinition(string OrganizationId, string RaceTeamId);

public sealed record ManagerDefinition(string Name, string OrganizationId);

public sealed record RiderDefinition(
    string Id,
    string Name,
    string OrganizationId,
    double CriticalPowerW,
    double WPrimeCapacityJ,
    double PeakPowerW,
    double WPrimeRecoveryJPerSecond,
    double LowIntensityDurability,
    double HighIntensityDurability,
    double BodyMassKg,
    double SystemMassKg,
    double CdAM2,
    double BaseCrr,
    double Positioning,
    double Handling,
    double TacticalAwareness,
    int AnnualWage,
    int ContractEndDay,
    double Loyalty01 = 0.5);

public sealed record WorldRecipe(
    ContentIdentity ContentIdentity,
    IReadOnlyList<RulesModuleIdentity> RulesModules,
    string RulesIdentity,
    IReadOnlyList<OrganizationDefinition> Organizations,
    IReadOnlyList<TeamRaceMappingDefinition> TeamRaceMappings,
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
