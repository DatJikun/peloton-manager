using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Domain;

public enum CourseSurface
{
    Asphalt,
    Cobble,
    Gravel,
    WhiteRoad,
}

public enum CourseKind
{
    Road,
    IndividualTimeTrial,
    TeamTimeTrial,
}

public enum ClassifiedStageType
{
    IndividualTimeTrial,
    TeamTimeTrial,
    Flat,
    Hilly,
    Mountain,
    MountainSummit,
    CobbleClassic,
    Mixed,
}

public sealed record CourseSampleVertex(
    double DistanceM,
    double ElevationM,
    double WidthM,
    double HeadingDegrees,
    CourseSurface Surface,
    double Curvature01,
    double Exposure01);

public sealed class CourseProfile
{
    public CourseProfile(
        WorldEntityId courseProfileId,
        string originDefinitionId,
        string raceContentId,
        int seasonYear,
        int stageIndex,
        string name,
        CourseKind kind,
        string country,
        double sampleSpacingM,
        IReadOnlyList<CourseSampleVertex> samples,
        double lengthM,
        double elevationGainM,
        double elevationLossM,
        double cobbleM,
        double gravelM,
        double maxGradient,
        double minGradient,
        ClassifiedStageType classifiedStageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originDefinitionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(raceContentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleSpacingM);
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count < 2)
        {
            throw new ArgumentException("Course profile requires at least two samples.", nameof(samples));
        }

        CourseProfileId = courseProfileId;
        OriginDefinitionId = originDefinitionId;
        RaceContentId = raceContentId;
        SeasonYear = seasonYear;
        StageIndex = stageIndex;
        Name = name;
        Kind = kind;
        Country = country;
        SampleSpacingM = sampleSpacingM;
        Samples = samples.ToArray();
        LengthM = lengthM;
        ElevationGainM = elevationGainM;
        ElevationLossM = elevationLossM;
        CobbleM = cobbleM;
        GravelM = gravelM;
        MaxGradient = maxGradient;
        MinGradient = minGradient;
        ClassifiedStageType = classifiedStageType;
    }

    public WorldEntityId CourseProfileId { get; }

    public string OriginDefinitionId { get; }

    public string RaceContentId { get; }

    public int SeasonYear { get; }

    public int StageIndex { get; }

    public string Name { get; }

    public CourseKind Kind { get; }

    public string Country { get; }

    public double SampleSpacingM { get; }

    public IReadOnlyList<CourseSampleVertex> Samples { get; }

    public double LengthM { get; }

    public double ElevationGainM { get; }

    public double ElevationLossM { get; }

    public double CobbleM { get; }

    public double GravelM { get; }

    public double MaxGradient { get; }

    public double MinGradient { get; }

    public ClassifiedStageType ClassifiedStageType { get; }
}

public sealed record RiderStageTime(
    string RaceContentId,
    int StageIndex,
    WorldEntityId RiderId,
    double FinishTimeSeconds);
