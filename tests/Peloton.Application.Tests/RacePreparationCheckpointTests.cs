using System.Collections.Generic;
using Peloton.Domain;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class RacePreparationCheckpointTests
{
    [Fact]
    public void EqualityComparesAssignmentsByValueNotCollectionType()
    {
        SquadAssignment[] array =
        {
            new(new WorldEntityId(4), SquadRoles.Leader),
            new(new WorldEntityId(5), SquadRoles.Card),
        };
        List<SquadAssignment> list = new(array);

        RacePreparationCheckpoint fromArray = new(
            RacePreparationDefaults.PrototypeScenarioId,
            PlanConfirmed: true,
            array);
        RacePreparationCheckpoint fromList = new(
            RacePreparationDefaults.PrototypeScenarioId,
            PlanConfirmed: true,
            list);

        Assert.Equal(fromArray, fromList);
        Assert.Equal(fromArray.GetHashCode(), fromList.GetHashCode());
    }
}
