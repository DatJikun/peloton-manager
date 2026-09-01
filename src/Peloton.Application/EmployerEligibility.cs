using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Application;

/// <summary>
/// Which clubs the player may start at is content/rules data, not a WorldTour hard-code.
/// A later UCI pyramid pack lists Continental / ProTeam here; the same CreateWorld and
/// pre-season commands apply. Empty list = every organization in the recipe (skeleton).
/// </summary>
public static class EmployerEligibility
{
    public static bool IsStartable(string division, IReadOnlyList<string>? playerStartDivisions)
    {
        if (playerStartDivisions is null || playerStartDivisions.Count == 0)
        {
            return true;
        }

        return playerStartDivisions.Contains(division, StringComparer.Ordinal);
    }

    public static bool IsStartable(OrganizationDefinition organization, WorldRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(recipe);
        return IsStartable(organization.Division, recipe.PlayerStartDivisions);
    }
}
