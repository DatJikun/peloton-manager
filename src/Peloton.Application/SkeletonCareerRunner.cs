using System;
using System.IO;
using Peloton.Domain;
using Peloton.Simulation;

namespace Peloton.Application;

public sealed record SkeletonRunReport(
    bool Crashed,
    int WorldDay,
    string Checksum,
    int RaceCount,
    string? FailureReason);

public sealed class SkeletonCareerRunner
{
    public const int DaysPerSeason = 12;
    private const string PrototypeRaceScenarioId = "race-scenario.peloton.prototype-v0";

    private readonly GameApplication application;

    public SkeletonCareerRunner(GameApplication application)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public SkeletonRunReport Run(int seasons, string autosaveDirectory)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seasons);
        ArgumentException.ThrowIfNullOrWhiteSpace(autosaveDirectory);
        Directory.CreateDirectory(autosaveDirectory);

        try
        {
            for (int season = 1; season <= seasons; season++)
            {
                for (int day = 0; day < DaysPerSeason; day++)
                {
                    Ensure(application.Execute(new AdvanceDayCommand()));
                }

                Ensure(application.Execute(new PrepareRaceCommand()));
                string autosavePath = Path.Combine(autosaveDirectory, $"season-{season}-pre-race.peloton");
                Ensure(application.Execute(new StartRaceCommand(
                    autosavePath,
                    PrototypeRaceScenarioId)));
                while (application.State == GameState.RaceLive)
                {
                    Ensure(application.Execute(new AdvanceRaceCommand()));
                    if (application.PendingRaceDecision is PendingRaceDecision decision)
                    {
                        Ensure(application.Execute(new RespondToRaceDecisionCommand(
                            decision.RequestId,
                            decision.AuthorityId,
                            decision.DelegatedDefaultOption)));
                    }
                }

                Ensure(application.Execute(new AcknowledgeRaceResultsCommand()));
                Ensure(application.Execute(new CompleteRaceDebriefCommand()));
            }

            return Report(crashed: false, null);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            return Report(crashed: true, exception.Message);
        }
    }

    private SkeletonRunReport Report(bool crashed, string? failureReason)
    {
        WorldState? world = application.World;
        return new SkeletonRunReport(
            crashed,
            world?.CurrentDate.DayNumber ?? 0,
            world is null ? string.Empty : WorldChecksum.Compute(world),
            world?.RaceCount ?? 0,
            failureReason);
    }

    private static void Ensure(CommandResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.ReasonCode);
        }
    }
}
