using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;
using Xunit.Abstractions;

namespace Peloton.Application.Tests;

public sealed class WorldTourFeelProbeTests
{
    private const long GateSeed = 91234;
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string PrototypeRaceTemplateId = "race-scenario.peloton.prototype-v0";
    private const string TduRaceContentId = "race.wt2026.tour_down_under";
    private const string RoubaixRaceContentId = "race.wt2026.roubaix";
    private const string ArtifactLogPath = "/opt/cursor/artifacts/wt-2026-feel-probe.log";

    private static readonly string[] ControlledTeamOriginIds =
    {
        "organization.wt2026.alpecin",
        "organization.wt2026.uae",
        "organization.wt2026.visma",
    };

    private static readonly string[] CatalogProbeRaceIds =
    {
        TduRaceContentId,
        "race.wt2026.copenhagen_sprint",
        "race.wt2026.lombardia",
        RoubaixRaceContentId,
        "race.wt2026.tdf",
    };

    private static readonly string[] OfficialStartListOrganizationIds =
    {
        "organization.wt2026.alpecin",
        "organization.wt2026.astana",
        "organization.wt2026.bahrain",
    };

    private readonly ITestOutputHelper output;

    public WorldTourFeelProbeTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void OfficialWorldTourStartListIsAlpecinAstanaBahrainWithoutPogacar()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        RaceScenario scenario = AssembleOfficial(world, TduRaceContentId, courseProfile: null, masterSeed: null);
        string[] orgOrigins = scenario.Riders
            .Select(rider => world.Organizations.Single(organization => organization.Id == rider.OrganizationId).OriginDefinitionId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] riderOrigins = scenario.Riders
            .Select(rider => world.TryGetRiderCareer(rider.RiderId)!.OriginDefinitionId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(12, scenario.Riders.Count);
        Assert.Equal(OfficialStartListOrganizationIds, orgOrigins);
        Assert.Contains("rider.wt2026.alpecin.leader", riderOrigins);
        Assert.Contains("rider.wt2026.alpecin.card", riderOrigins);
        Assert.DoesNotContain("rider.wt2026.uae.leader", riderOrigins);
        Assert.DoesNotContain("rider.wt2026.visma.leader", riderOrigins);
    }

