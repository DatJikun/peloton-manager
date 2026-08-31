using System;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerWorldTourPhase5Tests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string TduRaceContentId = "race.wt2026.tour_down_under";
    private const string PrototypeRaceTemplateId = "race-scenario.peloton.prototype-v0";
    private const string AlpecinOriginId = "organization.wt2026.alpecin";
    private const long GateSeed = 91234;

    private static readonly string[] EliteOrganizationOriginIds =
    {
        "organization.wt2026.uae",
        "organization.wt2026.visma",
        "organization.wt2026.ineos",
        "organization.wt2026.redbull",
        "organization.wt2026.lidl-trek",
    };

    [Fact]
    public void CreateWorldMaterializesWorldTourPack()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;

        Assert.Equal(18, world.Organizations.Count);
        Assert.Equal(72, world.RiderCareers.Count);
        Assert.Equal(72, world.RiderContracts.Count);
        Assert.Equal(36, world.CalendarEntries.Count(entry => entry.Kind == CalendarEntryKind.Race));
        Assert.False(world.GeneratePeriodicRaces);

        Organization picnic = world.Organizations.Single(
            organization => string.Equals(organization.OriginDefinitionId, "organization.wt2026.picnic", StringComparison.Ordinal));
        Assert.Equal(1, picnic.LicenceYearsRemaining);

        foreach (string originId in EliteOrganizationOriginIds)
        {
            Organization organization = world.Organizations.Single(
                item => string.Equals(item.OriginDefinitionId, originId, StringComparison.Ordinal));
            Assert.True(organization.EstimatedBudgetEur >= 28_000_000);
        }

        CalendarEntry tdu = world.CalendarEntries.Single(
            entry => string.Equals(entry.RaceContentId, TduRaceContentId, StringComparison.Ordinal));
        Assert.Equal(19, tdu.DayNumber);

        AccessContext access = application.GetAccessContext();
        Organization employer = world.Organizations.Single(
            organization => organization.Id == access.CurrentOrganizationId);
        Assert.Equal(AlpecinOriginId, employer.OriginDefinitionId);

        Assert.Contains(world.Persons, person => string.Equals(person.Name, "Tadej Pogačar", StringComparison.Ordinal));
        Assert.Contains(world.Persons, person => string.Equals(person.Name, "Mathieu van der Poel", StringComparison.Ordinal));
    }

    [Fact]
    public void SchemaVersionFiveRoundTripsWorldTourWorldChecksum()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "wt-2026.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        WorldCheckpoint stored = new SqliteWorldSaveStore().Load(savePath);
        Assert.Equal(72, stored.World.RiderCareers.Count);

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(
            WorldChecksum.Compute(source.World!),
            WorldChecksum.Compute(loaded.World!));
    }

    [Fact]
    public void TourDownUnderProducesTwelveRiderStartListAndCareerResults()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        for (int day = 0; day < 19; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        Assert.Equal(19, application.World!.CurrentDate.DayNumber);
        Assert.True(application.World.IsRaceDue);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(TduRaceContentId)).Succeeded);

        WorldEntityId[] starters = application.World.RiderCareers
            .Where(career => career.Results.Any(result =>
                string.Equals(result.RaceContentId, TduRaceContentId, StringComparison.Ordinal)))
            .Select(career => career.Id)
            .ToArray();
        Assert.Equal(12, starters.Length);
        Assert.Contains(application.World.LastRace!.WinnerId, starters);
        Assert.All(starters, id => Assert.NotNull(application.World.TryGetRiderCareer(id)));
    }

    [Fact]
    public void AssemblerCapsWorldTourStartListAndIncludesPlayerAlpecinRiders()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        WorldState world = application.World!;
        AccessContext access = application.GetAccessContext();
        WorldEntityId employerId = access.CurrentOrganizationId!.Value;

        WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot).Resolve(WtScenarioId);
        RaceScenarioTemplate template = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
            .ResolveTemplate(PrototypeRaceTemplateId);
        WorldEntityId[] squad = world.GetRiderCareersForOrganization(employerId)
            .OrderBy(career => career.Id.Value)
            .Select(career => career.Id)
            .Take(2)
            .ToArray();

        RaceScenario scenario = WorldRaceScenarioAssembler.Assemble(
            world,
            recipe,
            template,
            TduRaceContentId,
            new RacePreparationStrategy(squad[0], squad[1], RaceObjective.StageWin, RaceBriefingKind.Chase),
            employerId);

        Assert.Equal(12, scenario.Riders.Count);
        Assert.Contains(scenario.Riders, rider => rider.OrganizationId == employerId);
    }

    [Fact]
    public void SkeletonTenSeasonRunnerBehaviourIsUnchanged()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", GateSeed)).Succeeded);
        SkeletonCareerRunner runner = new(application);
        SkeletonRunReport report = runner.Run(10, temp.Path);

        Assert.False(report.Crashed);
        Assert.Equal(10, report.RaceCount);
        Assert.Equal(120, report.WorldDay);
        Assert.Equal(12, application.World!.RiderCareers.Count(item => item.OrganizationId is not null));
        Assert.True(application.World.GeneratePeriodicRaces);
    }

    [Fact]
    public void CatalogValidationFailsWhenWorldTourRiderWageMissing()
    {
        using TemporaryDirectory temp = new();
        string packRoot = Path.Combine(temp.Path, "peloton.wt-broken");
        Directory.CreateDirectory(packRoot);
        File.Copy(
            Path.Combine(TestApplication.ContentRoot, "peloton.wt-2026", "organizations.json"),
            Path.Combine(packRoot, "organizations.json"));
        File.Copy(
            Path.Combine(TestApplication.ContentRoot, "peloton.wt-2026", "calendar.json"),
            Path.Combine(packRoot, "calendar.json"));
        File.Copy(
            Path.Combine(TestApplication.ContentRoot, "peloton.wt-2026", "scenario.json"),
            Path.Combine(packRoot, "scenario.json"));
        string rosterPath = Path.Combine(packRoot, "roster.json");
        File.Copy(
            Path.Combine(TestApplication.ContentRoot, "peloton.wt-2026", "roster.json"),
            rosterPath);
        string rosterJson = File.ReadAllText(rosterPath);
        rosterJson = rosterJson.Replace("\"annualWage\": 1080000", "\"annualWage\": 0", StringComparison.Ordinal);
        File.WriteAllText(rosterPath, rosterJson);
        File.WriteAllText(
            Path.Combine(packRoot, "pack.json"),
            """
            {
              "packId": "peloton.wt-broken",
              "packVersion": "0.0.0",
              "contentSchemaVersion": 1,
              "resources": [
                {"kind": "scenarios", "path": "scenario.json"},
                {"kind": "roster", "path": "roster.json"},
                {"kind": "organizations", "path": "organizations.json"},
                {"kind": "calendar", "path": "calendar.json"}
              ],
              "dependencies": []
            }
            """);

        JsonScenarioCatalog catalog = new(temp.Path);
        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => catalog.Resolve(WtScenarioId));
        Assert.Contains("annualWage", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
