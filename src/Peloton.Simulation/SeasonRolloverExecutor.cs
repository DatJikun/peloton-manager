using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Course;

namespace Peloton.Simulation;

public static class SeasonRolloverExecutor
{
    public static void RegisterApplicator()
    {
        WorldState.SetSeasonRolloverApplicator(Apply);
    }

    public static void Apply(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.RaceIdentities.Count == 0)
        {
            return;
        }

        int newSeasonYear = world.SeasonYear + 1;
        int dayOffset = world.CurrentDate.DayNumber;

        foreach (RiderCareer career in world.RiderCareers)
        {
            career.ApplyWinterFormReset();
        }

        CalendarRaceDetail[] shiftedCalendar = world.CalendarRaceDetails
            .Select(race => new CalendarRaceDetail(
                race.Id,
                race.Name,
                race.Country,
                race.Kind,
                race.StartDayNumber + dayOffset,
                race.EndDayNumber + dayOffset))
            .ToArray();

        IReadOnlyList<CourseCatalogGenerator.GeneratedStageCourse> generated =
            CourseCatalogGenerator.GenerateSeason(
                world.RaceIdentities,
                shiftedCalendar,
                newSeasonYear,
                world.MasterSeed,
                world.AllocateEntityId);

        List<CourseProfile> profiles = generated.Select(item => item.Profile).ToList();
        List<CalendarEntry> entries = generated
            .Select(item => new CalendarEntry(
                world.AllocateEntityId(),
                item.DayNumber,
                CalendarEntryKind.Race,
                item.Profile.Name,
                RaceContentId: item.Profile.RaceContentId,
                StageIndex: item.Profile.StageIndex,
                CourseProfileId: item.Profile.CourseProfileId))
            .ToList();

        world.AddCourseProfiles(profiles);
        world.AddCalendarEntries(entries);
        world.ResetOrganizationRaceEntriesForNewSeason();
        world.CompleteSeasonRollover(newSeasonYear, dayOffset);
    }
}