    [Fact]
    public void FeelProbeRunsOfficialTduAndControlledArchetypeCourses()
    {
        StringBuilder log = new();
        GameApplication catalogApp = TestApplication.Create();
        Assert.True(catalogApp.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState catalogWorld = catalogApp.World!;
        WriteCatalogSummary(log, catalogWorld);
        int flatCount = catalogWorld.CourseProfiles.Count(
            profile => profile.ClassifiedStageType == ClassifiedStageType.Flat);
        AppendLine(log, string.Create(CultureInfo.InvariantCulture, $"explicit Flat count={flatCount}"));

        try
        {
            RunOfficialTdu(log);

            CourseProfile leastHilly = PickLeastHilly(catalogWorld);
            CourseProfile summit = PickSummit(catalogWorld);
            CourseProfile cobbles = catalogWorld.CourseProfiles.Single(
                profile => string.Equals(profile.RaceContentId, RoubaixRaceContentId, StringComparison.Ordinal));

            GameApplication controlled = TestApplication.Create();
            Assert.True(controlled.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
            WorldState world = controlled.World!;
            WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot).Resolve(WtScenarioId);
            RaceScenarioTemplate template = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
                .ResolveTemplate(PrototypeRaceTemplateId);

            FeelRaceReport flatReport = RunControlled(
                log,
                world,
                recipe,
                template,
                leastHilly,
                raceOrdinal: 1,
                "LEAST-HILLY proxy (catalog has zero Flat stages)");
            FeelRaceReport summitReport = RunControlled(
                log,
                world,
                recipe,
                template,
                summit,
                raceOrdinal: 2,
                "SUMMIT / long climb");
            FeelRaceReport cobbleReport = RunControlled(
                log,
                world,
                recipe,
                template,
                cobbles,
                raceOrdinal: 3,
                "COBBLES / Roubaix");

            WriteFeelJudgement(log, flatReport, summitReport, cobbleReport);

            Assert.Equal(12, flatReport.Places.Count);
            Assert.Equal(12, summitReport.Places.Count);
            Assert.Equal(12, cobbleReport.Places.Count);
            Assert.Contains(flatReport.Places, row => row.OriginId == "rider.wt2026.uae.leader");
            Assert.Contains(flatReport.Places, row => row.OriginId == "rider.wt2026.alpecin.card");
            Assert.Contains(flatReport.Places, row => row.OriginId == "rider.wt2026.alpecin.leader");
            Assert.Equal(ClassifiedStageType.CobbleClassic, cobbles.ClassifiedStageType);
        }
        finally
        {
            FlushLog(log);
        }
    }

    private void RunOfficialTdu(StringBuilder log)
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        for (int day = 0; day < 19; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Stopwatch timer = Stopwatch.StartNew();
        Assert.True(application.Execute(new SimulateRaceCommand(TduRaceContentId)).Succeeded);
        timer.Stop();

        WorldState world = application.World!;
        CalendarEntry tdu = world.CalendarEntries
            .Where(entry => string.Equals(entry.RaceContentId, TduRaceContentId, StringComparison.Ordinal))
            .OrderBy(entry => entry.StageIndex)
            .First();
        CourseProfile profile = world.TryGetCourseProfile(tdu.CourseProfileId!.Value)!;
        AppendLine(log, string.Empty);
        AppendLine(log, "=== OFFICIAL TDU STAGE 1 (real 12-cap start list, day 19 form) ===");
        AppendLine(log, DescribeCourse(profile));
        AppendLine(log, string.Create(CultureInfo.InvariantCulture, $"elapsedMs={timer.ElapsedMilliseconds}"));
        List<FeelPlace> places = new();
        foreach (WorldEntityId riderId in world.LastRace!.FinishOrder)
        {
            RiderCareer career = world.TryGetRiderCareer(riderId)!;
            RiderStageTime? stageTime = world.RiderStageTimes.FirstOrDefault(
                time => time.RiderId == riderId &&
                        string.Equals(time.RaceContentId, TduRaceContentId, StringComparison.Ordinal) &&
                        time.StageIndex == tdu.StageIndex);
            places.Add(ToPlace(world, career, stageTime?.FinishTimeSeconds ?? 0, places.Count + 1));
        }

        WritePlaces(log, places);
    }

    private FeelRaceReport RunControlled(
        StringBuilder log,
        WorldState world,
        WorldRecipe recipe,
        RaceScenarioTemplate template,
        CourseProfile profile,
        int raceOrdinal,
        string label)
    {
        RestrictEntries(world, profile.RaceContentId, ControlledTeamOriginIds);
        RaceScenario scenario = WorldRaceScenarioAssembler.Assemble(
            world,
            recipe,
            template,
            profile.RaceContentId,
            playerStrategy: null,
            playerOrganizationId: world.Organizations.Single(
                organization => string.Equals(
                    organization.OriginDefinitionId,
                    "organization.wt2026.alpecin",
                    StringComparison.Ordinal)).Id,
            profile,
            world.MasterSeed);
        long seed = unchecked((long)StableSeedDerivation.Derive(
            world.MasterSeed,
            $"official-race-v1:{raceOrdinal}:{scenario.Id}:{scenario.TuningIdentity}"));
        Stopwatch timer = Stopwatch.StartNew();
        RaceResult result = new PrototypeRaceEngine().RunBatch(scenario, seed);
        timer.Stop();

        AppendLine(log, string.Empty);
        AppendLine(log, $"=== CONTROLLED {label} ===");
        AppendLine(log, DescribeCourse(profile));
        AppendLine(log, string.Create(
            CultureInfo.InvariantCulture,
            $"starters={scenario.Riders.Count} seed={seed} elapsedMs={timer.ElapsedMilliseconds} checksum={result.Checksum}"));
        WriteTacticalLeaders(log, world, scenario);

        Dictionary<WorldEntityId, double> times = result.RiderMetrics.ToDictionary(
            metric => metric.RiderId,
            metric => metric.FinishTimeSeconds);
        List<FeelPlace> places = new();
        int place = 1;
        foreach (WorldEntityId riderId in result.FinishOrder)
        {
            places.Add(ToPlace(world, world.TryGetRiderCareer(riderId)!, times[riderId], place));
            place++;
        }

        WritePlaces(log, places);
        return new FeelRaceReport(profile, places);
    }

    private static RaceScenario AssembleOfficial(
        WorldState world,
        string raceContentId,
        CourseProfile? courseProfile,
        long? masterSeed)
    {
        WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot).Resolve(WtScenarioId);
        RaceScenarioTemplate template = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
            .ResolveTemplate(PrototypeRaceTemplateId);
        WorldEntityId playerOrg = world.Organizations.Single(
            organization => string.Equals(
                organization.OriginDefinitionId,
                "organization.wt2026.alpecin",
                StringComparison.Ordinal)).Id;
        return WorldRaceScenarioAssembler.Assemble(
            world,
            recipe,
            template,
            raceContentId,
            playerStrategy: null,
            playerOrganizationId: playerOrg,
            courseProfile,
            masterSeed);
    }

