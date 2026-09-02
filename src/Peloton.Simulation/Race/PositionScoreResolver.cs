using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public static class PositionScoreResolver
{
    public static double Score(
        RaceRiderProfile profile,
        RaceCommandKind intent,
        double remainingM,
        ClassifiedStageType? classifiedStageType,
        IReadOnlyList<(WorldEntityId RiderId, double PeakPowerPerKg)>? groupPeakPowerPerKg = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        double positioning = profile.Positioning;
        if (classifiedStageType == ClassifiedStageType.CobbleClassic)
        {
            positioning *= CobblePositioningScale(profile.Handling);
        }

        return positioning + IntentBonus(intent) + FinaleBonus(
            profile,
            remainingM,
            classifiedStageType,
            groupPeakPowerPerKg);
    }

    public static double CobblePositioningScale(double handling) =>
        RaceTuning.CobblePositioningBase + (RaceTuning.CobblePositioningHandlingWeight * handling);

    public static double IntentBonus(RaceCommandKind intent) =>
        intent switch
        {
            RaceCommandKind.LaunchSprint => RaceTuning.LaunchSprintIntentBonus,
            RaceCommandKind.Attack => RaceTuning.AttackIntentBonus,
            RaceCommandKind.ForcePace => RaceTuning.ForcePaceIntentBonus,
            RaceCommandKind.Conserve => RaceTuning.ConserveIntentBonus,
            _ => 0.0,
        };

    public static double FinaleBonus(
        RaceRiderProfile profile,
        double remainingM,
        ClassifiedStageType? classifiedStageType,
        IReadOnlyList<(WorldEntityId RiderId, double PeakPowerPerKg)>? groupPeakPowerPerKg)
    {
        if (classifiedStageType != ClassifiedStageType.Flat ||
            remainingM > RaceTuning.SprintFinaleDistanceM ||
            groupPeakPowerPerKg is null ||
            groupPeakPowerPerKg.Count == 0)
        {
            return 0.0;
        }

        double riderPeakPerKg = profile.PeakPowerW / profile.BodyMassKg;
        int quarterSize = Math.Max(1, (int)Math.Ceiling(groupPeakPowerPerKg.Count / 4.0));
        double threshold = groupPeakPowerPerKg
            .OrderByDescending(item => item.PeakPowerPerKg)
            .ThenBy(item => item.RiderId.Value)
            .Take(quarterSize)
            .Last()
            .PeakPowerPerKg;
        return riderPeakPerKg + 1e-12 >= threshold ? RaceTuning.SprintFinaleBonus : 0.0;
    }

    public static double CobbleShelterMultiplier(double shelter, double handling) =>
        1.0 - ((1.0 - shelter) * (0.25 + (0.75 * handling)));

    public static double EffectiveCobbleShelter(double shelter, double handling) =>
        Math.Max(CobbleShelterMultiplier(shelter, handling), RaceTuning.CobbleShelterFloor);

    public static double CobbleSurgeMultiplier(double handling) =>
        1.0 + (RaceTuning.CobbleSurgeCost * (1.0 - handling));
}
