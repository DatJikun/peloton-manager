using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Application;

public enum KomPointsSource
{
    StagePlacesFallback,
}

public sealed record ClassificationStanding(
    int Place,
    WorldEntityId? RiderId,
    string Label,
    WorldEntityId? OrganizationId,
    string OrganizationName,
    double Value);

public sealed record ClassificationProjection(
    string RaceContentId,
    bool IsStageRace,
    KomPointsSource KomPointsSource,
    ClassificationStanding? GcLeader,
    ClassificationStanding? PointsLeader,
    ClassificationStanding? KomLeader,
    ClassificationStanding? YouthLeader,
    ClassificationStanding? TeamLeader,
    IReadOnlyList<ClassificationStanding> GcTop10,
    IReadOnlyList<ClassificationStanding> PointsTop10,
    IReadOnlyList<ClassificationStanding> KomTop10,
    IReadOnlyList<ClassificationStanding> YouthTop10,
    IReadOnlyList<ClassificationStanding> TeamTop10);

public static class ClassificationQueries
{
    private static readonly int[] FlatPointsScale =
        { 50, 30, 20, 18, 16, 14, 12, 10, 8, 7, 6, 5, 4, 3, 2, 1 };
    private static readonly int[] MountainPointsScale =
        { 20, 17, 15, 13, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
    private static readonly int[] KomPlaceScale = { 10, 8, 6, 4, 2 };

    public static ClassificationProjection Build(
        WorldState world,
        string raceContentId,
        int seasonYear = 2026,
        IReadOnlyList<RiderStageTime>? stageTimes = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrWhiteSpace(raceContentId);

        bool isStageRace = world.CalendarEntries.Count(entry =>
            entry.Kind == CalendarEntryKind.Race &&
            string.Equals(entry.RaceContentId, raceContentId, StringComparison.Ordinal)) > 1;
        if (!isStageRace)
        {
            return EmptyProjection(raceContentId, isStageRace: false);
        }

        RiderStageTime[] times = (stageTimes ?? world.RiderStageTimes)
            .Where(time => string.Equals(time.RaceContentId, raceContentId, StringComparison.Ordinal))
            .ToArray();
        int[] stages = times.Select(time => time.StageIndex).Distinct().OrderBy(index => index).ToArray();
        if (stages.Length == 0)
        {
            return EmptyProjection(raceContentId, isStageRace: true);
        }

        Dictionary<WorldEntityId, RiderCareer> careers = world.RiderCareers.ToDictionary(career => career.Id);
        Dictionary<WorldEntityId, Person> persons = world.Persons.ToDictionary(person => person.Id);
        Dictionary<WorldEntityId, Organization> organizations = world.Organizations.ToDictionary(
            organization => organization.Id);
        Dictionary<int, ClassifiedStageType> stageTypes = world.CourseProfiles
            .Where(profile => string.Equals(profile.RaceContentId, raceContentId, StringComparison.Ordinal))
            .GroupBy(profile => profile.StageIndex)
            .ToDictionary(group => group.Key, group => group.First().ClassifiedStageType);

        List<ClassificationStanding> gc = BuildGc(times, stages, careers, persons, organizations);
        List<ClassificationStanding> points = BuildPoints(times, stages, stageTypes, careers, persons, organizations);
        List<ClassificationStanding> kom = BuildKom(times, stages, stageTypes, careers, persons, organizations);
        List<ClassificationStanding> youth = gc
            .Where(standing => standing.RiderId is WorldEntityId riderId &&
                               careers.TryGetValue(riderId, out RiderCareer? career) &&
                               persons.TryGetValue(career.PersonId, out Person? person) &&
                               person.BirthYear is int birthYear &&
                               seasonYear - birthYear <= 24)
            .Select((standing, index) => standing with { Place = index + 1 })
            .ToList();
        List<ClassificationStanding> team = BuildTeam(times, stages, careers, organizations);

        return new ClassificationProjection(
            raceContentId,
            IsStageRace: true,
            KomPointsSource.StagePlacesFallback,
            gc.FirstOrDefault(),
            points.FirstOrDefault(),
            kom.FirstOrDefault(),
            youth.FirstOrDefault(),
            team.FirstOrDefault(),
            gc.Take(10).ToArray(),
            points.Take(10).ToArray(),
            kom.Take(10).ToArray(),
            youth.Take(10).ToArray(),
            team.Take(10).ToArray());
    }

    public static string FormatJerseyLine(ClassificationProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"gc={Label(projection.GcLeader)} points={Label(projection.PointsLeader)} kom={Label(projection.KomLeader)} youth={Label(projection.YouthLeader)} team={Label(projection.TeamLeader)}");
    }

