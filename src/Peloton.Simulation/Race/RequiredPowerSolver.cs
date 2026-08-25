using System;

namespace Peloton.Simulation.Race;

public sealed record RequiredPowerInput(
    double GroundSpeedMps,
    double AccelerationMps2,
    double Gradient,
    double AirDensityKgPerM3,
    double RelativeAirSpeedMps,
    double BaseCdAM2,
    double ShelterMultiplier,
    double RollingResistanceCoefficient,
    double TotalMassKg);

public sealed record RequiredPowerBreakdown(
    double AerodynamicPowerW,
    double RollingPowerW,
    double GravityPowerW,
    double AccelerationPowerW)
{
    public double TotalPowerW => Math.Max(
        0.0,
        AerodynamicPowerW + RollingPowerW + GravityPowerW + AccelerationPowerW);
}

public static class RequiredPowerSolver
{
    public static RequiredPowerBreakdown Calculate(RequiredPowerInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        RequireNonNegative(input.GroundSpeedMps, nameof(input.GroundSpeedMps));
        RequireFinite(input.AccelerationMps2, nameof(input.AccelerationMps2));
        RequireFinite(input.Gradient, nameof(input.Gradient));
        RequirePositive(input.AirDensityKgPerM3, nameof(input.AirDensityKgPerM3));
        RequireNonNegative(input.RelativeAirSpeedMps, nameof(input.RelativeAirSpeedMps));
        RequirePositive(input.BaseCdAM2, nameof(input.BaseCdAM2));
        RequirePositive(input.ShelterMultiplier, nameof(input.ShelterMultiplier));
        if (input.ShelterMultiplier > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                "Shelter multiplier cannot exceed one.");
        }

        RequirePositive(
            input.RollingResistanceCoefficient,
            nameof(input.RollingResistanceCoefficient));
        RequirePositive(input.TotalMassKg, nameof(input.TotalMassKg));

        double angleRadians = Math.Atan(input.Gradient);
        double effectiveCdAM2 = input.BaseCdAM2 * input.ShelterMultiplier;
        double aerodynamicPowerW = 0.5 *
            input.AirDensityKgPerM3 *
            effectiveCdAM2 *
            input.RelativeAirSpeedMps *
            input.RelativeAirSpeedMps *
            input.RelativeAirSpeedMps;
        double rollingPowerW = input.RollingResistanceCoefficient *
            input.TotalMassKg *
            RaceTuning.GravityMps2 *
            Math.Cos(angleRadians) *
            input.GroundSpeedMps;
        double gravityPowerW = input.TotalMassKg *
            RaceTuning.GravityMps2 *
            Math.Sin(angleRadians) *
            input.GroundSpeedMps;
        double accelerationPowerW = input.TotalMassKg *
            input.AccelerationMps2 *
            input.GroundSpeedMps;

        return new RequiredPowerBreakdown(
            aerodynamicPowerW,
            rollingPowerW,
            gravityPowerW,
            accelerationPowerW);
    }

    private static void RequirePositive(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
