using System;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Peloton.Simulation.Course;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CdARoadTtTests
{
    private const long GateSeed = 91234;
    private const string WtScenarioId = "scenario.peloton.wt-2026";

    [Fact]
    public void LegacyCdAM2FillsBothAeroNumbers()
    {
        (double road, double timeTrial) = CdAJson.Resolve(null, null, 0.29);
        Assert.Equal(0.29, road);
        Assert.Equal(0.29, timeTrial);
    }

    [Fact]
    public void WorldTourPackHasTwoCdAKeysAndNamedTtSpecialistsAreMostAero()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        RiderCareer ganna = world.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, "rider.wt2026.ineos.support-2", StringComparison.Ordinal));
        RiderCareer evenepoel = world.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, "rider.wt2026.redbull.leader", StringComparison.Ordinal));
        double[] others = world.RiderCareers
            .Where(career => career.Id != ganna.Id && career.Id != evenepoel.Id)
            .Select(career => career.CdATtM2)
            .ToArray();
        Assert.True(ganna.CdATtM2 < evenepoel.CdATtM2);
        Assert.True(evenepoel.CdATtM2 < others.Min());
        Assert.All(world.RiderCareers, career => Assert.True(career.CdARoadM2 > 0.0 && career.CdATtM2 > 0.0));
        RiderCareer philipsen = world.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, "rider.wt2026.alpecin.card", StringComparison.Ordinal));
        Assert.True(philipsen.CdATtM2 > evenepoel.CdATtM2);
        Assert.True(philipsen.CdATtM2 > ganna.CdATtM2);
    }

    [Fact]
    public void EvenepoelTimeTrialRatingBeatsPogacarAndPhilipsenIsWeak()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        RiderRatingSet evenepoel = Ratings(world, "rider.wt2026.redbull.leader");
        RiderRatingSet pogacar = Ratings(world, "rider.wt2026.uae.leader");
        RiderRatingSet philipsen = Ratings(world, "rider.wt2026.alpecin.card");
        Assert.True(evenepoel.TimeTrial >= 90, $"evenepoel TT={evenepoel.TimeTrial}");
        Assert.True(evenepoel.TimeTrial >= pogacar.TimeTrial, $"evenepoel={evenepoel.TimeTrial} pogacar={pogacar.TimeTrial}");
        Assert.True(philipsen.TimeTrial < 70, $"philipsen TT={philipsen.TimeTrial}");
    }

    [Fact]
    public void SchemaTenRoundTripsBothCdAValues()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "cda-v10.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        RiderCareer before = source.World!.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, "rider.wt2026.redbull.leader", StringComparison.Ordinal));
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);
        Assert.Equal(11, SqliteWorldSaveStore.SchemaVersion);

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        RiderCareer after = loaded.World!.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, "rider.wt2026.redbull.leader", StringComparison.Ordinal));
        Assert.Equal(before.CdARoadM2, after.CdARoadM2);
        Assert.Equal(before.CdATtM2, after.CdATtM2);
        Assert.Equal(WorldChecksum.Compute(source.World), WorldChecksum.Compute(loaded.World));
    }

    [Fact]
    public void FirstStoredIttHasTtWinnerEvenepoelTopThreeAndIncreasingTimes()
    {
        WorldState world = CreateWorld();
        CourseProfile itt = world.CourseProfiles
            .Where(profile => profile.ClassifiedStageType == ClassifiedStageType.IndividualTimeTrial)
            .OrderBy(profile => profile.RaceContentId, StringComparer.Ordinal)
            .ThenBy(profile => profile.StageIndex)
            .First();
        RaceResult result = Simulate(world, itt);
        string winnerArchetype = ArchetypeOf(world, result.WinnerId);
        Assert.True(winnerArchetype is "tt" or "super-gc", $"winner={winnerArchetype}");
        int evenepoelPlace = PlaceOfOrigin(result, world, "rider.wt2026.redbull.leader");
        int philipsenPlace = PlaceOfOrigin(result, world, "rider.wt2026.alpecin.card");
        Assert.True(evenepoelPlace is > 0 and <= 3, $"evenepoel={evenepoelPlace}");
        Assert.True(philipsenPlace > 100 || philipsenPlace < 0, $"philipsen={philipsenPlace}");
        double previous = double.NegativeInfinity;
        foreach (RaceRiderMetrics metric in result.RiderMetrics.OrderBy(item => item.FinishTimeSeconds))
        {
            Assert.True(metric.FinishTimeSeconds > previous + 0.01 || Math.Abs(metric.FinishTimeSeconds - previous) < 1e-9);
            previous = metric.FinishTimeSeconds;
        }

        RaceResult second = Simulate(world, itt);
        Assert.Equal(result.Checksum, second.Checksum);
        Assert.Equal(result.FinishOrder, second.FinishOrder);
    }

    private static WorldState CreateWorld()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        return application.World!;
    }

    private static RaceResult Simulate(WorldState world, CourseProfile courseProfile)
    {
        WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot).Resolve(WtScenarioId);
        RaceScenarioTemplate template = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
            .ResolveTemplate("race-scenario.peloton.prototype-v0");
        RaceScenario scenario = WorldRaceScenarioAssembler.Assemble(
            world,
            recipe,
            template,
            courseProfile.RaceContentId,
            courseProfile: courseProfile,
            masterSeed: GateSeed);
        return new PrototypeRaceEngine().RunBatch(scenario, GateSeed);
    }

    private static RiderRatingSet Ratings(WorldState world, string originId)
    {
        RiderCareer career = world.RiderCareers.Single(
            item => string.Equals(item.OriginDefinitionId, originId, StringComparison.Ordinal));
        return RiderRatingQueries.FromPhysiology(career, career.PotentialOvr);
    }

    private static string ArchetypeOf(WorldState world, WorldEntityId riderId)
    {
        RiderCareer career = world.TryGetRiderCareer(riderId)
            ?? throw new InvalidOperationException($"Missing career {riderId.Value}.");
        string path = Path.Combine(TestApplication.ContentRoot, "peloton.wt-2026", "roster.json");
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        foreach (System.Text.Json.JsonElement rider in document.RootElement.GetProperty("riders").EnumerateArray())
        {
            if (string.Equals(rider.GetProperty("id").GetString(), career.OriginDefinitionId, StringComparison.Ordinal))
            {
                return rider.GetProperty("archetype").GetString()!;
            }
        }

        throw new InvalidOperationException(career.OriginDefinitionId);
    }

    private static int PlaceOfOrigin(RaceResult result, WorldState world, string originId)
    {
        RiderCareer career = world.RiderCareers.Single(
            item => string.Equals(item.OriginDefinitionId, originId, StringComparison.Ordinal));
        for (int index = 0; index < result.FinishOrder.Count; index++)
        {
            if (result.FinishOrder[index] == career.Id)
            {
                return index + 1;
            }
        }

        return -1;
    }
}
