using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerClubCalendarLeadersTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string SkeletonScenarioId = "scenario.peloton.skeleton";
    private const string AlpecinOriginId = "organization.wt2026.alpecin";
    private const string UaeOriginId = "organization.wt2026.uae";
    private const string AustraliaOriginId = "organization.wt2026.australia";
    private const string IsraelProTeamOriginId = "organization.wt2026.israel";
    private const string RoubaixRaceContentId = "race.wt2026.roubaix";
    private const string LombardiaRaceContentId = "race.wt2026.lombardia";
    private const string VdpOriginId = "rider.wt2026.alpecin.leader";
    private const string PhilipsenOriginId = "rider.wt2026.alpecin.card";
    private const long GateSeed = 91234;
    private static readonly string[] WorldTourStartDivisions = ["WorldTour"];
    private static readonly string[] LowerPyramidStartDivisions = ["Continental", "ProTeam"];

    [Fact]
    public void DefaultCreateWorldStillEmploysAlpecin()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        Organization employer = application.World!.Organizations.Single(
            organization => organization.Id == application.GetAccessContext().CurrentOrganizationId);
        Assert.Equal(AlpecinOriginId, employer.OriginDefinitionId);
    }

    [Fact]
    public void CreateWorldWithUaeEmploysUaeAndKeepsAlpecinRiders()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed, UaeOriginId)).Succeeded);
        AccessContext access = application.GetAccessContext();
        Organization employer = application.World!.Organizations.Single(
            organization => organization.Id == access.CurrentOrganizationId);
        Assert.Equal(UaeOriginId, employer.OriginDefinitionId);

        Organization alpecin = application.World.Organizations.Single(
            organization => string.Equals(organization.OriginDefinitionId, AlpecinOriginId, StringComparison.Ordinal));
        Assert.All(
            application.World.GetRiderCareersForOrganization(alpecin.Id),
            career => Assert.Equal(alpecin.Id, career.OrganizationId));
    }

    [Fact]
    public void NonStartableDivisionEmployerIsRejected()
    {
        GameApplication application = TestApplication.Create();
        Assert.Equal(
            "EMPLOYER_NOT_PLAYABLE",
            application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed, IsraelProTeamOriginId)).ReasonCode);
        Assert.Equal(
            "EMPLOYER_NOT_PLAYABLE",
            application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed, AustraliaOriginId)).ReasonCode);
        Assert.Equal(
            "EMPLOYER_NOT_PLAYABLE",
            application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed, "organization.unknown")).ReasonCode);
        Assert.Null(application.World);
        Assert.Equal(GameState.MainMenu, application.State);
    }

    [Fact]
    public void ListNewGameClubsReturnsEighteenWorldTourTeamsWithoutAustralia()
    {
        GameApplication application = TestApplication.Create();
        IReadOnlyList<NewGameClubProjection> clubs = application.ListNewGameClubs(WtScenarioId);
        Assert.Equal(18, clubs.Count);
        Assert.All(clubs, club => Assert.Equal("WorldTour", club.Division));
        Assert.DoesNotContain(clubs, club => string.Equals(club.OriginId, AustraliaOriginId, StringComparison.Ordinal));
        Assert.Equal(clubs.OrderBy(club => club.Name, StringComparer.Ordinal).ToArray(), clubs);
    }

    [Fact]
    public void StartableDivisionsAreContentNotAWorldTourHardCode()
    {
        Assert.True(EmployerEligibility.IsStartable("Continental", Array.Empty<string>()));
        Assert.True(EmployerEligibility.IsStartable("WorldTour", WorldTourStartDivisions));
        Assert.True(EmployerEligibility.IsStartable("Continental", LowerPyramidStartDivisions));
        Assert.False(EmployerEligibility.IsStartable("WorldTour", LowerPyramidStartDivisions));

        WorldRecipe recipe = new JsonScenarioCatalog(TestApplication.ContentRoot).Resolve(WtScenarioId);
        Assert.Equal(3, recipe.LicenceCycleYears);
        Assert.Equal("WorldTour", Assert.Single(recipe.PlayerStartDivisions!));
    }

    [Fact]
    public void SkippedLombardiaExcludesPlayerRidersButWorldStillRaces()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        AccessContext access = application.GetAccessContext();
        WorldEntityId employerId = access.CurrentOrganizationId!.Value;
        WorldEntityId[] employerRiderIds = application.World!.GetRiderCareersForOrganization(employerId)
            .Select(career => career.Id)
            .ToArray();

        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(application.Execute(new SetSeasonRaceEntryCommand(LombardiaRaceContentId, false)).Succeeded);
        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);

        int lombardiaDay = application.World.CalendarEntries
            .Where(entry => string.Equals(entry.RaceContentId, LombardiaRaceContentId, StringComparison.Ordinal))
            .Min(entry => entry.DayNumber);
        JumpToDay(application.World, lombardiaDay);

        Assert.False(application.World.IsRaceDueForOrganization(employerId));
        Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        Assert.True(application.World.RaceCount >= 1);
        Assert.All(employerRiderIds, riderId =>
            Assert.DoesNotContain(riderId, application.World.LastRace!.FinishOrder));
    }

    [Fact]
    public void DesignatedRoubaixLeaderOverridesDefaultStrategy()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        RiderCareer vanDerPoel = FindRider(application, VdpOriginId);
        RiderCareer philipsen = FindRider(application, PhilipsenOriginId);

        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        SkipAllRacesExcept(application, RoubaixRaceContentId);
        Assert.True(application.Execute(new SetSeasonRaceLeaderCommand(RoubaixRaceContentId, vanDerPoel.Id)).Succeeded);
        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);

        AdvanceToRaceDay(application, RoubaixRaceContentId);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.SetDefaultStrategy(application).Succeeded);
        Assert.Equal(vanDerPoel.Id, application.RacePreparation!.LeaderId);

        GameApplication flatApplication = TestApplication.Create();
        Assert.True(flatApplication.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        string flatRaceContentId = flatApplication.World!.CourseProfiles
            .Where(profile => profile.ClassifiedStageType == ClassifiedStageType.Flat)
            .OrderBy(profile => flatApplication.World.CalendarEntries
                .Where(entry => string.Equals(entry.RaceContentId, profile.RaceContentId, StringComparison.Ordinal))
                .Select(entry => entry.DayNumber)
                .DefaultIfEmpty(int.MaxValue)
                .Min())
            .First()
            .RaceContentId;
        Assert.True(flatApplication.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        SkipAllRacesExcept(flatApplication, flatRaceContentId);
        Assert.True(flatApplication.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);
        AdvanceToRaceDay(flatApplication, flatRaceContentId);
        Assert.True(flatApplication.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.SetDefaultStrategy(flatApplication).Succeeded);
        Assert.Equal(FindRider(flatApplication, VdpOriginId).Id, flatApplication.RacePreparation!.LeaderId);
        Assert.NotEqual(philipsen.Id, flatApplication.RacePreparation.LeaderId);
    }

    [Fact]
    public void CancelPreSeasonPlanningRestoresPreviousEntriesAndLeaders()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        AccessContext access = application.GetAccessContext();
        WorldEntityId employerId = access.CurrentOrganizationId!.Value;
        RiderCareer vanDerPoel = FindRider(application, VdpOriginId);

        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(application.Execute(new SetSeasonRaceEntryCommand(RoubaixRaceContentId, false)).Succeeded);
        Assert.True(application.Execute(new SetSeasonRaceLeaderCommand(RoubaixRaceContentId, vanDerPoel.Id)).Succeeded);
        Assert.True(application.Execute(new CancelPreSeasonPlanningCommand()).Succeeded);

        OrganizationRaceEntry entry = application.World!.OrganizationRaceEntries.Single(
            raceEntry => raceEntry.OrganizationId == employerId &&
                         string.Equals(raceEntry.RaceContentId, RoubaixRaceContentId, StringComparison.Ordinal));
        Assert.True(entry.Entered);
        Assert.Null(entry.DesignatedLeaderId);
    }

    [Fact]
    public void SchemaVersionNineRoundTripsDesignatedLeaderId()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "career-leaders.peloton");
        GameApplication source = TestApplication.Create();
        Assert.True(source.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        AccessContext access = source.GetAccessContext();
        WorldEntityId employerId = access.CurrentOrganizationId!.Value;
        RiderCareer vanDerPoel = FindRider(source, VdpOriginId);

        Assert.True(source.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(source.Execute(new SetSeasonRaceLeaderCommand(RoubaixRaceContentId, vanDerPoel.Id)).Succeeded);
        Assert.True(source.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        WorldCheckpoint stored = new SqliteWorldSaveStore().Load(savePath);
        Assert.Equal("9", SqliteWorldSaveStore.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        OrganizationRaceEntry storedEntry = stored.World.OrganizationRaceEntries.Single(
            raceEntry => raceEntry.OrganizationId == employerId &&
                         string.Equals(raceEntry.RaceContentId, RoubaixRaceContentId, StringComparison.Ordinal));
        Assert.Equal(vanDerPoel.Id, storedEntry.DesignatedLeaderId);

        GameApplication loaded = TestApplication.Create();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(
            WorldChecksum.Compute(source.World!),
            WorldChecksum.Compute(loaded.World!));
    }

    [Fact]
    public void SkeletonTenSeasonRunnerStillSucceeds()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        SkeletonCareerRunner runner = new(application);
        SkeletonRunReport report = runner.Run(10, temp.Path);
        Assert.False(report.Crashed);
        Assert.Equal(10, report.RaceCount);
    }

    private static RiderCareer FindRider(GameApplication application, string originId) =>
        application.World!.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, originId, StringComparison.Ordinal));

    private static void SkipAllRaces(GameApplication application)
    {
        foreach (PreSeasonRaceEntryProjection race in application.PreSeasonPlanning!.Races)
        {
            Assert.True(application.Execute(new SetSeasonRaceEntryCommand(race.RaceContentId, false)).Succeeded);
        }
    }

    private static void SkipAllRacesExcept(GameApplication application, params string[] enteredRaceContentIds)
    {
        HashSet<string> entered = enteredRaceContentIds.ToHashSet(StringComparer.Ordinal);
        foreach (PreSeasonRaceEntryProjection race in application.PreSeasonPlanning!.Races)
        {
            Assert.True(application.Execute(
                new SetSeasonRaceEntryCommand(race.RaceContentId, entered.Contains(race.RaceContentId))).Succeeded);
        }
    }

    private static void JumpToDay(WorldState world, int dayNumber)
    {
        PropertyInfo property = typeof(WorldState).GetProperty(nameof(WorldState.CurrentDate))!;
        property.SetValue(world, new WorldDate(dayNumber));
    }

    private static void AdvanceToRaceDay(GameApplication application, string raceContentId)
    {
        int raceDay = application.World!.CalendarEntries
            .Where(entry => string.Equals(entry.RaceContentId, raceContentId, StringComparison.Ordinal))
            .Min(entry => entry.DayNumber);
        JumpToDay(application.World, raceDay);
        Assert.True(application.World.IsRaceDue);
    }
}
