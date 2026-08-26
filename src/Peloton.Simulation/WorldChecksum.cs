using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Peloton.Domain;

namespace Peloton.Simulation;

public static class WorldChecksum
{
    public static string Compute(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        using MemoryStream buffer = new();
        using (BinaryWriter writer = new(buffer, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("peloton-world-checksum-v1");
            writer.Write(world.WorldId);
            writer.Write(world.MasterSeed);
            writer.Write(world.RngContractVersion);
            writer.Write(world.CurrentDate.DayNumber);
            writer.Write(world.EntityIdHighWaterMark);
            writer.Write(world.ContentIdentity.AggregateHash);
            writer.Write(world.RulesIdentity);

            foreach (RulesModuleIdentity module in world.RulesModules
                         .OrderBy(module => module.Slot, StringComparer.Ordinal))
            {
                writer.Write(module.Slot);
                writer.Write(module.Id);
                writer.Write(module.Contract);
                writer.Write(module.ContractVersion);
                writer.Write(module.ParameterIdentity);
            }

            foreach (Person person in world.Persons.OrderBy(person => person.Id.Value))
            {
                writer.Write(person.Id.Value);
                writer.Write(person.Name);
            }

            foreach (ManagerCareer manager in world.ManagerCareers.OrderBy(manager => manager.Id.Value))
            {
                writer.Write(manager.Id.Value);
                writer.Write(manager.PersonId.Value);
                writer.Write(manager.ActiveEmploymentId?.Value ?? 0);
            }

            foreach (Employment employment in world.Employments.OrderBy(employment => employment.Id.Value))
            {
                writer.Write(employment.Id.Value);
                writer.Write(employment.ManagerCareerId.Value);
                writer.Write(employment.OrganizationId.Value);
                writer.Write(employment.StartDate.DayNumber);
                writer.Write(employment.EndDate?.DayNumber ?? -1);
            }

            foreach (Organization organization in world.Organizations.OrderBy(organization => organization.Id.Value))
            {
                writer.Write(organization.Id.Value);
                writer.Write(organization.OriginDefinitionId);
                writer.Write(organization.Name);
                writer.Write(organization.DaysSimulated);
            }

            foreach (DecisionAuthority authority in world.DecisionAuthorities.OrderBy(authority => authority.Id.Value))
            {
                writer.Write(authority.Id.Value);
                writer.Write((int)authority.Kind);
            }

            writer.Write(world.RaceCount);
            writer.Write(world.LastRace is not null);
            if (world.LastRace is not null)
            {
                writer.Write(world.LastRace.RouteId);
                writer.Write(world.LastRace.WinnerId.Value);
                foreach (WorldEntityId rider in world.LastRace.FinishOrder)
                {
                    writer.Write(rider.Value);
                }
            }

            writer.Write(world.CalendarPeriodDays);
            writer.Write(world.LastCompletedRaceDay);
            writer.Write(world.LastDayNotes.Count);
            foreach (string note in world.LastDayNotes)
            {
                writer.Write(note);
            }
        }

        return Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
    }
}
