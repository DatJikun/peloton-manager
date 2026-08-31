using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public static class CareerRaceBinder
{
    public static RaceScenario Bind(RaceScenario fixture, WorldState world)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(world);

        Dictionary<string, RosterRider> seats = world.RosterRiders
            .ToDictionary(rider => rider.RacePrototypeRiderId, StringComparer.Ordinal);
        Dictionary<WorldEntityId, WorldEntityId> riderMap = fixture.Riders.ToDictionary(
            rider => rider.RiderId,
            rider => Seat(seats, rider.ContentId).PersonId);
        Dictionary<WorldEntityId, WorldEntityId> orgMap = fixture.Riders
            .GroupBy(rider => rider.OrganizationId)
            .ToDictionary(
                group => group.Key,
                group => Seat(seats, group.First().ContentId).OrganizationId);
        Dictionary<WorldEntityId, WorldEntityId> authorityMap = AssignAuthorities(world, orgMap.Values.Distinct());

        RaceRiderProfile[] riders = fixture.Riders
            .Select(rider => new RaceRiderProfile(
                riderMap[rider.RiderId],
                orgMap[rider.OrganizationId],
                rider.CriticalPowerW,
                rider.WPrimeCapacityJ,
                rider.PeakPowerW,
                rider.WPrimeRecoveryJPerSecond,
                rider.LowIntensityDurability,
                rider.HighIntensityDurability,
                rider.BodyMassKg,
                rider.SystemMassKg,
                rider.CdAM2,
                rider.BaseCrr,
                rider.Positioning,
                rider.Handling,
                rider.TacticalAwareness,
                rider.ContentId))
            .ToArray();
        RaceStartingPosition[] positions = fixture.StartingPositions
            .Select(position => new RaceStartingPosition(riderMap[position.RiderId], position.DistanceM))
            .ToArray();
        RaceCommand[] commands = fixture.Commands
            .Select(command => new RaceCommand(
                command.SimulationSecond,
                orgMap[command.OrganizationId],
                riderMap[command.RiderId],
                command.Kind))
            .ToArray();
        RaceTacticalPlan[] plans = fixture.TacticalPlans
            .Select(plan => new RaceTacticalPlan(
                plan.TriggerSecond,
                riderMap[plan.SupportRiderId],
                plan.Observation with
                {
                    OrganizationId = orgMap[plan.Observation.OrganizationId],
                    DecisionAuthorityId = authorityMap[orgMap[plan.Observation.OrganizationId]],
                },
                plan.Briefing))
            .ToArray();

        return new RaceScenario(
            fixture.Id,
            fixture.Definition,
            riders,
            positions,
            commands,
            fixture.InitialSpeedMps,
            fixture.MaximumDurationSeconds,
            plans,
            fixture.TuningIdentity);
    }

    public static WorldEntityId[] PlayerSquad(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        WorldEntityId? employerId = PlayerOrganizationId(world);
        if (employerId is null)
        {
            return Array.Empty<WorldEntityId>();
        }

        return world.RosterRiders
            .Where(rider => rider.OrganizationId == employerId.Value)
            .Select(rider => rider.PersonId)
            .OrderBy(id => id.Value)
            .ToArray();
    }

    public static WorldEntityId? PlayerOrganizationId(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.ManagerCareers.Count == 0)
        {
            return null;
        }

        return world.Employments
            .FirstOrDefault(employment => employment.Id == world.ManagerCareers[0].ActiveEmploymentId)
            ?.OrganizationId;
    }

    public static SquadAssignment[] DefaultAssignments(WorldState world)
    {
        return PlayerSquad(world)
            .Select(id => new SquadAssignment(id, DefaultRole(world, id)))
            .ToArray();
    }

    public static IReadOnlyList<SquadSeat> Seats(WorldState world, IReadOnlyList<SquadAssignment>? assignments)
    {
        ArgumentNullException.ThrowIfNull(world);
        Dictionary<long, string> roles = ResolveRoles(world, assignments);
        return PlayerSquad(world)
            .Select(id =>
            {
                string role = roles[id.Value];
                string name = world.Persons.First(person => person.Id == id).Name;
                return new SquadSeat(id, name, role, SquadRoles.Why(role));
            })
            .ToArray();
    }

    public static IReadOnlyList<SquadAssignment> AssignRole(
        WorldState world,
        IReadOnlyList<SquadAssignment>? assignments,
        WorldEntityId riderId,
        string role)
    {
        Dictionary<long, string> roles = ResolveRoles(world, assignments);
        if (role is SquadRoles.Leader or SquadRoles.Card)
        {
            foreach (long existing in roles.Keys.ToArray())
            {
                if (roles[existing] == role && existing != riderId.Value)
                {
                    roles[existing] = SquadRoles.Worker;
                }
            }
        }

        roles[riderId.Value] = role;
        return PlayerSquad(world)
            .Select(id => new SquadAssignment(id, roles[id.Value]))
            .ToArray();
    }

    public static bool HasLeaderAndCard(WorldState world, IReadOnlyList<SquadAssignment>? assignments)
    {
        IReadOnlyList<SquadSeat> seats = Seats(world, assignments);
        return seats.Count(seat => seat.Role == SquadRoles.Leader) == 1 &&
            seats.Count(seat => seat.Role == SquadRoles.Card) == 1;
    }

    private static Dictionary<long, string> ResolveRoles(
        WorldState world,
        IReadOnlyList<SquadAssignment>? assignments)
    {
        Dictionary<long, string> roles = DefaultAssignments(world)
            .ToDictionary(assignment => assignment.RiderId.Value, assignment => assignment.Role);
        if (assignments is null)
        {
            return roles;
        }

        foreach (SquadAssignment assignment in assignments)
        {
            if (roles.ContainsKey(assignment.RiderId.Value) && SquadRoles.IsKnown(assignment.Role))
            {
                roles[assignment.RiderId.Value] = assignment.Role;
            }
        }

        return roles;
    }

    private static string DefaultRole(WorldState world, WorldEntityId riderId)
    {
        RosterRider? rider = world.RosterRiders.FirstOrDefault(item => item.PersonId == riderId);
        if (rider is null)
        {
            return SquadRoles.Worker;
        }

        if (rider.RacePrototypeRiderId.EndsWith("-leader", StringComparison.Ordinal))
        {
            return SquadRoles.Leader;
        }

        if (rider.RacePrototypeRiderId.EndsWith("-card", StringComparison.Ordinal))
        {
            return SquadRoles.Card;
        }

        return SquadRoles.Worker;
    }

    private static RosterRider Seat(Dictionary<string, RosterRider> seats, string prototypeRiderId)
    {
        if (!seats.TryGetValue(prototypeRiderId, out RosterRider? seat))
        {
            throw new InvalidOperationException($"Roster is missing prototype rider '{prototypeRiderId}'.");
        }

        return seat;
    }

    private static Dictionary<WorldEntityId, WorldEntityId> AssignAuthorities(
        WorldState world,
        IEnumerable<WorldEntityId> organizationIds)
    {
        WorldEntityId? playerOrg = PlayerOrganizationId(world);
        DecisionAuthority human = world.DecisionAuthorities
            .First(authority => authority.Kind == DecisionAuthorityKind.HumanInput);
        Queue<WorldEntityId> ai = new(
            world.DecisionAuthorities
                .Where(authority => authority.Kind == DecisionAuthorityKind.AIInput)
                .OrderBy(authority => authority.Id.Value)
                .Select(authority => authority.Id));
        Dictionary<WorldEntityId, WorldEntityId> map = new();
        foreach (WorldEntityId organizationId in organizationIds.OrderBy(id => id.Value))
        {
            map[organizationId] = organizationId == playerOrg ? human.Id : ai.Dequeue();
        }

        return map;
    }
}
