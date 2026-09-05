using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerUiQueryTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string SkeletonScenarioId = "scenario.peloton.skeleton";
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";
    private const string TduRaceContentId = "race.wt2026.tour_down_under";
    private const string VdpOriginId = "rider.wt2026.alpecin.leader";
    private const string PhilipsenOriginId = "rider.wt2026.alpecin.card";
    private const string PogacarOriginId = "rider.wt2026.uae.leader";
    private const long GateSeed = 91234;

    [Fact]
    public void WorldTourClubRosterExposesRiderIdentityPresentation()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);

        ClubRosterProjection roster = Assert.IsType<ClubRosterProjection>(application.ClubRoster);
        ClubRosterEntry vdp = roster.Riders.Single(
            rider => string.Equals(rider.OriginDefinitionId, VdpOriginId, StringComparison.Ordinal));
        ClubRosterEntry philipsen = roster.Riders.Single(
            rider => string.Equals(rider.OriginDefinitionId, PhilipsenOriginId, StringComparison.Ordinal));

        Assert.Equal("NED", vdp.Nationality);
        Assert.Equal(31, vdp.Age);
        Assert.Equal("KLASYKI", vdp.RoleLabel);
        Assert.Equal(100, vdp.FormPercent);
        Assert.Contains("NED", vdp.IdentityLine, StringComparison.Ordinal);
        Assert.Contains("31 LAT", vdp.IdentityLine, StringComparison.Ordinal);
        Assert.Contains("KLASYKI", vdp.IdentityLine, StringComparison.Ordinal);

        Assert.Equal("BEL", philipsen.Nationality);
        Assert.Equal("SPRINTER", philipsen.RoleLabel);
    }

    [Fact]
    public void MarketRidersExposeIdentityForOtherClubs()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);

        IReadOnlyList<MarketRiderProjection> market = application.MarketRiders;
        Assert.NotEmpty(market);

        RiderCareer pogacarCareer = FindRider(application.World!, PogacarOriginId);
        MarketRiderProjection pogacar = market.Single(rider => rider.RiderCareerId == pogacarCareer.Id);
        Assert.Equal("SLO", pogacar.Nationality);
        Assert.NotNull(pogacar.Age);
        Assert.Equal("GÓRY", pogacar.RoleLabel);
    }

    [Fact]
    public void PreSeasonCalendarAndSeasonEventsExposeRouteSparklines()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);

        SeasonEventProjection tdu = application.SeasonEvents.Single(
            item => item.Name.Contains("Tour Down Under", StringComparison.Ordinal));
        Assert.NotNull(tdu.ElevationSparkline);
        Assert.Equal(CourseSparklineQueries.PointCount, tdu.ElevationSparkline!.Count);
        Assert.All(
            tdu.ElevationSparkline,
            height => Assert.InRange(height, CourseSparklineQueries.HeightFloor, CourseSparklineQueries.HeightCeil));
        Assert.True(tdu.LengthKm > 50);
        Assert.NotNull(tdu.Route);
        Assert.InRange(tdu.Route!.DistanceKm, 100, 180);
        Assert.Equal(24, tdu.Route.Heights.Count);
        Assert.All(tdu.Route.Heights, height => Assert.InRange(height, 0, 20));
        Assert.False(tdu.Route.Heights.Distinct().Count() == 1);

        IReadOnlyList<CalendarEntryProjection> stages = application.StagesForEvent(TduRaceContentId);
        Assert.NotEmpty(stages);
        Assert.All(stages, stage => Assert.NotNull(stage.Route));

        CalendarEntryProjection[] calendarStages = application.Calendar
            .Where(entry => string.Equals(entry.RaceContentId, TduRaceContentId, StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(stages.Count, calendarStages.Length);
        Assert.All(calendarStages, stage => Assert.NotNull(stage.Route));
    }

    [Fact]
    public void WorldTourTourDownUnderResultsExposeFinishTimesAndGaps()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        for (int day = 0; day < 19; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(TduRaceContentId)).Succeeded);

        RaceResultProjection result = Assert.IsType<RaceResultProjection>(application.RaceResult);
        Assert.Equal(140, result.FinishOrder.Count);

        RaceResultPlacement winner = result.FinishOrder[0];
        Assert.NotNull(winner.FinishTimeSeconds);
        Assert.True(winner.FinishTimeSeconds > 0);
        Assert.Equal(0, winner.GapSeconds);
        Assert.Equal("—", winner.GapLabel);

        RaceResultPlacement twentieth = result.FinishOrder[19];
        Assert.NotNull(twentieth.FinishTimeSeconds);
        Assert.True(twentieth.FinishTimeSeconds > winner.FinishTimeSeconds);
        Assert.NotNull(twentieth.GapSeconds);
        Assert.True(twentieth.GapSeconds > 0);

        double? previousTime = null;
        foreach (RaceResultPlacement placement in result.FinishOrder)
        {
            if (placement.FinishTimeSeconds is not double finishTime)
            {
                continue;
            }

            if (previousTime is double prior)
            {
                Assert.True(finishTime >= prior);
            }

            previousTime = finishTime;
        }
    }

    [Fact]
    public void SkeletonSimulateResultsExposeTimesInFormatTableWhenRecorded()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(PrototypeRaceScenarioId)).Succeeded);

        Assert.NotEmpty(application.World!.RiderStageTimes);
        RaceResultProjection result = Assert.IsType<RaceResultProjection>(application.RaceResult);
        Assert.Contains(result.FinishOrder, placement => placement.TimeLabel is not null);
        string table = RaceOutcomeQueries.FormatTable(result, organizationId: null);
        Assert.Contains(":", table, StringComparison.Ordinal);
        Assert.True(table.Contains('+', StringComparison.Ordinal) || table.Contains('—', StringComparison.Ordinal));
    }

    [Fact]
    public void PresentationQueriesDoNotMutateWorldChecksum()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);

        string checksumBefore = WorldChecksum.Compute(application.World!);
        _ = application.ClubRoster;
        _ = application.SeasonEvents;
        _ = application.UpcomingEvents;
        _ = application.MarketRiders;
        _ = application.Calendar;
        _ = application.StagesForEvent(TduRaceContentId);
        Assert.Equal(checksumBefore, WorldChecksum.Compute(application.World!));

        for (int day = 0; day < 19; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded);
        }

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(RacePreparationSupport.ConfirmWithDefaultStrategy(application).Succeeded);
        Assert.True(application.Execute(new SimulateRaceCommand(TduRaceContentId)).Succeeded);

        string racedChecksum = WorldChecksum.Compute(application.World!);
        _ = application.RaceResult;
        _ = application.RaceResultForOrganization(application.GetAccessContext().CurrentOrganizationId!.Value);
        Assert.Equal(racedChecksum, WorldChecksum.Compute(application.World!));
    }

    private static RiderCareer FindRider(WorldState world, string originDefinitionId) =>
        world.RiderCareers.Single(
            career => string.Equals(career.OriginDefinitionId, originDefinitionId, StringComparison.Ordinal));
}
