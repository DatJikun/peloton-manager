using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Peloton.Simulation.Race;

public static class RaceResultChecksum
{
    public static string Compute(RaceResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        using MemoryStream buffer = new();
        using (BinaryWriter writer = new(buffer, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("peloton-race-result-v1");
            writer.Write(result.ScenarioId);
            writer.Write(result.RouteId);
            writer.Write(result.PhysicsContractVersion);
            foreach (RaceRiderMetrics rider in result.RiderMetrics
                         .OrderBy(rider => rider.FinishTimeSeconds)
                         .ThenBy(rider => rider.RiderId.Value))
            {
                writer.Write(rider.RiderId.Value);
                writer.Write(rider.OrganizationId.Value);
                writer.Write(rider.FinishTimeSeconds);
                writer.Write(rider.EnergySpentJ);
                writer.Write(rider.WPrimeRemainingJ);
                writer.Write(rider.TimeAboveCriticalPowerSeconds);
                writer.Write(rider.MaximumGapAheadM);
                writer.Write(rider.LostShelterTransitions);
                writer.Write(rider.FinalGroupId);
            }

            writer.Write(result.MaximumGroupCount);
            writer.Write(result.DecisionCount);
        }

        return Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
    }
}
