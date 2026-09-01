using System;
using System.Collections.Generic;
using System.Linq;
using Peloton.Domain;
using Peloton.Simulation;
using Peloton.Simulation.Course;

namespace Peloton.Simulation.Course;

public static class CourseCatalogGenerator
{
    public sealed record StagePlan(
        int StageIndex,
        int DayNumber,
        string StageType,
        double TargetKm);

    public sealed record GeneratedStageCourse(CourseProfile Profile, int DayNumber);

    public static IReadOnlyList<GeneratedStageCourse> GenerateSeason(
        IReadOnlyList<RaceIdentityConstraints> identities,
        IReadOnlyList<CalendarRaceDetail> calendarRaces,
        int seasonYear,
        long masterSeed,
        Func<WorldEntityId> allocateId)
    {
        ArgumentNullException.ThrowIfNull(identities);
        ArgumentNullException.ThrowIfNull(calendarRaces);
        ArgumentNullException.ThrowIfNull(allocateId);

        Dictionary<string, RaceIdentityConstraints> byId = identities
            .ToDictionary(identity => identity.RaceContentId, StringComparer.Ordinal);
        List<GeneratedStageCourse> generated = new();
        foreach (CalendarRaceDetail race in calendarRaces.OrderBy(item => item.StartDayNumber))
        {
            if (!byId.TryGetValue(race.Id, out RaceIdentityConstraints? constraints))
            {
                throw new InvalidOperationException($"Missing race identity for '{race.Id}'.");
            }

            ulong seasonSeed = StableSeedDerivation.Derive(
                masterSeed,
                $"course-catalog:{seasonYear}:{race.Id}");
            DeterministicRng rng = new(seasonSeed);
            IReadOnlyList<StagePlan> plans = BuildStagePlans(constraints, race, rng);
            int attempt = 0;
            while (true)
            {
                try
                {
                    generated.AddRange(
                        BuildRaceProfiles(
                            constraints,
                            race,
                            plans,
                            seasonYear,
                            masterSeed,
                            allocateId,
                            rng));
                    break;
                }
                catch (InvalidOperationException) when (attempt < 3)
                {
                    attempt++;
                    rng = new DeterministicRng(seasonSeed + (ulong)attempt);
                    plans = BuildStagePlans(constraints, race, rng);
                }
            }
        }

        return generated;
    }

    private static List<StagePlan> BuildStagePlans(
        RaceIdentityConstraints constraints,
        CalendarRaceDetail race,
        DeterministicRng rng)
    {
        int stageCount = constraints.RacingStageCount;
        int span = race.EndDayNumber - race.StartDayNumber + 1;
        int restDays = Math.Max(0, span - stageCount);
        List<int> stageDays = DistributeStageDays(race.StartDayNumber, race.EndDayNumber, stageCount, restDays, rng);

        int ittCount = PickCount(rng, constraints.IttMin, constraints.IttMax);
        int tttCount = PickCount(rng, constraints.TttMin, constraints.TttMax);
        int summitCount = PickCount(rng, constraints.SummitFinishMin, constraints.SummitFinishMax);
        int mountainCount = PickCount(rng, constraints.MountainMin, constraints.MountainMax);
        int hillyCount = PickCount(rng, constraints.HillyMin, constraints.HillyMax);
        int flatCount = PickCount(rng, constraints.FlatMin, constraints.FlatMax);

        List<string> types = new();
        for (int i = 0; i < ittCount; i++)
        {
            types.Add("itt");
        }

        for (int i = 0; i < tttCount; i++)
        {
            types.Add("ttt");
        }

        for (int i = 0; i < summitCount; i++)
        {
            types.Add("summit");
        }

        int remainingMountain = Math.Max(0, mountainCount - summitCount);
        for (int i = 0; i < remainingMountain; i++)
        {
            types.Add("mountain");
        }

        for (int i = 0; i < hillyCount; i++)
        {
            types.Add("hilly");
        }

        while (types.Count < stageCount)
        {
            if (flatCount > 0 || types.Count < stageCount)
            {
                types.Add("flat");
            }
            else
            {
                types.Add("rolling");
            }
        }

        while (types.Count > stageCount)
        {
            types.RemoveAt(types.Count - 1);
        }

        Shuffle(types, rng);
        double totalKmTarget = constraints.TotalKmMin +
            rng.NextUnit() * (constraints.TotalKmMax - constraints.TotalKmMin);
        double perStage = totalKmTarget / stageCount;
        List<StagePlan> plans = new();
        for (int i = 0; i < stageCount; i++)
        {
            plans.Add(new StagePlan(i + 1, stageDays[i], types[i], perStage));
        }

        return plans;
    }

