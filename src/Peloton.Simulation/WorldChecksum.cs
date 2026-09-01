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
            writer.Write("peloton-world-checksum-v7");
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
                writer.Write(person.OriginDefinitionId ?? string.Empty);
                writer.Write(person.Nationality ?? string.Empty);
                writer.Write(person.BirthYear ?? 0);
            }

            foreach (RiderCareer career in world.RiderCareers.OrderBy(career => career.Id.Value))
            {
                writer.Write(career.Id.Value);
                writer.Write(career.PersonId.Value);
                writer.Write(career.OrganizationId?.Value ?? 0);
                writer.Write(career.OriginDefinitionId);
                writer.Write(career.CriticalPowerW);
                writer.Write(career.WPrimeCapacityJ);
                writer.Write(career.PeakPowerW);
                writer.Write(career.WPrimeRecoveryJPerSecond);
                writer.Write(career.LowIntensityDurability);
                writer.Write(career.HighIntensityDurability);
                writer.Write(career.BodyMassKg);
                writer.Write(career.SystemMassKg);
                writer.Write(career.CdAM2);
                writer.Write(career.BaseCrr);
                writer.Write(career.Positioning);
                writer.Write(career.Handling);
                writer.Write(career.TacticalAwareness);
                writer.Write(career.Form01);
                writer.Write(career.Freshness01);
                writer.Write(career.Fatigue01);
                writer.Write(career.Loyalty01);
                foreach (RiderCareerResult result in career.Results)
                {
                    writer.Write(result.RaceContentId);
                    writer.Write(result.DayNumber);
                    writer.Write(result.Place);
                    writer.Write(result.DidNotFinish);
                }
            }

            foreach (RiderContract contract in world.RiderContracts.OrderBy(contract => contract.Id.Value))
            {
                writer.Write(contract.Id.Value);
                writer.Write(contract.RiderCareerId.Value);
                writer.Write(contract.OrganizationId.Value);
                writer.Write(contract.AnnualWage);
                writer.Write(contract.StartDate.DayNumber);
                writer.Write(contract.EndDate.DayNumber);
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
                writer.Write(organization.Country);
                writer.Write(organization.Division);
                writer.Write(organization.LicenceYearsRemaining);
                writer.Write(organization.TitleSponsor);
                writer.Write(organization.Bike);
                writer.Write(organization.Groupset);
                writer.Write(organization.EstimatedBudgetEur);
                writer.Write(organization.CashEur);
                writer.Write(organization.TitleSponsorAnnualFeeEur);
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
            writer.Write(world.FinancialYearDays);
            writer.Write(world.LastCompletedRaceDay);
            writer.Write(world.GeneratePeriodicRaces);
            writer.Write(world.LastDayNotes.Count);
            foreach (string note in world.LastDayNotes)
            {
                writer.Write(note);
            }

            foreach (CalendarEntry entry in world.CalendarEntries
                         .OrderBy(entry => entry.DayNumber)
                         .ThenBy(entry => entry.Id.Value))
            {
                writer.Write(entry.Id.Value);
                writer.Write(entry.DayNumber);
                writer.Write((int)entry.Kind);
                writer.Write(entry.Title);
                writer.Write(entry.OfficialResult ?? string.Empty);
                writer.Write(entry.ResultAcknowledged);
                writer.Write(entry.RaceContentId ?? string.Empty);
            }

            foreach (OrganizationRaceEntry entry in world.OrganizationRaceEntries
                         .OrderBy(entry => entry.OrganizationId.Value)
                         .ThenBy(entry => entry.RaceContentId, StringComparer.Ordinal))
            {
                writer.Write(entry.OrganizationId.Value);
                writer.Write(entry.RaceContentId);
                writer.Write(entry.Entered);
            }
        }

        return Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
    }
}
