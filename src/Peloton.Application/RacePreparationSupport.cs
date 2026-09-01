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
            .Take(2)
            .ToArray();
        if (squad.Length < 2)
        {
            return CommandResult.Reject("PREP_STRATEGY_RIDERS_INVALID");
        }

        return application.Execute(new SetRacePreparationStrategyCommand(
            squad[0],
            squad[1],
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
