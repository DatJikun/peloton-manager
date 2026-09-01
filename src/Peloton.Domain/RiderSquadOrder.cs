using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Domain;

/// <summary>
/// WorldTour origin ids use dotted slots: <c>.leader</c>, <c>.card</c>, <c>.support-1</c>, <c>.support-2</c>.
/// Alphabetical origin order puts <c>.card</c> before <c>.leader</c>, so the second star became the default captain.
/// Skeleton origin ids use hyphenated names without dotted slots; they keep allocation order (JSON / Id).
/// </summary>
public static class RiderSquadOrder
{
    public static int SlotRank(string? originDefinitionId)
    {
        if (string.IsNullOrEmpty(originDefinitionId))
        {
            return 50;
        }

        if (originDefinitionId.EndsWith(".leader", StringComparison.Ordinal))
        {
            return 0;
        }

        if (originDefinitionId.EndsWith(".card", StringComparison.Ordinal))
        {
            return 1;
        }

        if (originDefinitionId.EndsWith(".support-1", StringComparison.Ordinal))
        {
            return 2;
        }

        if (originDefinitionId.EndsWith(".support-2", StringComparison.Ordinal))
        {
            return 3;
        }

        return 10;
    }

    public static IOrderedEnumerable<RiderCareer> OrderSquad(IEnumerable<RiderCareer> riders)
    {
        ArgumentNullException.ThrowIfNull(riders);
        return riders
            .OrderBy(career => SlotRank(career.OriginDefinitionId))
            .ThenBy(career => career.Id.Value);
    }
}
