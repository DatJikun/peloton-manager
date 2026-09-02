namespace Peloton.Simulation.Race;

public static class RaceTuning
{
    public const double GravityMps2 = 9.80665;

    public const double MaximumDesiredAccelerationMps2 = 0.5;

    public const double MaximumCriticalPowerLossFraction = 0.35;

    public const double MaximumPeakPowerLossFraction = 0.45;

    public const double DriftMps = 0.78;

    public const double SlotSpacingM = 0.7;

    public const double GroupSplitGapM = 5.0;

    public const double FinaleM = 24_000.0;

    public const double TempoFactorFinale = 1.00;

    public const double TempoFactorOutsideFinale = 0.92;

    public const double CobbleSurgeCost = 0.286;

    public const double CobbleCrrDelta = 0.018;

    public const double CobbleCrrHandlingIntercept = 1.60;

    public const double CobbleCrrHandlingSlope = 1.00;

    public const double CobbleShelterFloor = 0.85;

    public const int CobbleSurgeSeconds = 12;

    public const double CobbleSurgeSpeedMps = 2.5;

    public const double LaunchSprintIntentBonus = 0.50;

    public const double AttackIntentBonus = 0.40;

    public const double ForcePaceIntentBonus = 0.40;

    public const double ConserveIntentBonus = -0.30;

    public const double SprintFinaleBonus = 0.25;

    public const double SprintFinaleDistanceM = 3_000.0;

    public const double CobblePositioningBase = 0.21;

    public const double CobblePositioningHandlingWeight = 0.91;

    public const double SelectiveGradientThreshold = 0.03;
}
