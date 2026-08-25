using Peloton.Application;
using Peloton.Content;
using Peloton.Persistence;
using Peloton.Simulation;

namespace Peloton.Infrastructure;

public static class InfrastructureAssembly
{
}

public static class ApplicationFactory
{
    public static GameApplication Create(string contentRoot)
    {
        return new GameApplication(
            new JsonScenarioCatalog(contentRoot),
            new SqliteWorldSaveStore(),
            new StubRaceEngine());
    }
}
