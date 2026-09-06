using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record ScoutMissionProjection(
    int MissionId,
    WorldEntityId TargetRiderCareerId,
    string TargetRiderName,
    string ScoutName,
    int DurationDays,
    int DaysRemaining,
    bool IsCompleted,
    string StatusLabel);

public sealed record ScoutingReportProjection(
    WorldEntityId RiderCareerId,
    string RiderName,
    string Country,
    int? Age,
    string Style,
    double Stars,
    string StarsDisplay,
    int Ovr,
    string ClubName,
    string LevelLabel);

public sealed record ScoutingOverviewProjection(
    IReadOnlyList<ScoutMissionProjection> ActiveMissions,
    IReadOnlyList<ScoutingReportProjection> DiscoveredRiders,
    int AvailableScoutsCount);

public static class ScoutingQueries
{
    public static ScoutingOverviewProjection BuildOverview(WorldState world, AccessContext access)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (access.CurrentOrganizationId is not WorldEntityId orgId)
        {
            return new ScoutingOverviewProjection(
                Array.Empty<ScoutMissionProjection>(),
                Array.Empty<ScoutingReportProjection>(),
                0);
        }

        Organization? org = world.Organizations.FirstOrDefault(o => o.Id == orgId);
        if (org is null)
        {
            return new ScoutingOverviewProjection(
                Array.Empty<ScoutMissionProjection>(),
                Array.Empty<ScoutingReportProjection>(),
                0);
        }

        Dictionary<WorldEntityId, Person> personsById = world.Persons.ToDictionary(p => p.Id);
        Dictionary<WorldEntityId, RiderCareer> careersById = world.RiderCareers.ToDictionary(c => c.Id);
        Dictionary<WorldEntityId, Organization> orgsById = world.Organizations.ToDictionary(o => o.Id);

        List<ScoutMissionProjection> missionProjections = new();
        foreach (ScoutingMission mission in org.ScoutingStore.Missions.OrderBy(m => m.DaysRemaining))
        {
            string riderName = "Nieznany";
            if (careersById.TryGetValue(mission.TargetRiderCareerId, out RiderCareer? targetCareer) &&
                personsById.TryGetValue(targetCareer.PersonId, out Person? targetPerson))
            {
                riderName = targetPerson.Name;
            }

            string status = mission.IsCompleted ? "Zakończona" : $"{mission.DaysRemaining} dni";
            missionProjections.Add(new ScoutMissionProjection(
                mission.Id,
                mission.TargetRiderCareerId,
                riderName,
                mission.ScoutName,
                mission.DurationDays,
                mission.DaysRemaining,
                mission.IsCompleted,
                status));
        }

        List<ScoutingReportProjection> reportProjections = new();
        foreach (RiderCareer career in world.RiderCareers.Where(c => !c.IsRetired && c.OrganizationId != orgId))
        {
            ScoutingKnowledgeLevel level = org.ScoutingStore.GetLevel(career.Id);
            if (level == ScoutingKnowledgeLevel.Unknown)
            {
                continue;
            }

            Person person = personsById[career.PersonId];
            Organization? riderOrg = career.OrganizationId is WorldEntityId riderOrgId
                ? orgsById.GetValueOrDefault(riderOrgId)
                : null;
            RiderRatingSet ratings = RiderRatingQueries.FromPhysiology(career, career.PotentialOvr);
            RiderStarsProfile profile = RiderStyleRatingQueries.ComputeProfile(ratings, career.OriginDefinitionId);
            int? age = RiderPresentationQueries.ComputeAge(world, person);

            string levelLabel = level == ScoutingKnowledgeLevel.Discovered ? "Pełny raport" : "Wstępna obserwacja";
            reportProjections.Add(new ScoutingReportProjection(
                career.Id,
                person.Name,
                person.Nationality ?? "—",
                age,
                profile.PrimaryStyleLabel,
                profile.PrimaryStars,
                profile.PrimaryStarsDisplay,
                ratings.Ovr,
                riderOrg?.Name ?? "Wolny agent",
                levelLabel));
        }

        return new ScoutingOverviewProjection(
            missionProjections,
            reportProjections.OrderByDescending(r => r.Ovr).ToArray(),
            AvailableScoutsCount: Math.Max(0, 3 - missionProjections.Count(m => !m.IsCompleted)));
    }
}
