using Peloton.Domain;
using Xunit;

namespace Peloton.Domain.Tests;

public sealed class WorldEntityIdAllocatorTests
{
    [Fact]
    public void RetiredIdsAreNeverAllocatedAgain()
    {
        WorldEntityIdAllocator allocator = new();

        WorldEntityId first = allocator.Allocate();
        WorldEntityId second = allocator.Allocate();
        WorldEntityId third = allocator.Allocate();

        Assert.Equal(1, first.Value);
        Assert.Equal(2, second.Value);
        Assert.Equal(3, third.Value);
        Assert.Equal(3, allocator.HighWaterMark);
    }
}