    private static List<GeneratedStageCourse> BuildRaceProfiles(
        RaceIdentityConstraints constraints,
        CalendarRaceDetail race,
        IReadOnlyList<StagePlan> plans,
        int seasonYear,
        long masterSeed,
        Func<WorldEntityId> allocateId,
        DeterministicRng rng)
    {
        List<GeneratedStageCourse> generated = new();
        double cobbleBudgetM = constraints.CobbleKmMin * 1000 +
            rng.NextUnit() * (constraints.CobbleKmMax - constraints.CobbleKmMin) * 1000;
        double cobblePerClassic = plans.Count == 0 ? 0 : cobbleBudgetM;

        foreach (StagePlan plan in plans)
        {
            ulong stageSeed = StableSeedDerivation.Derive(
                masterSeed,
                $"course-catalog:{seasonYear}:{race.Id}:{plan.StageIndex}");
            DeterministicRng stageRng = new(stageSeed);
            double targetM = plan.TargetKm * 1000;
            if (string.Equals(race.Id, "race.wt2026.roubaix", StringComparison.Ordinal))
            {
                targetM = constraints.TotalKmMin * 1000 +
                    stageRng.NextUnit() * (constraints.TotalKmMax - constraints.TotalKmMin) * 1000;
            }

            List<CourseSampleVertex> samples = BuildStageSamples(
                plan.StageType,
                targetM,
                cobblePerClassic,
                constraints,
                stageRng);
            samples = CourseBricks.SmoothJoins(samples);
            samples = FitSamplesToTargetLength(samples, targetM, stageRng);
            (double lengthM, double gainM, double lossM, double cobbleM, double gravelM, double maxGrad, double minGrad) =
                CourseMetrics.Compute(samples);
            CourseKind kind = plan.StageType switch
            {
                "itt" => CourseKind.IndividualTimeTrial,
                "ttt" => CourseKind.TeamTimeTrial,
                _ => CourseKind.Road,
            };
            ClassifiedStageType classified = CourseClassifier.Classify(kind, samples, lengthM, gainM, cobbleM);
            string originId = $"course.wt{seasonYear}.{race.Id.Replace("race.wt2026.", "", StringComparison.Ordinal)}.{seasonYear}.s{plan.StageIndex}";
            string title = plans.Count == 1
                ? race.Name
                : $"{race.Name} — Stage {plan.StageIndex}";
            CourseProfile profile = new(
                allocateId(),
                originId,
                race.Id,
                seasonYear,
                plan.StageIndex,
                title,
                kind,
                race.Country,
                CourseMetrics.SampleSpacingM,
                samples,
                lengthM,
                gainM,
                lossM,
                cobbleM,
                gravelM,
                maxGrad,
                minGrad,
                classified);
            generated.Add(new GeneratedStageCourse(profile, plan.DayNumber));
        }

        ValidateRace(constraints, generated.Select(item => item.Profile).ToArray());
        return generated;
    }

    private static List<CourseSampleVertex> BuildStageSamples(
        string stageType,
        double targetM,
        double cobbleBudgetM,
        RaceIdentityConstraints constraints,
        DeterministicRng rng)
    {
        double baseElev = 200 + rng.NextUnit() * 400;
        if (string.Equals(constraints.RaceContentId, "race.wt2026.roubaix", StringComparison.Ordinal))
        {
            return ComposeCobbleClassic(rng, targetM, cobbleBudgetM, baseElev);
        }

        if (string.Equals(constraints.RaceContentId, "race.wt2026.ronde", StringComparison.Ordinal))
        {
            return ComposeCobbleClassic(rng, targetM, Math.Min(cobbleBudgetM, 30000), baseElev);
        }

        if (string.Equals(constraints.RaceContentId, "race.wt2026.milano_sanremo", StringComparison.Ordinal))
        {
            return ComposeMsr(rng, targetM, baseElev);
        }

        if (string.Equals(constraints.RaceContentId, "race.wt2026.strade_bianche", StringComparison.Ordinal))
        {
            return ComposeStradeBianche(rng, targetM, baseElev);
        }

        return stageType switch
        {
            "itt" => CourseBricks.BuildIttOutAndBack(rng, targetM, baseElev),
            "ttt" => CourseBricks.BuildIttOutAndBack(rng, targetM * 0.9, baseElev),
            "summit" => CourseBricks.BuildSummitFinish(
                rng,
                targetM * 0.45,
                targetM * 0.55,
                0.075 + rng.NextUnit() * 0.02,
                baseElev,
                PickClimbShape(rng)),
            "mountain" => ComposeMountainStage(rng, targetM, baseElev),
            "hilly" => ComposeHillyStage(rng, targetM, baseElev),
            "flat" => CourseBricks.BuildFlatRoad(rng, targetM, baseElev),
            _ => CourseBricks.BuildRolling(rng, targetM, baseElev),
        };
    }

