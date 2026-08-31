using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public static class WorldRaceScenarioAssembler
{
    public static RaceScenario Assemble(
        WorldState world,
        WorldRecipe recipe,
        RaceScenarioTemplate template,
        string raceContentId,
        RacePreparationStrategy? playerStrategy = null,
        WorldEntityId? playerOrganizationId = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(raceContentId);

        Dictionary<string, Organization> organizationsByOrigin = world.Organizations
            .ToDictionary(organization => organization.OriginDefinitionId, StringComparer.Ordinal);
        Dictionary<string, WorldEntityId> raceTeamToOrganization = recipe.TeamRaceMappings
            .ToDictionary(
                mapping => mapping.RaceTeamId,
                mapping => organizationsByOrigin[mapping.OrganizationId].Id,
                StringComparer.Ordinal);
        Dictionary<string, RiderCareer> careersByOrigin = world.RiderCareers
            .ToDictionary(career => career.OriginDefinitionId, StringComparer.Ordinal);
        WorldEntityId humanAuthorityId = ResolveHumanAuthorityId(world);
        HashSet<WorldEntityId> enteredOrganizationIds = world.OrganizationRaceEntries
            .Where(entry =>
                string.Equals(entry.RaceContentId, raceContentId, StringComparison.Ordinal) && entry.Entered)
            .Select(entry => entry.OrganizationId)
            .ToHashSet();

        RaceRiderProfile[] riders = world.RiderCareers
            .Where(career => enteredOrganizationIds.Contains(career.OrganizationId))
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
            template.Route,
            riders,
            startingPositions,
            commands,
            template.InitialSpeedMps,
            template.MaximumDurationSeconds,
            tacticalPlans,
            template.TuningIdentity);
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

    public static RaceRiderProfile ToRaceProfile(RiderCareer career)
    {
        double readiness = career.ComputeReadiness();
        double criticalPowerW = career.CriticalPowerW * readiness;
        double peakPowerW = Math.Max(career.PeakPowerW * readiness, criticalPowerW);
        return new RaceRiderProfile(
            career.Id,
            career.OrganizationId,
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
