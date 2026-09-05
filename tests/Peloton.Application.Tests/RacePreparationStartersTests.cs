using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class RacePreparationStartersTests
{
    private const string WtScenarioId = "scenario.peloton.wt-2026";
    private const string TduRaceContentId = "race.wt2026.tour_down_under";
    private const long GateSeed = 91234;

    [Fact]
    public void PrepareRaceSetsInitialDefaultStartersMatchingRequiredCount()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        AdvanceToTdu(application);

        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);
        RacePreparationProjection prep = Assert.IsType<RacePreparationProjection>(application.RacePreparation);

        Assert.Equal(7, prep.RequiredStartersCount);
        Assert.NotNull(prep.SelectedRiderIds);
        Assert.Equal(7, prep.SelectedRiderIds.Count);
        Assert.Equal(prep.Squad.Take(7), prep.SelectedRiderIds);
        Assert.Null(prep.LeaderId);
        Assert.Null(prep.SupportId);
    }

    [Fact]
    public void SetRacePreparationStartersUpdatesSelectedRidersAndAdjustsLeader()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        AdvanceToTdu(application);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);

        RacePreparationProjection prep = application.RacePreparation!;
        Assert.True(prep.Squad.Count >= 20);

        Assert.True(RacePreparationSupport.SetDefaultStrategy(application).Succeeded);
        WorldEntityId originalLeader = application.RacePreparation!.LeaderId!.Value;

        WorldEntityId[] customStarters = prep.Squad.Skip(5).Take(7).ToArray();
        Assert.DoesNotContain(originalLeader, customStarters);

        Assert.True(application.Execute(new SetRacePreparationStartersCommand(customStarters)).Succeeded);

        RacePreparationProjection updated = application.RacePreparation!;
        Assert.Equal(customStarters, updated.SelectedRiderIds);
        Assert.NotNull(updated.LeaderId);
        Assert.Contains(updated.LeaderId!.Value, customStarters);
        Assert.NotNull(updated.SupportId);
        Assert.Contains(updated.SupportId!.Value, customStarters);
    }

    [Fact]
    public void SetRacePreparationStartersRejectsInvalidCountOrMembers()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        AdvanceToTdu(application);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);

        RacePreparationProjection prep = application.RacePreparation!;

        WorldEntityId[] tooFew = prep.Squad.Take(6).ToArray();
        CommandResult resultTooFew = application.Execute(new SetRacePreparationStartersCommand(tooFew));
        Assert.False(resultTooFew.Succeeded);
        Assert.Equal("PREP_STARTERS_COUNT_INVALID", resultTooFew.ReasonCode);

        WorldEntityId[] tooMany = prep.Squad.Take(8).ToArray();
        CommandResult resultTooMany = application.Execute(new SetRacePreparationStartersCommand(tooMany));
        Assert.False(resultTooMany.Succeeded);
        Assert.Equal("PREP_STARTERS_COUNT_INVALID", resultTooMany.ReasonCode);

        WorldEntityId[] duplicates = prep.Squad.Take(6).Concat(new[] { prep.Squad[0] }).ToArray();
        CommandResult resultDup = application.Execute(new SetRacePreparationStartersCommand(duplicates));
        Assert.False(resultDup.Succeeded);
        Assert.Equal("PREP_STARTERS_INVALID", resultDup.ReasonCode);

        WorldEntityId outsideRider = application.World!.RiderCareers
            .First(career => career.OrganizationId != application.GetAccessContext().CurrentOrganizationId!.Value)
            .Id;
        WorldEntityId[] foreign = prep.Squad.Take(6).Concat(new[] { outsideRider }).ToArray();
        CommandResult resultForeign = application.Execute(new SetRacePreparationStartersCommand(foreign));
        Assert.False(resultForeign.Succeeded);
        Assert.Equal("PREP_STARTERS_INVALID", resultForeign.ReasonCode);
    }

    [Fact]
    public void SetStrategyEnforcesLeaderAndSupportInSelectedStarters()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        AdvanceToTdu(application);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);

        RacePreparationProjection prep = application.RacePreparation!;
        WorldEntityId[] starters = prep.Squad.Take(7).ToArray();
        WorldEntityId reserveRider = prep.Squad[8];

        CommandResult resultReserveLeader = application.Execute(new SetRacePreparationStrategyCommand(
            reserveRider,
            starters[1],
            RaceObjective.StageWin,
            RaceBriefingKind.Chase,
            starters));
        Assert.False(resultReserveLeader.Succeeded);
        Assert.Equal("PREP_STRATEGY_RIDERS_INVALID", resultReserveLeader.ReasonCode);

        CommandResult resultValid = application.Execute(new SetRacePreparationStrategyCommand(
            starters[2],
            starters[3],
            RaceObjective.StageWin,
            RaceBriefingKind.Chase,
            starters));
        Assert.True(resultValid.Succeeded);
        Assert.Equal(starters[2], application.RacePreparation!.LeaderId);
        Assert.Equal(starters[3], application.RacePreparation!.SupportId);
    }

    [Fact]
    public void SimulateRaceUsesExactCustomSelectedStartersForPlayerTeam()
    {
        GameApplication application = TestApplication.Create();
        Assert.True(application.Execute(new CreateWorldCommand(WtScenarioId, GateSeed)).Succeeded);
        AdvanceToTdu(application);
        Assert.True(application.Execute(new PrepareRaceCommand()).Succeeded);

        RacePreparationProjection prep = application.RacePreparation!;
        WorldEntityId playerOrgId = application.GetAccessContext().CurrentOrganizationId!.Value;

        WorldEntityId[] chosenStarters = prep.Squad.Skip(4).Take(7).ToArray();
        WorldEntityId[] unselectedRiders = prep.Squad.Take(4).ToArray();

        Assert.True(application.Execute(new SetRacePreparationStartersCommand(chosenStarters)).Succeeded);
        Assert.True(application.Execute(new SetRacePreparationStrategyCommand(
            chosenStarters[0],
            chosenStarters[1],
            RaceObjective.StageWin,
            RaceBriefingKind.Chase,
            chosenStarters)).Succeeded);
        Assert.True(application.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);

        Assert.True(application.Execute(new SimulateRaceCommand(TduRaceContentId)).Succeeded);

        WorldEntityId[] playerStartersInRace = application.World!.RiderCareers
            .Where(career => career.OrganizationId == playerOrgId &&
                             career.Results.Any(r => string.Equals(r.RaceContentId, TduRaceContentId, StringComparison.Ordinal)))
            .Select(career => career.Id)
            .ToArray();

        Assert.Equal(7, playerStartersInRace.Length);
        Assert.All(chosenStarters, id => Assert.Contains(id, playerStartersInRace));
        Assert.All(unselectedRiders, id => Assert.DoesNotContain(id, playerStartersInRace));
    }

    private static void AdvanceToTdu(GameApplication application)
    {
        for (int day = 0; day < 19; day++)
        {
            Assert.True(application.Execute(new AdvanceDayCommand()).Succeeded, $"day {day + 1}");
        }

        Assert.Equal(19, application.World!.CurrentDate.DayNumber);
        Assert.True(application.World.IsRaceDue);
    }
}