    private static List<CourseSampleVertex> ComposeMountainStage(DeterministicRng rng, double targetM, double baseElev)
    {
        double climbM = targetM * (0.35 + rng.NextUnit() * 0.15);
        double approachM = targetM - climbM;
        List<CourseSampleVertex> approach = CourseBricks.BuildRolling(rng, approachM * 0.6, baseElev);
        List<CourseSampleVertex> climb = CourseBricks.BuildClimb(
            rng,
            climbM,
            0.07 + rng.NextUnit() * 0.02,
            approach[^1].ElevationM,
            PickClimbShape(rng));
        List<CourseSampleVertex> finish = CourseBricks.BuildDescent(
            rng,
            approachM * 0.4,
            climb[^1].ElevationM,
            climbM * 0.06);
        return CourseBricks.Concatenate(
            CourseBricks.Concatenate(approach, climb, skipFirst: true),
            finish,
            skipFirst: true);
    }

    private static List<CourseSampleVertex> ComposeHillyStage(DeterministicRng rng, double targetM, double baseElev)
    {
        List<CourseSampleVertex> result = CourseBricks.BuildRolling(rng, targetM * 0.4, baseElev);
        for (int wall = 0; wall < 3; wall++)
        {
            double bergLen = 600 + rng.NextUnit() * 800;
            List<CourseSampleVertex> berg = CourseBricks.BuildBerg(
                rng,
                bergLen,
                0.08 + rng.NextUnit() * 0.03,
                result[^1].ElevationM);
            result = CourseBricks.Concatenate(result, berg, skipFirst: true);
            List<CourseSampleVertex> link = CourseBricks.BuildRolling(rng, 3000 + rng.NextUnit() * 2000, result[^1].ElevationM);
            result = CourseBricks.Concatenate(result, link, skipFirst: true);
        }

        double remaining = Math.Max(targetM - result[^1].DistanceM, 5000);
        List<CourseSampleVertex> tail = CourseBricks.BuildRolling(rng, remaining, result[^1].ElevationM);
        return CourseBricks.Concatenate(result, tail, skipFirst: true);
    }

    private static List<CourseSampleVertex> ComposeCobbleClassic(
        DeterministicRng rng,
        double targetM,
        double cobbleBudgetM,
        double baseElev)
    {
        double cobbleTarget = Math.Max(cobbleBudgetM, targetM * 0.20);
        List<CourseSampleVertex> result = CourseBricks.BuildFlatRoad(rng, targetM * 0.15, baseElev);
        int sectors = 14;
        double cobblePer = cobbleTarget / sectors;
        for (int i = 0; i < sectors; i++)
        {
            List<CourseSampleVertex> link = CourseBricks.BuildRolling(rng, 2500 + rng.NextUnit() * 2000, result[^1].ElevationM);
            result = CourseBricks.Concatenate(result, link, skipFirst: true);
            List<CourseSampleVertex> cobbles = CourseBricks.BuildCobbleSector(rng, cobblePer, result[^1].ElevationM);
            result = CourseBricks.Concatenate(result, cobbles, skipFirst: true);
            if (i % 3 == 0)
            {
                List<CourseSampleVertex> berg = CourseBricks.BuildBerg(rng, 800 + rng.NextUnit() * 600, 0.09, result[^1].ElevationM);
                result = CourseBricks.Concatenate(result, berg, skipFirst: true);
            }
        }

        double current = result[^1].DistanceM;
        if (current < targetM)
        {
            List<CourseSampleVertex> finish = CourseBricks.BuildFlatRoad(rng, targetM - current, result[^1].ElevationM);
            result = CourseBricks.Concatenate(result, finish, skipFirst: true);
        }

        return result;
    }

    private static List<CourseSampleVertex> ComposeMsr(DeterministicRng rng, double targetM, double baseElev)
    {
        List<CourseSampleVertex> route = CourseBricks.BuildCoastalExposed(rng, targetM * 0.86, baseElev);
        List<CourseSampleVertex> cipressa = CourseBricks.BuildClimb(rng, 2400, 0.085, route[^1].ElevationM, "cipressa");
        route = CourseBricks.Concatenate(route, cipressa, skipFirst: true);
        List<CourseSampleVertex> link = CourseBricks.BuildRolling(rng, 5000, route[^1].ElevationM);
        route = CourseBricks.Concatenate(route, link, skipFirst: true);
        List<CourseSampleVertex> poggio = CourseBricks.BuildClimb(rng, 3700, 0.065, route[^1].ElevationM, "poggio");
        route = CourseBricks.Concatenate(route, poggio, skipFirst: true);
        double remaining = targetM - route[^1].DistanceM;
        if (remaining > 1000)
        {
            List<CourseSampleVertex> pad = CourseBricks.BuildFlatRoad(rng, remaining, route[^1].ElevationM);
            route = CourseBricks.Concatenate(route, pad, skipFirst: true);
        }

        return route;
    }