    private static string Label(ClassificationStanding? standing) =>
        standing is null || string.IsNullOrWhiteSpace(standing.Label) ? "-" : standing.Label;

    private static ClassificationProjection EmptyProjection(string raceContentId, bool isStageRace) =>
        new(
            raceContentId,
            isStageRace,
            KomPointsSource.StagePlacesFallback,
            null,
            null,
            null,
            null,
            null,
            Array.Empty<ClassificationStanding>(),
            Array.Empty<ClassificationStanding>(),
            Array.Empty<ClassificationStanding>(),
            Array.Empty<ClassificationStanding>(),
            Array.Empty<ClassificationStanding>());

    private static List<ClassificationStanding> BuildGc(
        IReadOnlyList<RiderStageTime> times,
        IReadOnlyList<int> stages,
        Dictionary<WorldEntityId, RiderCareer> careers,
        Dictionary<WorldEntityId, Person> persons,
        Dictionary<WorldEntityId, Organization> organizations)
    {
        int lastStage = stages[^1];
        Dictionary<WorldEntityId, int> lastPlace = StagePlaces(times, lastStage);
        return times
            .GroupBy(time => time.RiderId)
            .Where(group => stages.All(stage => group.Any(time => time.StageIndex == stage)))
            .Select(group =>
            {
                double total = group.Sum(time => time.FinishTimeSeconds);
                int last = lastPlace.TryGetValue(group.Key, out int place) ? place : int.MaxValue;
                return (RiderId: group.Key, Total: total, Last: last);
            })
            .OrderBy(item => item.Total)
            .ThenBy(item => item.Last)
            .ThenBy(item => item.RiderId.Value)
            .Select((item, index) => ToStanding(index + 1, item.RiderId, item.Total, careers, persons, organizations))
            .ToList();
    }

    private static List<ClassificationStanding> BuildPoints(
        IReadOnlyList<RiderStageTime> times,
        IReadOnlyList<int> stages,
        Dictionary<int, ClassifiedStageType> stageTypes,
        Dictionary<WorldEntityId, RiderCareer> careers,
        Dictionary<WorldEntityId, Person> persons,
        Dictionary<WorldEntityId, Organization> organizations)
    {
        Dictionary<WorldEntityId, int> points = new();
        foreach (int stage in stages)
        {
            int[] scale = PointsScale(stageTypes.TryGetValue(stage, out ClassifiedStageType type) ? type : ClassifiedStageType.Flat);
            Dictionary<WorldEntityId, int> places = StagePlaces(times, stage);
            foreach ((WorldEntityId riderId, int place) in places)
            {
                if (place <= scale.Length)
                {
                    points[riderId] = points.GetValueOrDefault(riderId) + scale[place - 1];
                }
            }
        }

        return RankByPoints(points, careers, persons, organizations);
    }

    private static List<ClassificationStanding> BuildKom(
        IReadOnlyList<RiderStageTime> times,
        IReadOnlyList<int> stages,
        Dictionary<int, ClassifiedStageType> stageTypes,
        Dictionary<WorldEntityId, RiderCareer> careers,
        Dictionary<WorldEntityId, Person> persons,
        Dictionary<WorldEntityId, Organization> organizations)
    {
        Dictionary<WorldEntityId, int> points = new();
        foreach (int stage in stages)
        {
            if (!stageTypes.TryGetValue(stage, out ClassifiedStageType type) ||
                type is not (ClassifiedStageType.Mountain or ClassifiedStageType.MountainSummit))
            {
                continue;
            }

            Dictionary<WorldEntityId, int> places = StagePlaces(times, stage);
            foreach ((WorldEntityId riderId, int place) in places)
            {
                if (place <= KomPlaceScale.Length)
                {
                    points[riderId] = points.GetValueOrDefault(riderId) + KomPlaceScale[place - 1];
                }
            }
        }

        return RankByPoints(points, careers, persons, organizations);
    }

