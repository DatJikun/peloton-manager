using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public static class BunchSprintResolver
{
    public const double LaunchDistanceM = 800.0;
    public const double FinishGradientWindowM = 2_000.0;
    public const double MaximumMeanGradient = 0.015;
    public const double LeadGroupGapM = 15.0;
    public const double LeadGroupGapSeconds = 3.0;
    public const int MinimumLeadGroupSize = 8;
    public const double UnclassifiedMinimumLengthM = 50_000.0;

    public static bool IsClassifiedEligible(ClassifiedStageType? classifiedStageType, double totalLengthM)
    {
        if (classifiedStageType is ClassifiedStageType.Mountain
            or ClassifiedStageType.MountainSummit
            or ClassifiedStageType.IndividualTimeTrial
            or ClassifiedStageType.TeamTimeTrial
            or ClassifiedStageType.CobbleClassic)
        {
            return false;
        }

        if (classifiedStageType is ClassifiedStageType.Flat
            or ClassifiedStageType.Hilly
            or ClassifiedStageType.Mixed)
        {
            return true;
        }

        return totalLengthM >= UnclassifiedMinimumLengthM;
    }

    public static double MeanGradientOfLastWindow(RaceDefinition definition, double windowM = FinishGradientWindowM)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!double.IsFinite(windowM) || windowM <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowM));
        }

        double windowStartM = Math.Max(0.0, definition.TotalLengthM - windowM);
        double weighted = 0.0;
        double coveredM = 0.0;
        double cursorM = 0.0;
        foreach (RaceRouteSegment segment in definition.Segments)
        {
            double segmentStartM = cursorM;
            double segmentEndM = cursorM + segment.LengthM;
            cursorM = segmentEndM;
            double overlapStartM = Math.Max(segmentStartM, windowStartM);
            double overlapEndM = Math.Min(segmentEndM, definition.TotalLengthM);
            double overlapM = overlapEndM - overlapStartM;
            if (overlapM <= 0.0)
            {
                continue;
            }

            weighted += segment.Gradient * overlapM;
            coveredM += overlapM;
        }

        return coveredM <= 0.0 ? 0.0 : weighted / coveredM;
    }

    public static bool ShouldLaunch(
        RaceDefinition definition,
        ClassifiedStageType? classifiedStageType,
        double leaderDistanceM,
        double leaderSpeedMps,
        int leaderGroupId,
        IReadOnlyList<BunchSprintRiderSnapshot> unfinished)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(unfinished);
        if (!IsClassifiedEligible(classifiedStageType, definition.TotalLengthM))
        {
            return false;
        }

        double remainingM = definition.TotalLengthM - leaderDistanceM;
        if (remainingM > LaunchDistanceM)
        {
            return false;
        }

        if (MeanGradientOfLastWindow(definition) >= MaximumMeanGradient &&
            classifiedStageType != ClassifiedStageType.Flat)
        {
            return false;
        }

        return CountLeadGroup(leaderDistanceM, leaderSpeedMps, leaderGroupId, unfinished) >= MinimumLeadGroupSize;
    }

    public static int CountLeadGroup(
        double leaderDistanceM,
        double leaderSpeedMps,
        int leaderGroupId,
        IReadOnlyList<BunchSprintRiderSnapshot> unfinished)
    {
        ArgumentNullException.ThrowIfNull(unfinished);
        double safeSpeedMps = Math.Max(0.1, leaderSpeedMps);
        int count = 0;
        foreach (BunchSprintRiderSnapshot rider in unfinished)
        {
            if (rider.GroupId != leaderGroupId)
            {
                continue;
            }

            double gapM = leaderDistanceM - rider.DistanceM;
            double gapSeconds = gapM / safeSpeedMps;
            if (gapM <= LeadGroupGapM || gapSeconds <= LeadGroupGapSeconds)
            {
                count++;
            }
        }

        return count;
    }

    public static double SpeedForPowerW(
        double targetPowerW,
        double gradient,
        double airDensityKgPerM3,
        double windSpeedMps,
        double windYawDegrees,
        double baseCdAM2,
        double shelterMultiplier,
        double rollingResistanceCoefficient,
        double totalMassKg)
    {
        if (!double.IsFinite(targetPowerW) || targetPowerW <= 0.0)
        {
            return 2.0;
        }

        double lowMps = 2.0;
        double highMps = 32.0;
        for (int iteration = 0; iteration < 40; iteration++)
        {
            double midMps = (lowMps + highMps) * 0.5;
            double yawRadians = windYawDegrees * (Math.PI / 180.0);
            double headwindMps = Math.Cos(yawRadians) * windSpeedMps;
            double crosswindMps = Math.Sin(yawRadians) * windSpeedMps;
            double relativeAirSpeedMps = Math.Sqrt(
                Math.Pow(Math.Max(0.0, midMps + headwindMps), 2.0) +
                Math.Pow(crosswindMps, 2.0));
            RequiredPowerBreakdown demand = RequiredPowerSolver.Calculate(new RequiredPowerInput(
                midMps,
                AccelerationMps2: 0.0,
                gradient,
                airDensityKgPerM3,
                relativeAirSpeedMps,
                baseCdAM2,
                shelterMultiplier,
                rollingResistanceCoefficient,
                totalMassKg));
            if (demand.TotalPowerW > targetPowerW)
            {
                highMps = midMps;
            }
            else
            {
                lowMps = midMps;
            }
        }

        return lowMps;
    }
}

public readonly record struct BunchSprintRiderSnapshot(
    WorldEntityId RiderId,
    int GroupId,
    double DistanceM,
    double SpeedMps);
