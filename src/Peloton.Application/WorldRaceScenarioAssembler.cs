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
        RaceScenarioTemplate template)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(template);

        Dictionary<string, Organization> organizationsByOrigin = world.Organizations
            .ToDictionary(organization => organization.OriginDefinitionId, StringComparer.Ordinal);
        Dictionary<string, WorldEntityId> raceTeamToOrganization = recipe.TeamRaceMappings
            .ToDictionary(
                mapping => mapping.RaceTeamId,
                mapping => organizationsByOrigin[mapping.OrganizationId].Id,
                StringComparer.Ordinal);
        Dictionary<string, RiderCareer> careersByOrigin = world.RiderCareers
            .ToDictionary(career => career.OriginDefinitionId, StringComparer.Ordinal);

        RaceRiderProfile[] riders = world.RiderCareers
            .OrderBy(career => career.OriginDefinitionId, StringComparer.Ordinal)
            .Select(career => ToRaceProfile(career))
            .ToArray();

        IReadOnlyList<string> startingOrder = template.StartingOrderRiderIds;
        RaceStartingPosition[] startingPositions = startingOrder
            .Select((originId, index) => new RaceStartingPosition(
                careersByOrigin[originId].Id,
                (startingOrder.Count - 1 - index) * 0.7))
            .ToArray();

        RaceCommand[] commands = template.Commands
            .Select(command => new RaceCommand(
                command.SimulationSecond,
                raceTeamToOrganization[command.TeamId],
                careersByOrigin[command.RiderId].Id,
                command.Intent))
            .ToArray();

        RaceTacticalPlan[] tacticalPlans = template.TacticalPlans
            .Select(plan =>
            {
                RaceTeamTemplate team = template.Teams[plan.TeamId];
                WorldEntityId organizationId = raceTeamToOrganization[plan.TeamId];
                return new RaceTacticalPlan(
                    plan.TriggerSecond,
                    careersByOrigin[plan.SupportRiderId].Id,
                    new TeamRaceObservation(
                        organizationId,
                        new WorldEntityId(checked(organizationId.Value + 100)),
                        plan.OfficialGapSeconds,
                        plan.VisibleSplit,
                        plan.LeaderPositionBand,
                        plan.ResourceEstimate,
                        plan.ThreatEstimate,
                        team.Objective,
                        plan.Confidence),
                    team.Briefing);
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

    private static RaceRiderProfile ToRaceProfile(RiderCareer career)
    {
        return new RaceRiderProfile(
            career.Id,
            career.OrganizationId,
            career.CriticalPowerW,
            career.WPrimeCapacityJ,
            career.PeakPowerW,
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
