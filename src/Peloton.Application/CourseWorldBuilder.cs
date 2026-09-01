using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Course;

namespace Peloton.Application;

public static class CourseWorldBuilder
{
    public static (IReadOnlyList<CourseProfile> Profiles, IReadOnlyList<CalendarEntry> CalendarEntries)
        BuildWorldTourCalendar(
            WorldRecipe recipe,
            long seed,
            Func<WorldEntityId> allocateId)
    {
        if (recipe.RaceIdentities.Count == 0)
        {
            return (Array.Empty<CourseProfile>(), Array.Empty<CalendarEntry>());
        }

        CalendarRaceDetail[] calendarDetails = recipe.CalendarRaces
            .Select(race => new CalendarRaceDetail(
                race.Id,
                race.Name,
                race.Country,
                race.Kind,
                race.DayNumber,
                race.EndDayNumber >= 0 ? race.EndDayNumber : race.DayNumber))
            .ToArray();

        IReadOnlyList<CourseCatalogGenerator.GeneratedStageCourse> generated =
            CourseCatalogGenerator.GenerateSeason(
                recipe.RaceIdentities,
                calendarDetails,
                2026,
                seed,
                allocateId);

        List<CourseProfile> profiles = generated.Select(item => item.Profile).ToList();
        List<CalendarEntry> entries = generated
            .Select(item => new CalendarEntry(
                allocateId(),
                item.DayNumber,
                CalendarEntryKind.Race,
                item.Profile.Name,
                RaceContentId: item.Profile.RaceContentId,
                StageIndex: item.Profile.StageIndex,
                CourseProfileId: item.Profile.CourseProfileId))
            .OrderBy(entry => entry.DayNumber)
            .ThenBy(entry => entry.StageIndex)
            .ToList();

        return (profiles, entries);
    }
}
