using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerWorldTourBindTests
{
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";
    private const long GateSeed = 91234;

    [Fact]
    public void FinishOrderUsesRiderCareerIdsPresentBeforeTheRace()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        WorldEntityId[] knownIds = application.World!.RiderCareers.Select(career => career.Id).ToArray();
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        CareerWorldTestSupport.AssertFinishOrderUsesWorldRiderCareers(application);
        Assert.All(application.World.LastRace!.FinishOrder, id => Assert.Contains(id, knownIds));
    }

    [Fact]
    public void SameSeedProducesSameFinishOrderAndCareerHistory()
    {
        GameApplication first = RunBoundRace(GateSeed);
        GameApplication second = RunBoundRace(GateSeed);

        Assert.Equivalent(first.World!.LastRace, second.World!.LastRace, strict: true);
        Assert.Equal(
            first.World.RiderCareers.Select(career => career.Results.Count).ToArray(),
            second.World.RiderCareers.Select(career => career.Results.Count).ToArray());
        Assert.Equal(
            first.World.RiderCareers.SelectMany(career => career.Results).ToArray(),
            second.World.RiderCareers.SelectMany(career => career.Results).ToArray());
    }

    [Fact]
    public void SchemaVersionSixRoundTripsRiderCareersAndResults()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "career-bind.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(source.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(source).Succeeded);
        Assert.True(source.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        WorldCheckpoint stored = new SqliteWorldSaveStore().Load(savePath);
        Assert.Equal(12, stored.World.RiderCareers.Count);
        Assert.Equal(12, stored.World.RiderContracts.Count);
        Assert.All(stored.World.RiderCareers, career => Assert.Single(career.Results));

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(
            WorldChecksum.Compute(source.World!),
            WorldChecksum.Compute(loaded.World!));
    }

    [Fact]
    public void PrepSquadIsEmployerWorldRoster()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);

        RacePreparationProjection prep = Assert.IsType<RacePreparationProjection>(application.RacePreparation);
        Assert.Equal(CareerWorldTestSupport.EmployerSquadCareerIds(application), prep.Squad.Select(id => id.Value));
    }

    private static GameApplication RunBoundRace(long seed)
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", seed)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);
        return application;
    }
}
