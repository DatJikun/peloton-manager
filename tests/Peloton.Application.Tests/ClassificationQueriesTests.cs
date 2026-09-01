using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class ClassificationQueriesTests
{
    private const string RaceId = "race.test.stage-race";

    [Fact]
    public void TwoStageWorldComputesAllJerseysAndDropsDnfFromGc()
    {
        WorldState world = CreateTwoStageWorld();
        ClassificationProjection projection = ClassificationQueries.Build(world, RaceId, seasonYear: 2026);

        Assert.True(projection.IsStageRace);
        Assert.Equal(KomPointsSource.StagePlacesFallback, projection.KomPointsSource);
        Assert.Equal("GC Rider", projection.GcLeader!.Label);
        Assert.Equal("Youth Rider", projection.YouthLeader!.Label);
        Assert.Equal("Sprint Rider", projection.PointsLeader!.Label);
        Assert.Equal("GC Rider", projection.KomLeader!.Label);
        Assert.Equal("Alpha", projection.TeamLeader!.Label);
        Assert.DoesNotContain(projection.GcTop10, standing => standing.Label == "Dnf Rider");
        Assert.Contains("gc=GC Rider", ClassificationQueries.FormatJerseyLine(projection), StringComparison.Ordinal);
    }

    [Fact]
    public void OneDayRaceHasNoJerseyTable()
    {
        WorldState world = CreateTwoStageWorld();
        CalendarEntry only = world.CalendarEntries[0];
        WorldState oneDay = new(
            world.WorldId,
            world.MasterSeed,
            world.RngContractVersion,
            world.CurrentDate,
            world.ContentIdentity,
            world.RulesIdentity,
            world.RulesModules,
            world.EntityIdHighWaterMark,
            world.Persons,
            world.ManagerCareers,
            world.Employments,
            world.Organizations,
            world.DecisionAuthorities,
            calendarEntries: new[] { only },
            riderCareers: world.RiderCareers,
            organizationRaceEntries: world.OrganizationRaceEntries,
            riderContracts: world.RiderContracts,
            courseProfiles: world.CourseProfiles,
            riderStageTimes: world.RiderStageTimes,
            generatePeriodicRaces: false);

        ClassificationProjection projection = ClassificationQueries.Build(oneDay, RaceId);
        Assert.False(projection.IsStageRace);
        Assert.Null(projection.GcLeader);
    }

    private static WorldState CreateTwoStageWorld()
    {
        WorldEntityId orgA = new(10);
        WorldEntityId orgB = new(11);
        Organization alpha = new(orgA, "organization.test.alpha", "Alpha");
        Organization beta = new(orgB, "organization.test.beta", "Beta");
        (Person Person, RiderCareer Career) gc = Rider(21, 101, orgA, "GC Rider", 1995, "rider.test.gc");
        (Person Person, RiderCareer Career) youth = Rider(22, 102, orgA, "Youth Rider", 2002, "rider.test.youth");
        (Person Person, RiderCareer Career) sprint = Rider(23, 103, orgA, "Sprint Rider", 1994, "rider.test.sprint");
        (Person Person, RiderCareer Career) dnf = Rider(24, 104, orgB, "Dnf Rider", 1990, "rider.test.dnf");
        (Person Person, RiderCareer Career) helper = Rider(25, 105, orgB, "Helper Rider", 1993, "rider.test.helper");
        (Person Person, RiderCareer Career) helper2 = Rider(26, 106, orgB, "Helper Two", 1992, "rider.test.helper2");

        return new WorldState(
            "classification-test",
            masterSeed: 1,
            rngContractVersion: 1,
            new WorldDate(2),
            new ContentIdentity("pack", "0.1.0", 1, "scenario.test", "Dynamic", "Advanced", "Guessed", "hash"),
            rulesIdentity: "test-rules",
            rulesModules: Array.Empty<RulesModuleIdentity>(),
            entityIdHighWaterMark: 200,
            new[] { gc.Person, youth.Person, sprint.Person, dnf.Person, helper.Person, helper2.Person },
            Array.Empty<ManagerCareer>(),
            Array.Empty<Employment>(),
            new[] { alpha, beta },
            new[] { new DecisionAuthority(new WorldEntityId(50), DecisionAuthorityKind.HumanInput) },
            calendarEntries: new[]
            {
                new CalendarEntry(new WorldEntityId(60), 1, CalendarEntryKind.Race, "S1", RaceContentId: RaceId, StageIndex: 1),
                new CalendarEntry(new WorldEntityId(61), 2, CalendarEntryKind.Race, "S2", RaceContentId: RaceId, StageIndex: 2),
            },
            riderCareers: new[] { gc.Career, youth.Career, sprint.Career, dnf.Career, helper.Career, helper2.Career },
            courseProfiles: new[]
            {
                MiniCourse(new WorldEntityId(70), 1, ClassifiedStageType.Flat, gain: 40),
                MiniCourse(new WorldEntityId(71), 2, ClassifiedStageType.Mountain, gain: 2500),
            },
            riderStageTimes: new[]
            {
                new RiderStageTime(RaceId, 1, gc.Career.Id, 1010),
                new RiderStageTime(RaceId, 1, youth.Career.Id, 1020),
                new RiderStageTime(RaceId, 1, sprint.Career.Id, 1000),
                new RiderStageTime(RaceId, 1, dnf.Career.Id, 1030),
                new RiderStageTime(RaceId, 1, helper.Career.Id, 1040),
                new RiderStageTime(RaceId, 1, helper2.Career.Id, 1050),
                new RiderStageTime(RaceId, 2, gc.Career.Id, 2000),
                new RiderStageTime(RaceId, 2, youth.Career.Id, 2010),
                new RiderStageTime(RaceId, 2, sprint.Career.Id, 2200),
                new RiderStageTime(RaceId, 2, helper.Career.Id, 2300),
                new RiderStageTime(RaceId, 2, helper2.Career.Id, 2400),
            },
            generatePeriodicRaces: false);
    }

    private static (Person Person, RiderCareer Career) Rider(
        long personId,
        long careerId,
        WorldEntityId organizationId,
        string name,
        int birthYear,
        string originId)
    {
        Person person = new(new WorldEntityId(personId), name, originId, "NED", birthYear);
        RiderCareer career = new(
            new WorldEntityId(careerId),
            person.Id,
            organizationId,
            originId,
            criticalPowerW: 380,
            wPrimeCapacityJ: 25000,
            peakPowerW: 1100,
            wPrimeRecoveryJPerSecond: 40,
            lowIntensityDurability: 0.8,
            highIntensityDurability: 0.8,
            bodyMassKg: 70,
            systemMassKg: 8,
            cdAM2: 0.3,
            baseCrr: 0.004,
            positioning: 0.7,
            handling: 0.7,
            tacticalAwareness: 0.7);
        return (person, career);
    }

    private static CourseProfile MiniCourse(
        WorldEntityId id,
        int stageIndex,
        ClassifiedStageType type,
        double gain)
    {
        CourseSampleVertex[] samples =
        {
            new(0, 0, 6, 0, CourseSurface.Asphalt, 0, 0),
            new(1000, gain, 6, 0, CourseSurface.Asphalt, 0, 0),
        };
        return new CourseProfile(
            id,
            $"course.test.{stageIndex}",
            RaceId,
            2026,
            stageIndex,
            $"Stage {stageIndex}",
            CourseKind.Road,
            "FRA",
            1000,
            samples,
            1000,
            gain,
            0,
            0,
            0,
            0.08,
            0,
            type);
    }
}