    private static void RestrictEntries(WorldState world, string raceContentId, IReadOnlyList<string> keepOriginIds)
    {
        HashSet<string> keep = new(keepOriginIds, StringComparer.Ordinal);
        foreach (Organization organization in world.Organizations)
        {
            world.SetOrganizationRaceEntry(
                organization.Id,
                raceContentId,
                keep.Contains(organization.OriginDefinitionId));
        }
    }

    private static CourseProfile PickLeastHilly(WorldState world)
    {
        CourseProfile[] candidates = world.CourseProfiles
            .Where(profile =>
                profile.Kind == CourseKind.Road &&
                profile.ClassifiedStageType is ClassifiedStageType.Flat
                    or ClassifiedStageType.Hilly
                    or ClassifiedStageType.Mixed)
            .OrderBy(profile => profile.ElevationGainM / Math.Max(1.0, profile.LengthM))
            .ThenBy(profile => profile.LengthM)
            .ToArray();
        Assert.True(candidates.Length > 0, "2026 catalog has no road stage usable as a sprint proxy.");
        return candidates[0];
    }

    private static CourseProfile PickSummit(WorldState world)
    {
        CourseProfile[] summits = world.CourseProfiles
            .Where(profile => profile.ClassifiedStageType == ClassifiedStageType.MountainSummit)
            .OrderByDescending(profile => profile.ElevationGainM)
            .ToArray();
        if (summits.Length > 0)
        {
            return summits[0];
        }

        CourseProfile[] mountains = world.CourseProfiles
            .Where(profile => profile.ClassifiedStageType == ClassifiedStageType.Mountain)
            .OrderByDescending(profile => profile.ElevationGainM)
            .ToArray();
        Assert.True(mountains.Length > 0, "2026 catalog has no mountain stage.");
        return mountains[0];
    }

    private void WriteCatalogSummary(StringBuilder log, WorldState world)
    {
        AppendLine(log, "=== 2026 COURSE CATALOG (seed 91234) ===");
        IGrouping<ClassifiedStageType, CourseProfile>[] groups = world.CourseProfiles
            .GroupBy(profile => profile.ClassifiedStageType)
            .OrderBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (IGrouping<ClassifiedStageType, CourseProfile> group in groups)
        {
            AppendLine(log, string.Create(CultureInfo.InvariantCulture, $"  {group.Key}: {group.Count()}"));
        }

        AppendLine(log, "Named probes:");
        foreach (string raceId in CatalogProbeRaceIds)
        {
            CourseProfile[] stages = world.CourseProfiles
                .Where(profile => string.Equals(profile.RaceContentId, raceId, StringComparison.Ordinal))
                .OrderBy(profile => profile.StageIndex)
                .ToArray();
            foreach (CourseProfile stage in stages.Take(3))
            {
                AppendLine(log, "  " + DescribeCourse(stage));
            }

            if (stages.Length > 3)
            {
                AppendLine(log, string.Create(CultureInfo.InvariantCulture, $"  ... {stages.Length - 3} more {raceId} stages"));
            }
        }
    }

    private void WriteTacticalLeaders(StringBuilder log, WorldState world, RaceScenario scenario)
    {
        foreach (IGrouping<WorldEntityId, RaceRiderProfile> group in scenario.Riders
                     .GroupBy(rider => rider.OrganizationId)
                     .OrderBy(group => group.Key.Value))
        {
            RiderCareer[] orgRiders = world.GetRiderCareersForOrganization(group.Key)
                .Where(career => scenario.Riders.Any(rider => rider.RiderId == career.Id))
                .ToArray();
            string org = world.Organizations.Single(item => item.Id == group.Key).OriginDefinitionId;
            string leader = PersonName(world, orgRiders[0]);
            string support = orgRiders.Length > 1 ? PersonName(world, orgRiders[1]) : leader;
            AppendLine(log, $"  tactics {org}: leader={leader} support={support}");
        }
    }

