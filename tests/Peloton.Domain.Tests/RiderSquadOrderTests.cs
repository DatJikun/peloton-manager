using Peloton.Domain;
using Xunit;

namespace Peloton.Domain.Tests;

public sealed class RiderSquadOrderTests
{
    [Theory]
    [InlineData("rider.wt2026.alpecin.leader", 0)]
    [InlineData("rider.wt2026.alpecin.card", 1)]
    [InlineData("rider.wt2026.alpecin.support-1", 2)]
    [InlineData("rider.wt2026.alpecin.support-2", 3)]
    [InlineData("rider.race-prototype.alpha-leader", 10)]
    public void WorldTourDottedSlotsRankCaptainBeforeCard(string originId, int rank)
    {
        Assert.Equal(rank, RiderSquadOrder.SlotRank(originId));
    }

    [Fact]
    public void CardDoesNotSortBeforeLeaderAlphabeticallyOnceRanked()
    {
        Assert.True(string.CompareOrdinal("rider.wt2026.bahrain.card", "rider.wt2026.bahrain.leader") < 0);
        Assert.True(
            RiderSquadOrder.SlotRank("rider.wt2026.bahrain.leader") <
            RiderSquadOrder.SlotRank("rider.wt2026.bahrain.card"));
    }
}
