using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerRosterTests
{
    private static readonly string[] PlayerSquadNames =
    {
        "Dawid Rutka",
        "Piotr Kowalczyk",
        "Marek Zieliński",
        "Tomasz Barski",
    };

    [Fact]
    public void SkeletonWorldHasTwelveNamedRidersThreeTeamsAndASeparateManager()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 91234)).Succeeded);

        WorldState world = application.World!;
        Assert.Equal(3, world.Organizations.Count);
        Assert.Equal(12, world.RosterRiders.Count);
        Assert.Equal(13, world.Persons.Count);
        Assert.Equal("Beskid–Vetter", world.Organizations[0].Name);
        Assert.Equal("Fala–Karpaty", world.Organizations[1].Name);
        Assert.Equal("Ost-Wind", world.Organizations[2].Name);
        Assert.Equal("Adam Wroński", world.Persons.Single(person => person.Id == world.ManagerCareers[0].PersonId).Name);
        Assert.DoesNotContain(world.RosterRiders, rider => rider.PersonId == world.ManagerCareers[0].PersonId);
        Assert.All(world.Organizations, organization =>
            Assert.Equal(4, world.RosterRiders.Count(rider => rider.OrganizationId == organization.Id)));
        Assert.Equal(CareerRaceBinder.PlayerOrganizationId(world), world.Organizations[0].Id);
        Assert.Equal(
            PlayerSquadNames,
            CareerRaceBinder.PlayerSquad(world)
                .Select(id => world.Persons.Single(person => person.Id == id).Name)
                .ToArray());

        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "roster.peloton");
        Assert.True(application.Execute(new SaveGameCommand(savePath)).Succeeded);
        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(12, loaded.World!.RosterRiders.Count);
        Assert.Equal("Adam Wroński", loaded.World.Persons.Single(person => person.Id == loaded.World.ManagerCareers[0].PersonId).Name);
        Assert.DoesNotContain(
            loaded.World.RosterRiders,
            rider => rider.PersonId == loaded.World.ManagerCareers[0].PersonId);
        Assert.Equal(WorldChecksum.Compute(world), WorldChecksum.Compute(loaded.World));
    }

    [Fact]
    public void CareerRaceUsesRosterPeopleWhilePrototypeCatalogKeepsSyntheticIds()
    {
        RaceScenario fixture = new JsonRacePrototypeCatalog(TestApplication.ContentRoot)
            .Resolve(RacePreparationDefaults.PrototypeScenarioId);
        Assert.Equal(1006, fixture.Riders.Single(rider => rider.ContentId == "rider.race-prototype.beta-leader").RiderId.Value);

        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 91234)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(RacePreparationDefaults.PrototypeScenarioId)).Succeeded);

        WorldState world = application.World!;
        WorldEntityId anconi = world.Persons.Single(person => person.Name == "Marco Anconi").Id;
        Assert.Equal(anconi, world.LastRace!.WinnerId);
        Assert.Equal("Marco Anconi", application.RaceResult!.WinnerLabel);
        Assert.Equal(12, world.LastRace.FinishOrder.Count);
        Assert.All(world.LastRace.FinishOrder, id => Assert.Contains(world.RosterRiders, rider => rider.PersonId == id));
        Assert.DoesNotContain(world.LastRace.FinishOrder, id => id.Value is >= 1001 and <= 1012);
    }
}
