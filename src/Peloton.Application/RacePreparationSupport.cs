using System;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public static class RacePreparationSupport
{
    public static CommandResult SetDefaultStrategy(GameApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (application.World is null)
        {
            return CommandResult.Reject("GAME_STATE_INVALID");
        }

        AccessContext access = application.GetAccessContext();
        if (access.CurrentOrganizationId is not WorldEntityId organizationId)
        {
            return CommandResult.Reject("EMPLOYER_REQUIRED");
        }

        WorldEntityId[] squad = application.World.GetRiderCareersForOrganization(organizationId)
            .Select(career => career.Id)
            .ToArray();
        if (squad.Length < 2)
        {
            return CommandResult.Reject("PREP_STRATEGY_RIDERS_INVALID");
        }

        WorldEntityId leader = squad[0];
        if (application.World.TryGetTodaysRaceContentId() is string raceContentId)
        {
            OrganizationRaceEntry? entry = application.World.OrganizationRaceEntries.FirstOrDefault(
                item => item.OrganizationId == organizationId &&
                        string.Equals(item.RaceContentId, raceContentId, StringComparison.Ordinal));
            if (entry?.DesignatedLeaderId is WorldEntityId designatedLeaderId &&
                squad.Contains(designatedLeaderId))
            {
                leader = designatedLeaderId;
            }
        }

        WorldEntityId support = squad.First(id => id != leader);

        return application.Execute(new SetRacePreparationStrategyCommand(
            leader,
            support,
            RaceObjective.StageWin,
            RaceBriefingKind.Chase));
    }

    public static CommandResult ConfirmWithDefaultStrategy(GameApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        CommandResult strategy = SetDefaultStrategy(application);
        if (!strategy.Succeeded)
        {
            return strategy;
        }

        return application.Execute(new ConfirmRacePreparationPlanCommand());
    }
}