    private void WritePlaces(StringBuilder log, IReadOnlyList<FeelPlace> places)
    {
        double winnerTime = places[0].FinishSeconds;
        foreach (FeelPlace row in places)
        {
            double gap = row.FinishSeconds - winnerTime;
            string line = string.Create(
                CultureInfo.InvariantCulture,
                $"{row.Place,2}. {row.Name,-24} {FormatTime(row.FinishSeconds),10}  +{gap,7:0}s  climb={row.Ratings.Climb,2} spr={row.Ratings.Sprint,2} cob={row.Ratings.Cobbles,2} ovr={row.Ratings.Ovr,2}  {row.OriginId}");
            AppendLine(log, line);
        }
    }

    private void WriteFeelJudgement(
        StringBuilder log,
        FeelRaceReport flat,
        FeelRaceReport summit,
        FeelRaceReport cobbles)
    {
        int philipsenFlat = PlaceOf(flat, "rider.wt2026.alpecin.card");
        int pogacarFlat = PlaceOf(flat, "rider.wt2026.uae.leader");
        int philipsenSummit = PlaceOf(summit, "rider.wt2026.alpecin.card");
        int pogacarSummit = PlaceOf(summit, "rider.wt2026.uae.leader");
        int mvdpCobbles = PlaceOf(cobbles, "rider.wt2026.alpecin.leader");
        int almeidaCobbles = PlaceOf(cobbles, "rider.wt2026.uae.support-1");
        AppendLine(log, string.Empty);
        AppendLine(log, "=== FEEL JUDGEMENT (controlled 12: Alpecin + UAE + Visma) ===");
        AppendLine(log, string.Create(
            CultureInfo.InvariantCulture,
            $"least-hilly: Philipsen P{philipsenFlat} vs Pogacar P{pogacarFlat}  (want sprinter ahead on a true flat; this catalog has none)"));
        AppendLine(log, string.Create(
            CultureInfo.InvariantCulture,
            $"summit: Pogacar P{pogacarSummit} vs Philipsen P{philipsenSummit}  (want climber ahead)"));
        AppendLine(log, string.Create(
            CultureInfo.InvariantCulture,
            $"cobbles: van der Poel P{mvdpCobbles} vs Almeida P{almeidaCobbles}  (want classics ahead of GC support)"));
        AppendLine(log, string.Create(
            CultureInfo.InvariantCulture,
            $"leastHillySprinterAhead={philipsenFlat < pogacarFlat} summitClimberAhead={pogacarSummit < philipsenSummit} cobblesClassicsAhead={mvdpCobbles < almeidaCobbles}"));
    }

    private static int PlaceOf(FeelRaceReport report, string originId) =>
        report.Places.Single(row => string.Equals(row.OriginId, originId, StringComparison.Ordinal)).Place;

    private static FeelPlace ToPlace(WorldState world, RiderCareer career, double finishSeconds, int place)
    {
        RiderRatingSet ratings = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr);
        return new FeelPlace(place, PersonName(world, career), career.OriginDefinitionId, finishSeconds, ratings);
    }

    private static string PersonName(WorldState world, RiderCareer career) =>
        world.Persons.Single(person => person.Id == career.PersonId).Name;

    private static string DescribeCourse(CourseProfile profile)
    {
        string km = (profile.LengthM / 1000.0).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
        string cobbleKm = (profile.CobbleM / 1000.0).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
        string gain = profile.ElevationGainM.ToString("0", CultureInfo.InvariantCulture);
        return $"{profile.RaceContentId} S{profile.StageIndex} {profile.ClassifiedStageType} {km} km +{gain} m cobble={cobbleKm} km {profile.Name}";
    }

    private static string FormatTime(double seconds)
    {
        TimeSpan span = TimeSpan.FromSeconds(seconds);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}");
    }

    private void AppendLine(StringBuilder log, string line)
    {
        log.AppendLine(line);
        output.WriteLine(line);
    }

    private void FlushLog(StringBuilder log)
    {
        string? directory = Path.GetDirectoryName(ArtifactLogPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        File.WriteAllText(ArtifactLogPath, log.ToString());
        output.WriteLine($"wrote={ArtifactLogPath}");
    }

    private sealed record FeelPlace(
        int Place,
        string Name,
        string OriginId,
        double FinishSeconds,
        RiderRatingSet Ratings);

    private sealed record FeelRaceReport(CourseProfile Course, IReadOnlyList<FeelPlace> Places);
}
