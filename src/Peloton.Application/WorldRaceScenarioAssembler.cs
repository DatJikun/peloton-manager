using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Course;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public static class WorldRaceScenarioAssembler
{
    private const int DefaultStartersPerOrganization = 4;

    public static RaceScenario Assemble(
        WorldState world,
        WorldRecipe recipe,
        RaceScenarioTemplate template,
        string raceContentId,
        RacePreparationStrategy? playerStrategy = null,
        WorldEntityId? playerOrganizationId = null,
        CourseProfile? courseProfile = null,
        long? masterSeed = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(raceContentId);

        RaceDefinition route = template.Route;
        int maximumDurationSeconds = template.MaximumDurationSeconds;
        if (courseProfile is not null && masterSeed is long seed)
        {
            CourseWeather weather = CourseWeatherFactory.FromSeed(
                seed,
                courseProfile.RaceContentId,
                courseProfile.StageIndex);
            route = CourseCompiler.ToRaceDefinition(courseProfile, weather, courseProfile.OriginDefinitionId);
            maximumDurationSeconds = CourseCompiler.MaximumDurationSeconds(courseProfile);
        }

        Dictionary<string, RiderCareer> careersByOrigin = world.RiderCareers
            .ToDictionary(career => career.OriginDefinitionId, StringComparer.Ordinal);
        bool useSkeletonPath = template.StartingOrderRiderIds.All(careersByOrigin.ContainsKey);
        if (useSkeletonPath)
        {
            return AssembleSkeletonPath(
                world,
                recipe,
                template,
                raceContentId,
                careersByOrigin,
                playerStrategy,
                playerOrganizationId,
                route,
                maximumDurationSeconds);
        }

        return AssembleWorldTourPath(
            world,
            recipe,
            template,
            raceContentId,
            careersByOrigin,
            playerStrategy,
            playerOrganizationId,
            route,
            maximumDurationSeconds,
            courseProfile is not null,
            courseProfile?.ClassifiedStageType);
    }

    private static RaceScenario AssembleSkeletonPath(
        WorldState world,
        WorldRecipe recipe,
        RaceScenarioTemplate template,
        string raceContentId,
        Dictionary<string, RiderCareer> careersByOrigin,
        RacePreparationStrategy? playerStrategy,
        WorldEntityId? playerOrganizationId,
        RaceDefinition route,
        int maximumDurationSeconds)
    {
        Dictionary<string, Organization> organizationsByOrigin = world.Organizations
            .ToDictionary(organization => organization.OriginDefinitionId, StringComparer.Ordinal);
        Dictionary<string, WorldEntityId> raceTeamToOrganization = recipe.TeamRaceMappings
            .ToDictionary(
                mapping => mapping.RaceTeamId,
                mapping => organizationsByOrigin[mapping.OrganizationId].Id,
                StringComparer.Ordinal);
        WorldEntityId humanAuthorityId = ResolveHumanAuthorityId(world);
        HashSet<WorldEntityId> enteredOrganizationIds = world.OrganizationRaceEntries
            .Where(entry =>
                string.Equals(entry.RaceContentId, raceContentId, StringComparison.Ordinal) && entry.Entered)
            .Select(entry => entry.OrganizationId)
            .ToHashSet();

        RaceRiderProfile[] riders = world.RiderCareers
            .Where(career =>
                career.OrganizationId is WorldEntityId organizationId &&
                enteredOrganizationIds.Contains(organizationId))
            .OrderBy(career => career.OriginDefinitionId, StringComparer.Ordinal)
            .Select(career => ToRaceProfile(career))
            .ToArray();

        IReadOnlyList<string> startingOrder = template.StartingOrderRiderIds;
        RaceStartingPosition[] startingPositions = startingOrder
            .Select((originId, index) => new RaceStartingPosition(
                careersByOrigin[originId].Id,
                (startingOrder.Count - 1 - index) * 0.7))
            .Where(position => riders.Any(rider => rider.RiderId == position.RiderId))
            .ToArray();

        RaceCommand[] commands = template.Commands
            .Where(command => enteredOrganizationIds.Contains(raceTeamToOrganization[command.TeamId]))
            .Select(command => new RaceCommand(
                command.SimulationSecond,
                raceTeamToOrganization[command.TeamId],
                careersByOrigin[command.RiderId].Id,
                command.Intent))
            .ToArray();

        RaceTacticalPlan[] tacticalPlans = template.TacticalPlans
            .Where(plan => enteredOrganizationIds.Contains(raceTeamToOrganization[plan.TeamId]))
            .Select(plan =>
            {
                RaceTeamTemplate team = template.Teams[plan.TeamId];
                WorldEntityId organizationId = raceTeamToOrganization[plan.TeamId];
                WorldEntityId supportRiderId = careersByOrigin[plan.SupportRiderId].Id;
                RaceObjective objective = team.Objective;
                RaceBriefing briefing = team.Briefing;
                if (playerOrganizationId is not null &&
                    playerStrategy is not null &&
                    organizationId == playerOrganizationId.Value)
                {
                    supportRiderId = playerStrategy.SupportId;
                    objective = playerStrategy.Objective;
                    briefing = new RaceBriefing(playerStrategy.BriefingKind, team.Briefing.ConsultManager);
                }

                return new RaceTacticalPlan(
                    plan.TriggerSecond,
                    supportRiderId,
                    new TeamRaceObservation(
                        organizationId,
                        humanAuthorityId,
                        plan.OfficialGapSeconds,
                        plan.VisibleSplit,
                        plan.LeaderPositionBand,
                        plan.ResourceEstimate,
                        plan.ThreatEstimate,
                        objective,
                        plan.Confidence),
                    briefing);
            })
            .ToArray();

        return new RaceScenario(
            template.Id,
            route,
            riders,
            startingPositions,
            commands,
            template.InitialSpeedMps,
            maximumDurationSeconds,
            tacticalPlans,
            template.TuningIdentity,
            classifiedStageType: null);
    }

    private static RaceScenario AssembleWorldTourPath(
        WorldState world,
        WorldRecipe recipe,
        RaceScenarioTemplate template,
        string raceContentId,
        Dictionary<string, RiderCareer> careersByOrigin,
        RacePreparationStrategy? playerStrategy,
        WorldEntityId? playerOrganizationId,
        RaceDefinition route,
        int maximumDurationSeconds,
        bool generatedCourse,
        ClassifiedStageType? classifiedStageType)
    {
        WorldEntityId humanAuthorityId = ResolveHumanAuthorityId(world);
        HashSet<WorldEntityId> enteredOrganizationIds = world.OrganizationRaceEntries
            .Where(entry =>
                string.Equals(entry.RaceContentId, raceContentId, StringComparison.Ordinal) && entry.Entered)
            .Select(entry => entry.OrganizationId)
            .ToHashSet();
        int startersPerTeam = ResolveStartersPerTeam(recipe, raceContentId);
        HashSet<string>? inviteOriginIds = ResolveInviteOriginIds(recipe, raceContentId);
        List<RiderCareer> selectedCareers = new();
        foreach (Organization organization in world.Organizations
                     .Where(organization => enteredOrganizationIds.Contains(organization.Id))
                     .Where(organization =>
                         inviteOriginIds is null ||
                         inviteOriginIds.Contains(organization.OriginDefinitionId))
                     .OrderBy(organization => organization.OriginDefinitionId, StringComparer.Ordinal))
        {
            selectedCareers.AddRange(
                world.GetRiderCareersForOrganization(organization.Id).Take(startersPerTeam));
        }

        RiderCareer[] starters = selectedCareers
            .OrderBy(career => career.OriginDefinitionId, StringComparer.Ordinal)
            .ToArray();
        RaceRiderProfile[] riders = starters.Select(career => ToRaceProfile(career)).ToArray();
        RaceStartingPosition[] startingPositions = starters
            .Select((career, index) => new RaceStartingPosition(
                career.Id,
                (starters.Length - 1 - index) * 0.7))
            .ToArray();

        HashSet<WorldEntityId> starterIds = starters.Select(career => career.Id).ToHashSet();
        Dictionary<string, WorldEntityId> raceTeamToOrganization = world.Organizations
            .Where(organization => enteredOrganizationIds.Contains(organization.Id))
            .ToDictionary(
                organization => organization.OriginDefinitionId,
                organization => organization.Id,
                StringComparer.Ordinal);
        RaceCommand[] commands = generatedCourse
            ? Array.Empty<RaceCommand>()
            : template.Commands
                .Where(command =>
                    careersByOrigin.TryGetValue(command.RiderId, out RiderCareer? career) &&
                    career.OrganizationId is WorldEntityId organizationId &&
                    enteredOrganizationIds.Contains(organizationId) &&
                    starterIds.Contains(career.Id) &&
                    raceTeamToOrganization.TryGetValue(command.TeamId, out WorldEntityId mappedOrg) &&
                    mappedOrg == organizationId)
                .Select(command => new RaceCommand(
                    command.SimulationSecond,
                    raceTeamToOrganization[command.TeamId],
                    careersByOrigin[command.RiderId].Id,
                    command.Intent))
                .ToArray();

        HashSet<WorldEntityId> organizationsWithStarters = starters
            .Where(career => career.OrganizationId is not null)
            .Select(career => career.OrganizationId!.Value)
            .ToHashSet();
        List<RaceTacticalPlan> tacticalPlans = new();
        foreach (WorldEntityId organizationId in organizationsWithStarters.OrderBy(id => id.Value))
        {
            RiderCareer[] orgRiders = world.GetRiderCareersForOrganization(organizationId)
                .Where(career => starterIds.Contains(career.Id))
                .ToArray();
            if (orgRiders.Length == 0)
            {
                continue;
            }

            WorldEntityId leaderId = orgRiders[0].Id;
            WorldEntityId supportId = orgRiders.Length > 1 ? orgRiders[1].Id : orgRiders[0].Id;
            RaceObjective objective = RaceObjective.StageWin;
            RaceBriefingKind briefingKind = RaceBriefingKind.Chase;
            if (playerOrganizationId is not null &&
                playerStrategy is not null &&
                organizationId == playerOrganizationId.Value)
            {
                leaderId = playerStrategy.LeaderId;
                supportId = playerStrategy.SupportId;
                objective = playerStrategy.Objective;
                briefingKind = playerStrategy.BriefingKind;
            }

            tacticalPlans.Add(new RaceTacticalPlan(
                0,
                supportId,
                new TeamRaceObservation(
                    organizationId,
                    humanAuthorityId,
                    0,
                    VisibleSplit: false,
                    RacePositionBand.Front,
                    RaceResourceEstimate.Strong,
                    RaceThreatEstimate.Low,
                    objective,
                    RaceInformationConfidence.Medium),
                new RaceBriefing(briefingKind, ConsultManager: false)));
        }

        return new RaceScenario(
            template.Id,
            route,
            riders,
            startingPositions,
            commands,
            template.InitialSpeedMps,
            maximumDurationSeconds,
            tacticalPlans.ToArray(),
            template.TuningIdentity,
            classifiedStageType);
    }

    private static WorldEntityId ResolveHumanAuthorityId(WorldState world)
    {
        DecisionAuthority? humanAuthority = world.DecisionAuthorities.FirstOrDefault(
            authority => authority.Kind == DecisionAuthorityKind.HumanInput);
        if (humanAuthority is null)
        {
            throw new InvalidOperationException("World has no human decision authority.");
        }

        return humanAuthority.Id;
    }

    private static int ResolveStartersPerTeam(WorldRecipe recipe, string raceContentId)
    {
        RaceIdentityConstraints? identity = recipe.RaceIdentities.FirstOrDefault(
            item => string.Equals(item.RaceContentId, raceContentId, StringComparison.Ordinal));
        if (identity is not null && identity.StartersPerTeam > 0)
        {
            return identity.StartersPerTeam;
        }

        return DefaultStartersPerOrganization;
    }

    private static HashSet<string>? ResolveInviteOriginIds(WorldRecipe recipe, string raceContentId)
    {
        RaceIdentityConstraints? identity = recipe.RaceIdentities.FirstOrDefault(
            item => string.Equals(item.RaceContentId, raceContentId, StringComparison.Ordinal));
        if (identity?.InviteOrganizationIds is not { Count: > 0 })
        {
            return null;
        }

        return identity.InviteOrganizationIds.ToHashSet(StringComparer.Ordinal);
    }

    public static RaceRiderProfile ToRaceProfile(RiderCareer career)
    {
        double readiness = career.ComputeReadiness();
        double criticalPowerW = career.CriticalPowerW * readiness;
        double peakPowerW = Math.Max(career.PeakPowerW * readiness, criticalPowerW);
        return new RaceRiderProfile(
            career.Id,
            career.OrganizationId!.Value,
            criticalPowerW,
            career.WPrimeCapacityJ,
            peakPowerW,
            career.WPrimeRecoveryJPerSecond,
            career.LowIntensityDurability,
            career.HighIntensityDurability,
            career.BodyMassKg,
            career.SystemMassKg,
            career.CdAM2,
            career.BaseCrr,
            career.Positioning,
            career.Handling,
            career.TacticalAwareness,
            career.OriginDefinitionId);
    }
}