    private static List<ClassificationStanding> BuildTeam(
        IReadOnlyList<RiderStageTime> times,
        IReadOnlyList<int> stages,
        Dictionary<WorldEntityId, RiderCareer> careers,
        Dictionary<WorldEntityId, Organization> organizations)
    {
        Dictionary<WorldEntityId, double> totals = new();
        foreach (int stage in stages)
        {
            Dictionary<WorldEntityId, List<double>> byOrg = new();
            foreach (RiderStageTime time in times.Where(item => item.StageIndex == stage))
            {
                if (!careers.TryGetValue(time.RiderId, out RiderCareer? career) ||
                    career.OrganizationId is not WorldEntityId organizationId)
                {
                    continue;
                }

                if (!byOrg.TryGetValue(organizationId, out List<double>? list))
                {
                    list = new List<double>();
                    byOrg[organizationId] = list;
                }

                list.Add(time.FinishTimeSeconds);
            }

            foreach ((WorldEntityId organizationId, List<double> list) in byOrg)
            {
                if (list.Count < 3)
                {
                    continue;
                }

                list.Sort();
                totals[organizationId] = totals.GetValueOrDefault(organizationId) + list.Take(3).Sum();
            }
        }

        return totals
            .OrderBy(pair => pair.Value)
            .ThenBy(pair => pair.Key.Value)
            .Select((pair, index) =>
            {
                string name = organizations.TryGetValue(pair.Key, out Organization? organization)
                    ? organization.Name
                    : pair.Key.Value.ToString(CultureInfo.InvariantCulture);
                return new ClassificationStanding(
                    index + 1,
                    RiderId: null,
                    name,
                    pair.Key,
                    name,
                    pair.Value);
            })
            .ToList();
    }

    private static List<ClassificationStanding> RankByPoints(
        Dictionary<WorldEntityId, int> points,
        Dictionary<WorldEntityId, RiderCareer> careers,
        Dictionary<WorldEntityId, Person> persons,
        Dictionary<WorldEntityId, Organization> organizations)
    {
        return points
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key.Value)
            .Select((pair, index) => ToStanding(index + 1, pair.Key, pair.Value, careers, persons, organizations))
            .ToList();
    }

    private static Dictionary<WorldEntityId, int> StagePlaces(IReadOnlyList<RiderStageTime> times, int stageIndex)
    {
        return times
            .Where(time => time.StageIndex == stageIndex)
            .OrderBy(time => time.FinishTimeSeconds)
            .ThenBy(time => time.RiderId.Value)
            .Select((time, index) => (time.RiderId, Place: index + 1))
            .ToDictionary(item => item.RiderId, item => item.Place);
    }

    private static int[] PointsScale(ClassifiedStageType type) =>
        type is ClassifiedStageType.Mountain
            or ClassifiedStageType.MountainSummit
            or ClassifiedStageType.IndividualTimeTrial
            or ClassifiedStageType.TeamTimeTrial
            ? MountainPointsScale
            : FlatPointsScale;

    private static ClassificationStanding ToStanding(
        int place,
        WorldEntityId riderId,
        double value,
        Dictionary<WorldEntityId, RiderCareer> careers,
        Dictionary<WorldEntityId, Person> persons,
        Dictionary<WorldEntityId, Organization> organizations)
    {
        string label = riderId.Value.ToString(CultureInfo.InvariantCulture);
        WorldEntityId? organizationId = null;
        string organizationName = string.Empty;
        if (careers.TryGetValue(riderId, out RiderCareer? career))
        {
            if (persons.TryGetValue(career.PersonId, out Person? person) &&
                !string.IsNullOrWhiteSpace(person.Name))
            {
                label = person.Name;
            }
            else if (!string.IsNullOrWhiteSpace(career.OriginDefinitionId))
            {
                label = career.OriginDefinitionId;
            }

            organizationId = career.OrganizationId;
            if (career.OrganizationId is WorldEntityId orgId &&
                organizations.TryGetValue(orgId, out Organization? organization))
            {
                organizationName = organization.Name;
            }
        }

        return new ClassificationStanding(place, riderId, label, organizationId, organizationName, value);
    }
}
