using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Peloton.Domain;

namespace Peloton.Simulation.Race;

public sealed record RaceWatchCourseSegment(
    string Id,
    double LengthM,
    double Gradient,
    double RoadWidthM,
    double WindSpeedMps,
    double WindYawDegrees);

public sealed record RaceWatchCourse(
    double TotalLengthM,
    IReadOnlyList<RaceWatchCourseSegment> Segments);

public sealed record RaceWatchRiderFrame(
    WorldEntityId RiderId,
    double DistanceM,
    double GapM,
    double SpeedMps,
    double ShelterMultiplier,
    double Gradient);

public sealed record RaceWatchFrame(
    int WatchSecond,
    int RaceSecond,
    int Rate,
    bool Paused,
    double RouteLengthM,
    IReadOnlyList<RaceWatchRiderFrame> FocalRiders);

public sealed record RaceWatchReport(
    IReadOnlyList<RaceWatchFrame> Frames,
    RaceResult Result)
{
    public int WatchSeconds => Frames.Count == 0 ? 0 : Frames[^1].WatchSecond;
}

public sealed class RaceWatchClock
{
    public const int MinimumRate = 1;
    public const int MaximumRate = 120;

    private readonly RaceSession session;
    private readonly int rate;
    private int watchSecond;

    public RaceWatchClock(RaceSession session, int rate = 5)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (rate < MinimumRate || rate > MaximumRate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rate),
                "Watch rate must be between 1 and 120.");
        }

        this.session = session;
        this.rate = rate;
    }

    public RaceDecisionRequest? PendingDecision => session.PendingDecision;

    public RaceWatchFrame Current => RaceWatchProjector.ProjectFrame(session, watchSecond, rate);

    public RaceWatchFrame AdvanceOneWatchSecond()
    {
        if (session.IsCompleted || session.PendingDecision is not null)
        {
            return Current;
        }

        watchSecond++;
        for (int step = 0; step < rate; step++)
        {
            RaceStepResult result = session.Step();
            if (result.Status != RaceStepStatus.Advanced)
            {
                break;
            }
        }

        return Current;
    }

    public void Respond(RaceDecisionResolution resolution)
    {
        session.ResolveDecision(resolution);
    }
}

public static class RaceWatchProjector
{
    public static RaceWatchReport Project(RaceScenario scenario, long seed, int rate = 5)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        RaceSession session = new PrototypeRaceEngine().CreateSession(
            scenario,
            seed,
            NullWorldSpySink.Instance);
        RaceWatchClock clock = new(session, rate);
        List<RaceWatchFrame> frames = new() { clock.Current };
        while (!session.IsCompleted)
        {
            RaceWatchFrame frame = clock.AdvanceOneWatchSecond();
            frames.Add(frame);
            if (!frame.Paused)
            {
                continue;
            }

            RaceDecisionRequest request = clock.PendingDecision
                ?? throw new InvalidOperationException("Decision pause did not expose a request.");
            clock.Respond(new RaceDecisionResolution(
                request.Id,
                request.AuthorityId,
                request.DelegatedDefaultOption));
        }

        RaceResult result = session.Result
            ?? throw new InvalidOperationException("Watch projection completed without an official result.");
        return new RaceWatchReport(frames, result);
    }

    internal static RaceWatchFrame ProjectFrame(RaceSession session, int watchSecond, int rate)
    {
        RaceMotionSnapshot motion = session.GetMotionSnapshot();
        RaceRiderMotion[] focal = motion.Riders
            .OrderByDescending(rider => rider.DistanceM)
            .ThenBy(rider => rider.RiderId.Value)
            .Take(3)
            .ToArray();
        double leaderDistanceM = focal.Length == 0 ? 0.0 : focal[0].DistanceM;
        RaceWatchRiderFrame[] riders = focal
            .Select(rider => new RaceWatchRiderFrame(
                rider.RiderId,
                rider.DistanceM,
                Math.Max(0.0, leaderDistanceM - rider.DistanceM),
                rider.SpeedMps,
                rider.ShelterMultiplier,
                rider.Gradient))
            .ToArray();
        return new RaceWatchFrame(
            watchSecond,
            motion.RaceSecond,
            rate,
            session.PendingDecision is not null,
            motion.RouteLengthM,
            riders);
    }

    public static string ExportMarkdown(RaceWatchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        StringBuilder markdown = new();
        markdown.AppendLine("# Headless Watch clock");
        markdown.AppendLine();
        markdown.AppendLine(
            CultureInfo.InvariantCulture,
            $"Official winner {report.Result.WinnerId.Value}; checksum `{report.Result.Checksum}`.");
        markdown.AppendLine(
            "The supervising clock advances sequential one-second physics steps and pauses on decisions.");
        markdown.AppendLine();
        foreach (RaceWatchFrame frame in report.Frames)
        {
            markdown.AppendLine(
                CultureInfo.InvariantCulture,
                $"- watchSecond={frame.WatchSecond} simSecond={frame.RaceSecond} rate={frame.Rate} paused={frame.Paused.ToString().ToLowerInvariant()}");
            foreach (RaceWatchRiderFrame rider in frame.FocalRiders)
            {
                markdown.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"  - rider={rider.RiderId.Value} distanceM={rider.DistanceM:F2} gapM={rider.GapM:F2} speedMps={rider.SpeedMps:F2} shelter={rider.ShelterMultiplier:F2} gradient={rider.Gradient:F3}");
            }
        }

        return markdown.ToString();
    }
}
