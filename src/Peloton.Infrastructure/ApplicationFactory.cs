using Peloton.Application;
using Peloton.Content;
using Peloton.Persistence;
using Peloton.Simulation;
using Peloton.Simulation.Race;

namespace Peloton.Infrastructure;

public static class InfrastructureAssembly
{
}

public static class ApplicationFactory
{
    public static GameApplication Create(string contentRoot)
    {
        SeasonRolloverExecutor.AgingTick = SeasonAging.Apply;
        SeasonRolloverExecutor.RetirementTick = SeasonRetirements.Apply;
        SeasonRolloverExecutor.NeoProTick = SeasonNeoPros.Apply;
        SeasonRolloverExecutor.ContractTick = SeasonAiContracts.Apply;
        SeasonRolloverExecutor.SeasonSummaryTick = SeasonInboxSupport.PublishSeasonSummary;
        SeasonNeoPros.ContentRoot = contentRoot;
        RiderMetadataCatalog.ContentRoot = contentRoot;
        SeasonRolloverExecutor.RegisterApplicator();
        return new GameApplication(
            new JsonScenarioCatalog(contentRoot),
            new JsonRacePrototypeCatalog(contentRoot),
            new SqliteWorldSaveStore(),
            new PrototypeRaceEngine());
    }
}
