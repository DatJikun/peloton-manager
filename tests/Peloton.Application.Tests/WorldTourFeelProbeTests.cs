using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Simulation.Course;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class WorldTourFeelProbeTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string TduRaceContentId = "race.wt2026.tour_down_under";
    private const string PrototypeRaceTemplateId = "race-scenario.peloton.prototype-v0";
    private const string PogacarOriginId = "rider.wt2026.uae.leader";
    private const string PhilipsenOriginId = "rider.wt2026.alpecin.card";
    private const long GateSeed = 91234;
    private static readonly string ProbeLogPath = "/opt/cursor/artifacts/wt-2026-feel-probe.log";

    [Fact]
    public void OfficialTduAndTerrainFeelProbe()
    {
        StringBuilder log = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;

        CourseProfile flatStage = world.CourseProfiles
            .Single(profile => string.Equals(
                profile.OriginDefinitionId,
                "course.wt2026.tdf.2026.s1",
                StringComparison.Ordinal));
        CourseProfile mountainStage = world.CourseProfiles
            .Where(profile => profile.ClassifiedStageType is ClassifiedStageType.Mountain or ClassifiedStageType.MountainSummit)
            .OrderByDescending(profile => profile.ElevationGainM)
            .First();
        RiderCareer philipsen = FindByOrigin(world, PhilipsenOriginId);
        RiderCareer pogacar = FindByOrigin(world, PogacarOriginId);

        RaceResult flatRace = SimulateControlledCourse(world, flatStage);
        RaceResult mountainRace = SimulateControlledCourse(world, mountainStage);
        int philFlat = PlaceOf(flatRace, philipsen.Id);
        int pogaFlat = PlaceOf(flatRace, pogacar.Id);
        int philMountain = PlaceOf(mountainRace, philipsen.Id);
        int pogaMountain = PlaceOf(mountainRace, pogacar.Id);

        for (int day = 0; day < 19; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(TduRaceContentId)).Succeeded);

        RaceResultProjection tduResult = Assert.IsType<RaceResultProjection>(application.RaceResult);
        Assert.Equal(140, tduResult.FinishOrder.Count);
        Assert.Contains(tduResult.FinishOrder, place => place.RiderId == pogacar.Id);
        log.AppendLine(CultureInfo.InvariantCulture, $"TDU starters={tduResult.FinishOrder.Count} pogacar_place={PlaceOf(tduResult, pogacar.Id)}");

        log.AppendLine(CultureInfo.InvariantCulture,
            $"flat_stage={flatStage.OriginDefinitionId} gain_m={flatStage.ElevationGainM:F0} starters={flatRace.FinishOrder.Count}");
        log.AppendLine(CultureInfo.InvariantCulture,
            $"flat_philipsen={philFlat} flat_pogacar={pogaFlat} sprinter_ahead={(philFlat > 0 && philFlat < pogaFlat)}");
        log.AppendLine(CultureInfo.InvariantCulture,
            $"mountain_stage={mountainStage.OriginDefinitionId} gain_m={mountainStage.ElevationGainM:F0}");
        log.AppendLine(CultureInfo.InvariantCulture,
            $"mountain_pogacar={pogaMountain} mountain_philipsen={philMountain} climber_ahead={(pogaMountain > 0 && pogaMountain < philMountain)}");

        WriteProbeLog(log.ToString());

        Assert.Equal(ClassifiedStageType.Flat, flatStage.ClassifiedStageType);
        Assert.True(flatRace.FinishOrder.Count >= 140);
        Assert.True(philFlat > 0 && pogaFlat > 0 && philFlat < pogaFlat, $"flat_philipsen={philFlat} flat_pogacar={pogaFlat}");
        Assert.True(pogaMountain > 0 && philMountain > 0);
        Assert.True(pogaMountain < philMountain);
    }

    private static RaceResult SimulateControlledCourse(WorldState world, CourseProfile courseProfile)
    {
        WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot).Resolve(WtScenarioId);
        RaceScenarioTemplate template = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
            .ResolveTemplate(PrototypeRaceTemplateId);
        RaceScenario scenario = WorldRaceScenarioAssembler.Assemble(
            world,
            recipe,
            template,
            courseProfile.RaceContentId,
            courseProfile: courseProfile,
            masterSeed: GateSeed);
        return new PrototypeRaceEngine().RunBatch(scenario, GateSeed);
    }

    private static int PlaceOf(RaceResultProjection projection, WorldEntityId riderId)
    {
        for (int index = 0; index < projection.FinishOrder.Count; index++)
        {
            if (projection.FinishOrder[index].RiderId == riderId)
            {
                return index + 1;
            }
        }

        return -1;
    }

    private static int PlaceOf(RaceResult result, WorldEntityId riderId)
    {
        for (int index = 0; index < result.FinishOrder.Count; index++)
        {
            if (result.FinishOrder[index] == riderId)
            {
                return index + 1;
            }
        }

        return -1;
    }

    private static RiderCareer FindByOrigin(WorldState world, string originId) =>
        world.RiderCareers.Single(career => string.Equals(career.OriginDefinitionId, originId, StringComparison.Ordinal));

    private static void WriteProbeLog(string text)
    {
        string? directory = Path.GetDirectoryName(ProbeLogPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return;
        }

        File.WriteAllText(ProbeLogPath, text, Encoding.UTF8);
    }
}
