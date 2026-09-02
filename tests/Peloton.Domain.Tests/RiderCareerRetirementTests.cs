using Xunit;

namespace Peloton.Domain.Tests;

public sealed class RiderCareerRetirementTests
{
    [Fact]
    public void RetireStoresFormerClubThenDetaches()
    {
        WorldEntityId organizationId = new(3);
        RiderCareer career = new(
            new WorldEntityId(1),
            new WorldEntityId(2),
            organizationId,
            "rider.test.retire",
            criticalPowerW: 400.0,
            wPrimeCapacityJ: 25_000.0,
            peakPowerW: 900.0,
            wPrimeRecoveryJPerSecond: 40.0,
            lowIntensityDurability: 0.90,
            highIntensityDurability: 0.88,
            bodyMassKg: 70.0,
            systemMassKg: 8.0,
            cdAM2: 0.27,
            baseCrr: 0.004,
            positioning: 0.80,
            handling: 0.80,
            tacticalAwareness: 0.80);

        career.Retire();

        Assert.True(career.IsRetired);
        Assert.Null(career.OrganizationId);
        Assert.Equal(organizationId, career.RetiredFromOrganizationId);

        career.Retire();
        Assert.Equal(organizationId, career.RetiredFromOrganizationId);
    }
}
