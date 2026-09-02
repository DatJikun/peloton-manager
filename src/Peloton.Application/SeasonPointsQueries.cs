using System;
using Peloton.Domain;

namespace Peloton.Application;

internal static class SeasonPointsQueries
{
    private static readonly int[] PlacePoints =
        [50, 30, 20, 18, 16, 14, 12, 10, 8, 7, 6, 5, 4, 3, 2, 1];

    public static int ComputePoints(RiderCareer career, int seasonStartDay, int seasonEndDayExclusive)
    {
        ArgumentNullException.ThrowIfNull(career);
        int points = 0;
        foreach (RiderCareerResult result in career.Results)
        {
            if (result.DayNumber < seasonStartDay || result.DayNumber >= seasonEndDayExclusive)
            {
                continue;
            }

            if (result.DidNotFinish || result.Place < 1 || result.Place > PlacePoints.Length)
            {
                continue;
            }

            points += PlacePoints[result.Place - 1];
        }

        return points;
    }
}