    private static List<CourseSampleVertex> ComposeStradeBianche(DeterministicRng rng, double targetM, double baseElev)
    {
        List<CourseSampleVertex> route = CourseBricks.BuildRolling(rng, targetM * 0.25, baseElev);
        int sectors = 8;
        double whiteKm = 55 + rng.NextUnit() * 15;
        double perSector = whiteKm * 1000 / sectors;
        for (int i = 0; i < sectors; i++)
        {
            List<CourseSampleVertex> white = CourseBricks.BuildWhiteRoad(rng, perSector, route[^1].ElevationM);
            route = CourseBricks.Concatenate(route, white, skipFirst: true);
            List<CourseSampleVertex> link = CourseBricks.BuildRolling(rng, 2500, route[^1].ElevationM);
            route = CourseBricks.Concatenate(route, link, skipFirst: true);
        }

        double remaining = Math.Max(targetM - route[^1].DistanceM, 5000);
        List<CourseSampleVertex> finish = CourseBricks.BuildRolling(rng, remaining, route[^1].ElevationM);
        return CourseBricks.Concatenate(route, finish, skipFirst: true);
    }

    private static List<CourseSampleVertex> FitSamplesToTargetLength(
        List<CourseSampleVertex> samples,
        double targetM,
        DeterministicRng rng)
    {
        double current = samples[^1].DistanceM;
        if (current < targetM * 0.95)
        {
            List<CourseSampleVertex> pad = CourseBricks.BuildFlatRoad(
                rng,
                targetM - current,
                samples[^1].ElevationM);
            return CourseBricks.Concatenate(samples, pad, skipFirst: true);
        }

        if (current > targetM * 1.05)
        {
            int keep = Math.Max(2, (int)Math.Round(targetM / CourseMetrics.SampleSpacingM) + 1);
            return samples.Take(keep).Select((vertex, index) => vertex with
            {
                DistanceM = index * CourseMetrics.SampleSpacingM,
            }).ToList();
        }

        return samples;
    }

    private static void ValidateRace(RaceIdentityConstraints constraints, IReadOnlyList<CourseProfile> profiles)
    {
        if (profiles.Count != constraints.RacingStageCount)
        {
            throw new InvalidOperationException(
                $"Race '{constraints.RaceContentId}' stage count {profiles.Count} != {constraints.RacingStageCount}.");
        }

        double totalKm = profiles.Sum(profile => profile.LengthM) / 1000.0;
        if (totalKm < constraints.TotalKmMin - 5 || totalKm > constraints.TotalKmMax + 10)
        {
            throw new InvalidOperationException(
                $"Race '{constraints.RaceContentId}' total km {totalKm:F0} outside band.");
        }

        foreach (CourseProfile profile in profiles)
        {
            int minVertices = (int)(profile.LengthM / CourseMetrics.SampleSpacingM);
            if (profile.Samples.Count < Math.Max(50, minVertices - 2))
            {
                throw new InvalidOperationException(
                    $"Race '{constraints.RaceContentId}' stage {profile.StageIndex} too few samples.");
            }
        }
    }

    private static List<int> DistributeStageDays(
        int startDay,
        int endDay,
        int stageCount,
        int restDays,
        DeterministicRng rng)
    {
        int span = endDay - startDay + 1;
        List<int> days = new();
        if (stageCount <= 0)
        {
            return days;
        }

        if (stageCount == 1)
        {
            days.Add(startDay);
            return days;
        }

        int blocks = stageCount;
        int[] blockSizes = new int[blocks];
        int baseSize = span / blocks;
        int remainder = span % blocks;
        for (int i = 0; i < blocks; i++)
        {
            blockSizes[i] = baseSize + (i < remainder ? 1 : 0);
        }

        int cursor = startDay;
        for (int block = 0; block < blocks; block++)
        {
            int blockEnd = cursor + blockSizes[block] - 1;
            if (block == 0)
            {
                days.Add(cursor);
            }
            else if (block == blocks - 1)
            {
                days.Add(blockEnd);
            }
            else
            {
                days.Add(cursor + blockSizes[block] / 2);
            }

            cursor = blockEnd + 1;
        }

        return days.Take(stageCount).OrderBy(day => day).ToList();
    }

    private static int PickCount(DeterministicRng rng, int min, int max)
    {
        if (max <= min)
        {
            return min;
        }

        return min + (int)(rng.NextUnit() * (max - min + 1));
    }

    private static void Shuffle<T>(IList<T> list, DeterministicRng rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = (int)(rng.NextUInt64() % (ulong)(i + 1));
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static string PickClimbShape(DeterministicRng rng)
    {
        string[] shapes = { "alpe", "pyrenean", "wall", "generic" };
        return shapes[(int)(rng.NextUInt64() % (ulong)shapes.Length)];
    }
}
