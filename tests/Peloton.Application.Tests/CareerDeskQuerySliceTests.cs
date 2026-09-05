using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerDeskQuerySliceTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string SkeletonScenarioId = "scenario.peloton.skeleton";
    private const string AlpecinEmployerOriginId = "organization.wt2026.alpecin";
    private const string TduRaceContentId = "race.wt2026.tour_down_under";
    private const long GateSeed = 91234;

    [Fact]
    public void WorldTourClubRosterIncludesNationalityAndAge()
    {
        GameApplication application = CreateConfirmedWorldTourCareer(AlpecinEmployerOriginId);
        ClubRosterProjection roster = Assert.IsType<ClubRosterProjection>(application.ClubRoster);

        ClubRosterEntry mvdp = Assert.Single(
            roster.Riders,
            rider => string.Equals(rider.Name, "Mathieu van der Poel", StringComparison.Ordinal));
        Assert.Equal("NED", mvdp.Nationality);
        Assert.Equal(31, mvdp.Age);

        Assert.All(
            roster.Riders,
            rider =>
            {
                Assert.False(string.IsNullOrWhiteSpace(rider.Nationality));
                Assert.True(rider.Age > 0);
            });
    }

    [Fact]
    public void MarketRidersIncludeNationalityAndAgeFromPersonData()
    {
        GameApplication application = CreateConfirmedWorldTourCareer(AlpecinEmployerOriginId);
        MarketRiderProjection pogacar = Assert.Single(
            application.MarketRiders,
            rider => string.Equals(rider.Name, "Tadej Pogačar", StringComparison.Ordinal));
        Assert.Equal("SLO", pogacar.Nationality);
        Assert.Equal(28, pogacar.Age);
    }

    [Fact]
    public void WorldTourSeasonEventsExposeDeterministicCourseSparkline()
    {
        GameApplication application = CreateConfirmedWorldTourCareer("organization.wt2026.ineos");
        SeasonEventProjection tdu = application.SeasonEvents.Single(
            item => string.Equals(item.RaceContentId, TduRaceContentId, StringComparison.Ordinal));

        Assert.Equal(CourseSparklineQueries.PointCount, tdu.ElevationSparkline!.Count);
        Assert.All(tdu.ElevationSparkline, value =>
            Assert.InRange(value, CourseSparklineQueries.HeightFloor, CourseSparklineQueries.HeightCeil));
        Assert.True(tdu.LengthKm > 50);
        Assert.True(tdu.ElevationGainM >= 0);
        Assert.False(string.IsNullOrWhiteSpace(tdu.ClassifiedStageType));

        GameApplication other = CreateConfirmedWorldTourCareer("organization.wt2026.ineos");
        SeasonEventProjection otherTdu = other.SeasonEvents.Single(
            item => string.Equals(item.RaceContentId, TduRaceContentId, StringComparison.Ordinal));
        Assert.Equal(tdu.ElevationSparkline, otherTdu.ElevationSparkline);
    }

    [Fact]
    public void SkeletonSeasonEventsHaveEmptySparklineAndQueriesDoNotMutateWorld()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(SkeletonScenarioId, GateSeed)).Succeeded);

        Assert.All(
            application.SeasonEvents,
            item => Assert.Empty(item.ElevationSparkline ?? Array.Empty<int>()));

        string checksumBefore = WorldChecksum.Compute(application.World!);
        _ = application.SeasonEvents.ToArray();
        _ = application.SeasonEvents.ToArray();
        Assert.Equal(checksumBefore, WorldChecksum.Compute(application.World!));
    }

    [Fact]
    public void SimulatedWorldTourRaceResultsIncludeFinishTimesAndGaps()
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

        WorldState world = application.World!;
        Organization alpecin = world.Organizations.Single(
            organization => string.Equals(organization.OriginDefinitionId, AlpecinEmployerOriginId, StringComparison.Ordinal));
        CalendarEntry completedRace = world.CalendarEntries.Single(entry =>
            entry.DayNumber == world.LastCompletedRaceDay &&
            entry.Kind == CalendarEntryKind.Race);

        RaceResultProjection full = Assert.IsType<RaceResultProjection>(application.RaceResult);
        Assert.All(full.FinishOrder, placement => Assert.True(placement.FinishTimeSeconds > 0));
        Assert.Equal(0, full.FinishOrder[0].GapSeconds);
        Assert.True(full.FinishOrder[20].GapSeconds > 0);
        Assert.True(full.FinishOrder[^1].GapSeconds > 0);

        for (int index = 0; index < full.FinishOrder.Count; index++)
        {
            Assert.Equal(index + 1, full.FinishOrder[index].Place);
        }

        foreach (RaceResultPlacement placement in full.FinishOrder)
        {
            RiderStageTime stageTime = Assert.Single(
                world.RiderStageTimes,
                item =>
                    string.Equals(item.RaceContentId, completedRace.RaceContentId, StringComparison.Ordinal) &&
                    item.StageIndex == completedRace.StageIndex &&
                    item.RiderId == placement.RiderId);
            Assert.Equal(stageTime.FinishTimeSeconds, placement.FinishTimeSeconds);
        }

        IReadOnlyList<RaceResultPlacement> alpecinFiltered =
            Assert.IsType<RaceResultPlacement[]>(application.RaceResultForOrganization(alpecin.Id));
        foreach (RaceResultPlacement filtered in alpecinFiltered)
        {
            RaceResultPlacement official = full.FinishOrder.Single(item => item.RiderId == filtered.RiderId);
            Assert.Equal(official.FinishTimeSeconds, filtered.FinishTimeSeconds);
            Assert.Equal(official.GapSeconds, filtered.GapSeconds);
        }

        Assert.Equal("—", RaceOutcomeQueries.FormatGap(0));
        Assert.Equal("—", RaceOutcomeQueries.FormatGap(-1));
        Assert.Equal(string.Empty, RaceOutcomeQueries.FormatGap(null));
        Assert.Equal("+1:05", RaceOutcomeQueries.FormatGap(65));
        Assert.Equal("+1:01:01", RaceOutcomeQueries.FormatGap(3661));
        Assert.Equal("2:05", RaceOutcomeQueries.FormatClock(125));
        Assert.Equal("1:01:01", RaceOutcomeQueries.FormatClock(3661));
    }

    private static GameApplication CreateConfirmedWorldTourCareer(string employerOriginId)
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(
            new CreateWorldCommand(WtScenarioId, GateSeed, employerOriginId)).Succeeded);
        Assert.True(application.Execute(new BeginPreSeasonPlanningCommand()).Succeeded);
        Assert.True(application.Execute(new ConfirmPreSeasonPlanCommand()).Succeeded);
        return application;
    }
}
