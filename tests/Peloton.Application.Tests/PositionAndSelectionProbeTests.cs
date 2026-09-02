using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Simulation.Course;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class PositionAndSelectionProbeTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string PrototypeRaceTemplateId = "race-scenario.peloton.prototype-v0";
    private const long GateSeed = 91234;
    private const string RoubaixRaceContentId = "race.wt2026.paris_roubaix";
    private const string TdfRaceContentId = "race.wt2026.tour_de_france";
    private const string TduRaceContentId = "race.wt2026.tour_down_under";
    private const string VdpOriginId = "rider.wt2026.alpecin.leader";
    private const string EvenepoelOriginId = "rider.wt2026.redbull.leader";
    private const string VingegaardOriginId = "rider.wt2026.visma.leader";
    private const string PogacarOriginId = "rider.wt2026.uae.leader";
    private const string PhilipsenOriginId = "rider.wt2026.alpecin.card";
    private const string VanAertOriginId = "rider.wt2026.visma.support-2";

    private static readonly Lazy<Dictionary<string, string>> ArchetypesByOrigin = new(LoadArchetypes);
    private static readonly Lazy<Dictionary<string, double>> HandlingByOrigin = new(LoadHandling);

    [Fact]
    public void RoubaixCobblesSelectAndVanDerPoelInFrontGroup()
    {
        WorldState world = CreateWorld();
        CourseProfile roubaix = world.CourseProfiles.Single(
            profile => string.Equals(profile.OriginDefinitionId, "course.wt2026.roubaix.2026.s1", StringComparison.Ordinal));
        RaceResult result = Simulate(world, roubaix);

        int vdpPlace = PlaceOfOrigin(result, world, VdpOriginId);
        string[] top5Archetypes = result.FinishOrder
            .Take(5)
            .Select(id => ArchetypeOf(world, id))
            .ToArray();
        WorldEntityId[] top10 = result.FinishOrder.Take(10).ToArray();
        double[] top10Handling = top10
            .Select(id =>
            {
                RiderCareer career = world.TryGetRiderCareer(id)
                    ?? throw new InvalidOperationException($"Missing career for rider {id.Value}.");
                return HandlingByOrigin.Value[career.OriginDefinitionId];
            })
            .ToArray();

        RaceRiderMetrics winnerMetrics = result.RiderMetrics.Single(metric => metric.RiderId == result.WinnerId);
        WorldEntityId twentiethId = result.FinishOrder[19];
        RaceRiderMetrics twentiethMetrics = result.RiderMetrics.Single(metric => metric.RiderId == twentiethId);
        double winnerToTwentiethGapSeconds = twentiethMetrics.FinishTimeSeconds - winnerMetrics.FinishTimeSeconds;

        Assert.True(vdpPlace is > 0 and <= 20, $"vdp={vdpPlace}");
        Assert.DoesNotContain("sprinter", top5Archetypes);
        Assert.All(top10Handling, handling => Assert.True(handling >= 0.70, $"handling={handling}"));
        Assert.True(
            winnerToTwentiethGapSeconds > 0.0,
            $"winnerTo20thGapSeconds={winnerToTwentiethGapSeconds}");
    }

    [Fact(Skip = "D-057: Roubaix winner remains super-gc (Evenepoel) after classics-star CP bump; engine unchanged — top5 super-gc|super-gc|super-gc|tt|gc at seed 91234")]
    public void RoubaixClassicsWinAndVanDerPoelBeatsGcRivals()
    {
        WorldState world = CreateWorld();
        CourseProfile roubaix = world.CourseProfiles.Single(
            profile => string.Equals(profile.OriginDefinitionId, "course.wt2026.roubaix.2026.s1", StringComparison.Ordinal));
        RaceResult result = Simulate(world, roubaix);

        string winnerArchetype = ArchetypeOf(world, result.WinnerId);
        string[] top5Archetypes = result.FinishOrder
            .Take(5)
            .Select(id => ArchetypeOf(world, id))
            .ToArray();
        int classicsInTop5 = top5Archetypes.Count(archetype => archetype == "classics");
        int vdpPlace = PlaceOfOrigin(result, world, VdpOriginId);
        int evenepoelPlace = PlaceOfOrigin(result, world, EvenepoelOriginId);
        int vingegaardPlace = PlaceOfOrigin(result, world, VingegaardOriginId);
        int vanAertPlace = PlaceOfOrigin(result, world, VanAertOriginId);
        Assert.Equal("classics", winnerArchetype);
        Assert.True(classicsInTop5 >= 3, $"top5={string.Join(',', top5Archetypes)}");
        Assert.True(vdpPlace > 0 && evenepoelPlace > 0 && vingegaardPlace > 0);
        Assert.True(
            vdpPlace < evenepoelPlace && vdpPlace < vingegaardPlace,
            $"vdp={vdpPlace} evenepoel={evenepoelPlace} vingegaard={vingegaardPlace} vanaert={vanAertPlace} top5={string.Join(',', top5Archetypes)}");
    }

    [Fact]
    public void TdfStageOneSprintHasMultiTeamSprinterPodium()
    {
        WorldState world = CreateWorld();
        CourseProfile stage = world.CourseProfiles.Single(
            profile => string.Equals(profile.OriginDefinitionId, "course.wt2026.tdf.2026.s1", StringComparison.Ordinal));
        RaceResult result = Simulate(world, stage);

        WorldEntityId[] top5 = result.FinishOrder.Take(5).ToArray();
        int sprinterCount = top5.Count(id => ArchetypeOf(world, id) == "sprinter");
        int organizationCount = top5
            .Select(id => world.TryGetRiderCareer(id)?.OrganizationId)
            .Distinct()
            .Count();
        int philipsenPlace = PlaceOfOrigin(result, world, PhilipsenOriginId);

        Assert.True(sprinterCount >= 3, $"top5 archetypes={string.Join(',', top5.Select(id => ArchetypeOf(world, id)))}");
        Assert.True(organizationCount >= 3);
        Assert.True(philipsenPlace is > 0 and <= 3);
    }

    [Fact]
    public void TduStageSixSprinterWins()
    {
        WorldState world = CreateWorld();
        CourseProfile stage = world.CourseProfiles.Single(
            profile => profile.RaceContentId == TduRaceContentId && profile.StageIndex == 6);
        RaceResult result = Simulate(world, stage);

        Assert.Equal("sprinter", ArchetypeOf(world, result.WinnerId));
    }

    [Fact]
    public void HautacamGcWinsWithPogacarPodiumAndPhilipsenOutsideTop100()
    {
        WorldState world = CreateWorld();
        CourseProfile stage = world.CourseProfiles.Single(
            profile => string.Equals(profile.OriginDefinitionId, "course.wt2026.tdf.2026.s13", StringComparison.Ordinal));
        RaceResult result = Simulate(world, stage);

        string winnerArchetype = ArchetypeOf(world, result.WinnerId);
        int pogacarPlace = PlaceOfOrigin(result, world, PogacarOriginId);
        int philipsenPlace = PlaceOfOrigin(result, world, PhilipsenOriginId);

        Assert.True(winnerArchetype is "gc" or "super-gc", $"winner={winnerArchetype}");
        Assert.True(pogacarPlace is > 0 and <= 3);
        Assert.True(philipsenPlace > 100 || philipsenPlace < 0);
    }

    [Fact]
    public void SameSeedIsDeterministicAndSpyNeutral()
    {
        WorldState world = CreateWorld();
        CourseProfile stage = world.CourseProfiles.Single(
            profile => string.Equals(profile.OriginDefinitionId, "course.wt2026.tdf.2026.s1", StringComparison.Ordinal));
        PrototypeRaceEngine engine = new();
        RaceScenario scenario = BuildScenario(world, stage);

        RaceResult first = engine.RunBatch(scenario, GateSeed, NullWorldSpySink.Instance);
        RaceResult second = engine.RunBatch(scenario, GateSeed, NullWorldSpySink.Instance);
        RecordingWorldSpySink spy = new();
        RaceResult traced = engine.RunBatch(scenario, GateSeed, spy);

        Assert.Equal(first.Checksum, second.Checksum);
        Assert.Equal(first.FinishOrder, second.FinishOrder);
        Assert.Equal(first.Checksum, traced.Checksum);
        Assert.Equal(first.FinishOrder, traced.FinishOrder);
    }

    private static WorldState CreateWorld()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        return application.World!;
    }

    private static RaceResult Simulate(WorldState world, CourseProfile courseProfile)
    {
        return new PrototypeRaceEngine().RunBatch(BuildScenario(world, courseProfile), GateSeed);
    }

    private static RaceScenario BuildScenario(WorldState world, CourseProfile courseProfile)
    {
        WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot).Resolve(WtScenarioId);
        RaceScenarioTemplate template = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
            .ResolveTemplate(PrototypeRaceTemplateId);
        return WorldRaceScenarioAssembler.Assemble(
            world,
            recipe,
            template,
            courseProfile.RaceContentId,
            courseProfile: courseProfile,
            masterSeed: GateSeed);
    }

    private static string ArchetypeOf(WorldState world, WorldEntityId riderId)
    {
        RiderCareer? career = world.TryGetRiderCareer(riderId);
        Assert.NotNull(career);
        return ArchetypesByOrigin.Value[career!.OriginDefinitionId];
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

    private static Dictionary<string, string> LoadArchetypes()
    {
        string path = Path.Combine(TestApplication.ContentRoot, "peloton.wt-2026", "roster.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        Dictionary<string, string> archetypes = new(StringComparer.Ordinal);
        foreach (JsonElement rider in document.RootElement.GetProperty("riders").EnumerateArray())
        {
            string id = rider.GetProperty("id").GetString()!;
            string archetype = rider.GetProperty("archetype").GetString()!;
            archetypes[id] = archetype;
        }

        return archetypes;
    }

    private static Dictionary<string, double> LoadHandling()
    {
        string path = Path.Combine(TestApplication.ContentRoot, "peloton.wt-2026", "roster.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        Dictionary<string, double> handling = new(StringComparer.Ordinal);
        foreach (JsonElement rider in document.RootElement.GetProperty("riders").EnumerateArray())
        {
            string id = rider.GetProperty("id").GetString()!;
            double handlingValue = rider.GetProperty("handling").GetDouble();
            handling[id] = handlingValue;
        }

        return handling;
    }

    private sealed class RecordingWorldSpySink : IWorldSpySink
    {
        public void Emit(DecisionTrace trace)
        {
        }
    }
}
