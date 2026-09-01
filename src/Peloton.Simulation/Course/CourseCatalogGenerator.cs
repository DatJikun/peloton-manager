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

        List<string> types = stageCount == 1
            ? new List<string> { PickOneDayStageType(constraints, rng) }
            : BuildMultiStageTypeList(constraints, stageCount, rng);

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

    private static string PickOneDayStageType(RaceIdentityConstraints constraints, DeterministicRng rng)
    {
        if (constraints.FlatMin >= 1 && constraints.FlatMax > 0)
        {
            return "flat";
        }

        List<string> allowed = BuildAllowedStageTypes(constraints);
        if (allowed.Count == 0)
        {
            return "flat";
        }

        return WeightedPick(allowed, constraints.TerrainPalette, rng);
    }

    private static List<string> BuildMultiStageTypeList(
        RaceIdentityConstraints constraints,
        int stageCount,
        DeterministicRng rng)
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal)
        {
            ["itt"] = 0,
            ["ttt"] = 0,
            ["summit"] = 0,
            ["mountain"] = 0,
            ["hilly"] = 0,
            ["flat"] = 0,
            ["rolling"] = 0,
        };

        void AddMinimum(string type, int min)
        {
            for (int i = 0; i < min; i++)
            {
                counts[type]++;
            }
        }

        int summitCount = PickCount(rng, constraints.SummitFinishMin, constraints.SummitFinishMax);
        AddMinimum("summit", summitCount);
        int mountainMin = constraints.MountainMin;
        int mountainExtras = Math.Max(0, mountainMin - counts["summit"]);
        AddMinimum("mountain", mountainExtras);
        AddMinimum("itt", constraints.IttMin);
        AddMinimum("ttt", constraints.TttMin);
        AddMinimum("hilly", constraints.HillyMin);
        AddMinimum("flat", constraints.FlatMin);

        int total = counts.Values.Sum();
        if (total > stageCount)
        {
            ReduceExtrasAboveMinimum(counts, stageCount, constraints);
        }

        while (counts.Values.Sum() < stageCount)
        {
            List<string> candidates = BuildFillCandidates(constraints, counts);
            if (candidates.Count == 0)
            {
                counts["rolling"]++;
                continue;
            }

            string picked = WeightedPick(candidates, constraints.TerrainPalette, rng);
            counts[picked]++;
        }

        List<string> types = new(stageCount);
        foreach (KeyValuePair<string, int> pair in counts.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            for (int i = 0; i < pair.Value; i++)
            {
                types.Add(pair.Key);
            }
        }

        while (types.Count > stageCount)
        {
            types.RemoveAt(types.Count - 1);
        }

        return types;
    }

    private static void ReduceExtrasAboveMinimum(
        Dictionary<string, int> counts,
        int stageCount,
        RaceIdentityConstraints constraints)
    {
        Dictionary<string, int> minimums = new(StringComparer.Ordinal)
        {
            ["itt"] = constraints.IttMin,
            ["ttt"] = constraints.TttMin,
            ["summit"] = constraints.SummitFinishMin,
            ["mountain"] = Math.Max(0, constraints.MountainMin - counts["summit"]),
            ["hilly"] = constraints.HillyMin,
            ["flat"] = constraints.FlatMin,
            ["rolling"] = 0,
        };

        string[] reductionOrder = { "rolling", "hilly", "mountain", "flat", "summit", "itt", "ttt" };
        while (counts.Values.Sum() > stageCount)
        {
            bool removed = false;
            foreach (string type in reductionOrder)
            {
                int floor = minimums[type];
                if (counts[type] > floor)
                {
                    counts[type]--;
                    removed = true;
                    break;
                }
            }

            if (!removed)
            {
                break;
            }
        }
    }

    private static List<string> BuildAllowedStageTypes(RaceIdentityConstraints constraints)
    {
        List<string> allowed = new();
        if (constraints.IttMax > 0)
        {
            allowed.Add("itt");
        }

        if (constraints.TttMax > 0)
        {
            allowed.Add("ttt");
        }

        if (constraints.SummitFinishMax > 0)
        {
            allowed.Add("summit");
        }

        if (constraints.MountainMax > 0)
        {
            allowed.Add("mountain");
        }

        if (constraints.HillyMax > 0)
        {
            allowed.Add("hilly");
        }

        if (constraints.FlatMax > 0)
        {
            allowed.Add("flat");
        }

        if (allowed.Count == 0)
        {
            allowed.Add("rolling");
        }

        return allowed;
    }

    private static List<string> BuildFillCandidates(
        RaceIdentityConstraints constraints,
        Dictionary<string, int> counts)
    {
        List<string> candidates = new();
        TryAddCandidate(candidates, "itt", counts["itt"], constraints.IttMax);
        TryAddCandidate(candidates, "ttt", counts["ttt"], constraints.TttMax);
        TryAddCandidate(candidates, "summit", counts["summit"], constraints.SummitFinishMax);
        TryAddCandidate(candidates, "mountain", counts["mountain"] + counts["summit"], constraints.MountainMax);
        TryAddCandidate(candidates, "hilly", counts["hilly"], constraints.HillyMax);
        TryAddCandidate(candidates, "flat", counts["flat"], constraints.FlatMax);
        if (candidates.Count == 0)
        {
            candidates.Add("rolling");
        }

        return candidates;
    }

    private static void TryAddCandidate(List<string> candidates, string type, int current, int max)
    {
        if (max <= 0)
        {
            return;
        }

        if (current < max)
        {
            candidates.Add(type);
        }
    }

    private static string WeightedPick(
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> terrainPalette,
        DeterministicRng rng)
    {
        double[] weights = new double[candidates.Count];
        double total = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            weights[i] = PaletteWeight(candidates[i], terrainPalette);
            total += weights[i];
        }

        if (total <= 0)
        {
            return candidates[(int)(rng.NextUInt64() % (ulong)candidates.Count)];
        }

        double roll = rng.NextUnit() * total;
        double cursor = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            cursor += weights[i];
            if (roll <= cursor)
            {
                return candidates[i];
            }
        }

        return candidates[^1];
    }

    private static double PaletteWeight(string stageType, IReadOnlyList<string> terrainPalette)
    {
        double weight = 0.2;
        foreach (string palette in terrainPalette)
        {
            if (PaletteMatchesStageType(palette, stageType))
            {
                weight += 1.0;
            }
        }

        return weight;
    }

    private static bool PaletteMatchesStageType(string palette, string stageType) =>
        palette switch
        {
            "flat" => stageType is "flat" or "rolling",
            "rolling" => stageType is "flat" or "hilly" or "rolling",
            "hilly" or "valley" => stageType is "hilly" or "rolling",
            "climb" => stageType is "mountain" or "summit",
            "summit" => stageType is "summit" or "mountain",
            "cobble" => stageType is "flat" or "hilly" or "rolling",
            _ => string.Equals(palette, stageType, StringComparison.Ordinal),
        };

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
            samples = FitSamplesToTargetLength(samples, targetM, stageRng, plan.StageType);
            if (!IsMonumentComposerRace(constraints.RaceContentId))
            {
                samples = EnsurePlannedClassification(samples, plan.StageType, stageRng);
            }
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
            "summit" => CapElevationGain(
                CourseBricks.BuildSummitFinish(
                    rng,
                    targetM * 0.45,
                    targetM * 0.55,
                    0.065 + rng.NextUnit() * 0.015,
                    baseElev,
                    PickClimbShape(rng)),
                targetM <= 120_000 ? 5500 : 5000),
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
            0.06 + rng.NextUnit() * 0.015,
            approach[^1].ElevationM,
            PickClimbShape(rng));
        List<CourseSampleVertex> finish = CourseBricks.BuildDescent(
            rng,
            approachM * 0.4,
            climb[^1].ElevationM,
            climbM * 0.05);
        List<CourseSampleVertex> combined = CourseBricks.Concatenate(
            CourseBricks.Concatenate(approach, climb, skipFirst: true),
            finish,
            skipFirst: true);
        double maxGain = targetM <= 120_000 ? 5500 : 5000;
        return CapElevationGain(combined, maxGain);
    }

    private static List<CourseSampleVertex> ComposeHillyStage(DeterministicRng rng, double targetM, double baseElev)
    {
        List<CourseSampleVertex> result = CourseBricks.BuildRolling(rng, targetM * 0.4, baseElev);
        int walls = 2 + (int)(rng.NextUnit() * 2);
        for (int wall = 0; wall < walls; wall++)
        {
            double bergLen = 500 + rng.NextUnit() * 600;
            List<CourseSampleVertex> berg = CourseBricks.BuildBerg(
                rng,
                bergLen,
                0.06 + rng.NextUnit() * 0.02,
                result[^1].ElevationM);
            result = CourseBricks.Concatenate(result, berg, skipFirst: true);
            List<CourseSampleVertex> link = CourseBricks.BuildRolling(rng, 2500 + rng.NextUnit() * 1500, result[^1].ElevationM);
            result = CourseBricks.Concatenate(result, link, skipFirst: true);
        }

        double remaining = Math.Max(targetM - result[^1].DistanceM, 5000);
        List<CourseSampleVertex> tail = CourseBricks.BuildRolling(rng, remaining, result[^1].ElevationM);
        result = CourseBricks.Concatenate(result, tail, skipFirst: true);
        return CapElevationGain(result, 2200);
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
        DeterministicRng rng,
        string stageType)
    {
        double current = samples[^1].DistanceM;
        if (current < targetM * 0.95)
        {
            List<CourseSampleVertex> pad = string.Equals(stageType, "flat", StringComparison.Ordinal)
                ? CourseBricks.BuildFlatRoad(rng, targetM - current, samples[^1].ElevationM)
                : CourseBricks.BuildRolling(rng, targetM - current, samples[^1].ElevationM);
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

    private static bool IsMonumentComposerRace(string raceContentId) =>
        string.Equals(raceContentId, "race.wt2026.roubaix", StringComparison.Ordinal) ||
        string.Equals(raceContentId, "race.wt2026.ronde", StringComparison.Ordinal) ||
        string.Equals(raceContentId, "race.wt2026.milano_sanremo", StringComparison.Ordinal) ||
        string.Equals(raceContentId, "race.wt2026.strade_bianche", StringComparison.Ordinal);

    private static List<CourseSampleVertex> EnsurePlannedClassification(
        List<CourseSampleVertex> samples,
        string stageType,
        DeterministicRng rng)
    {
        if (!string.Equals(stageType, "flat", StringComparison.Ordinal))
        {
            return samples;
        }

        (double lengthM, double gainM, _, double cobbleM, _, _, _) = CourseMetrics.Compute(samples);
        ClassifiedStageType classified = CourseClassifier.Classify(
            CourseKind.Road,
            samples,
            lengthM,
            gainM,
            cobbleM);
        if (classified is ClassifiedStageType.Flat or ClassifiedStageType.Mixed && gainM < 1000)
        {
            return samples;
        }

        return CourseBricks.BuildFlatRoad(rng, lengthM, samples[0].ElevationM);
    }

    private static List<CourseSampleVertex> CapElevationGain(List<CourseSampleVertex> samples, double maxGainM)
    {
        (double _, double gainM, _, _, _, _, _) = CourseMetrics.Compute(samples);
        if (gainM <= maxGainM)
        {
            return samples;
        }

        double scale = maxGainM / gainM;
        double startElev = samples[0].ElevationM;
        List<CourseSampleVertex> capped = new(samples.Count);
        for (int i = 0; i < samples.Count; i++)
        {
            CourseSampleVertex vertex = samples[i];
            double relative = vertex.ElevationM - startElev;
            capped.Add(vertex with { ElevationM = startElev + relative * scale });
        }

        return capped;
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
