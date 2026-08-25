using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Simulation.Tests.Race;

public sealed class RequiredPowerSolverTests
{
    [Fact]
    public void ShelterReducesOnlyAerodynamicDemand()
    {
        RequiredPowerBreakdown exposed = RequiredPowerSolver.Calculate(Input(1.0));
        RequiredPowerBreakdown sheltered = RequiredPowerSolver.Calculate(Input(0.62));

        Assert.True(sheltered.AerodynamicPowerW < exposed.AerodynamicPowerW);
        Assert.Equal(exposed.RollingPowerW, sheltered.RollingPowerW, 8);
        Assert.Equal(exposed.GravityPowerW, sheltered.GravityPowerW, 8);
        Assert.Equal(exposed.AccelerationPowerW, sheltered.AccelerationPowerW, 8);
    }

    [Fact]
    public void PositiveGradientRaisesRequiredPower()
    {
        double flatPowerW = RequiredPowerSolver.Calculate(Input(1.0, 0.0)).TotalPowerW;
        double climbingPowerW = RequiredPowerSolver.Calculate(Input(1.0, 0.07)).TotalPowerW;

        Assert.True(climbingPowerW > flatPowerW);
    }

    [Fact]
    public void AccelerationAddsDemandWithoutChangingSteadyComponents()
    {
        RequiredPowerBreakdown steady = RequiredPowerSolver.Calculate(Input(1.0));
        RequiredPowerBreakdown accelerating = RequiredPowerSolver.Calculate(Input(1.0) with
        {
            AccelerationMps2 = 0.4,
        });

        Assert.True(accelerating.AccelerationPowerW > 0.0);
        Assert.Equal(steady.AerodynamicPowerW, accelerating.AerodynamicPowerW, 8);
        Assert.Equal(steady.RollingPowerW, accelerating.RollingPowerW, 8);
        Assert.Equal(steady.GravityPowerW, accelerating.GravityPowerW, 8);
    }

    private static RequiredPowerInput Input(double shelterMultiplier, double gradient = 0.0)
    {
        return new RequiredPowerInput(
            GroundSpeedMps: 12.0,
            AccelerationMps2: 0.0,
            Gradient: gradient,
            AirDensityKgPerM3: 1.225,
            RelativeAirSpeedMps: 12.0,
            BaseCdAM2: 0.32,
            ShelterMultiplier: shelterMultiplier,
            RollingResistanceCoefficient: 0.004,
            TotalMassKg: 83.0);
    }
}
