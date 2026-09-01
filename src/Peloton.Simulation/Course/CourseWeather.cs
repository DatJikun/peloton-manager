using System;

namespace Peloton.Simulation.Course;

public sealed record CourseWeather(
    double WindSpeedMps,
    double WindFromDegrees);

public static class CourseWeatherFactory
{
    public static CourseWeather FromSeed(long masterSeed, string raceContentId, int stageIndex)
    {
        ulong derived = StableSeedDerivation.Derive(
            masterSeed,
            $"course-weather:{raceContentId}:{stageIndex}");
        DeterministicRng rng = new(derived);
        double windSpeed = 2.0 + CourseRng.NextUnit(rng) * 8.0;
        double windFrom = CourseRng.NextUnit(rng) * 360.0;
        return new CourseWeather(windSpeed, windFrom);
    }
}
