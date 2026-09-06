using System;
using System.Globalization;
using Peloton.Domain;

namespace Peloton.Application;

public enum RiderStyleArchetype
{
    Climber,
    Sprinter,
    Classics,
    TimeTrial,
    Puncheur,
    Domestique,
}

public sealed record RiderStyleScore(
    RiderStyleArchetype Style,
    string StyleLabel,
    int Score,
    double Stars,
    string StarsDisplay);

public sealed record RiderStarsProfile(
    RiderStyleArchetype PrimaryStyle,
    string PrimaryStyleLabel,
    double PrimaryStars,
    string PrimaryStarsDisplay,
    RiderStyleScore Climber,
    RiderStyleScore Sprinter,
    RiderStyleScore Classics,
    RiderStyleScore TimeTrial,
    RiderStyleScore Domestique);

public static class RiderStyleRatingQueries
{
    public static double RatingToStars(int rating) => rating switch
    {
        >= 88 => 5.0,
        >= 83 => 4.5,
        >= 78 => 4.0,
        >= 73 => 3.5,
        >= 68 => 3.0,
        >= 63 => 2.5,
        >= 58 => 2.0,
        >= 52 => 1.5,
        _ => 1.0,
    };

    public static string FormatStars(double stars)
    {
        int full = (int)Math.Floor(stars);
        bool half = stars - full >= 0.4;
        int empty = 5 - full - (half ? 1 : 0);

        string s = new string('★', full);
        if (half)
        {
            s += "½";
        }

        if (empty > 0)
        {
            s += new string('☆', empty);
        }

        return s;
    }

    public static string FormatFogBandStars(double minStars, double maxStars)
    {
        string minStr = minStars.ToString("0.0", CultureInfo.InvariantCulture);
        string maxStr = maxStars.ToString("0.0", CultureInfo.InvariantCulture);
        return $"{minStr}-{maxStr} ★";
    }

    public static RiderStarsProfile ComputeProfile(RiderRatingSet ratings, string originDefinitionId)
    {
        ArgumentNullException.ThrowIfNull(ratings);

        int climbScore = Math.Clamp((int)Math.Round(0.70 * ratings.Climb + 0.20 * ratings.Hills + 0.10 * ratings.Ovr), 1, 99);
        int sprintScore = Math.Clamp((int)Math.Round(0.75 * ratings.Sprint + 0.15 * ratings.Flat + 0.10 * ratings.Ovr), 1, 99);
        int classicsScore = Math.Clamp((int)Math.Round(0.50 * ratings.Cobbles + 0.35 * ratings.Hills + 0.15 * ratings.Flat), 1, 99);
        int ttScore = Math.Clamp((int)Math.Round(0.80 * ratings.TimeTrial + 0.20 * ratings.Flat), 1, 99);
        int domestiqueScore = Math.Clamp((int)Math.Round(0.40 * ratings.Flat + 0.30 * ratings.Ovr + 0.30 * ratings.Hills), 1, 99);

        double climbStars = RatingToStars(climbScore);
        double sprintStars = RatingToStars(sprintScore);
        double classicsStars = RatingToStars(classicsScore);
        double ttStars = RatingToStars(ttScore);
        double domestiqueStars = RatingToStars(domestiqueScore);

        RiderStyleScore climber = new(RiderStyleArchetype.Climber, "GÓRY", climbScore, climbStars, FormatStars(climbStars));
        RiderStyleScore sprinter = new(RiderStyleArchetype.Sprinter, "SPRINT", sprintScore, sprintStars, FormatStars(sprintStars));
        RiderStyleScore classics = new(RiderStyleArchetype.Classics, "KLASYKI", classicsScore, classicsStars, FormatStars(classicsStars));
        RiderStyleScore tt = new(RiderStyleArchetype.TimeTrial, "CZASOWIEC", ttScore, ttStars, FormatStars(ttStars));
        RiderStyleScore domestique = new(RiderStyleArchetype.Domestique, "POMOCNIK", domestiqueScore, domestiqueStars, FormatStars(domestiqueStars));

        RiderStyleScore highestSpec = (climbScore >= sprintScore && climbScore >= classicsScore && climbScore >= ttScore) ? climber :
             (sprintScore >= classicsScore && sprintScore >= ttScore) ? sprinter :
             (classicsScore >= ttScore) ? classics : tt;

        string role = RiderMetadataCatalog.ResolveArchetype(originDefinitionId).ToLowerInvariant();
        RiderStyleScore primary = role switch
        {
            "sprinter" => sprinter,
            "classics" => classics,
            "tt" => tt,
            "gc" or "super-gc" or "climber" => climber,
            "domestique" when highestSpec.Score >= 75 => highestSpec,
            "domestique" => domestique,
            _ => highestSpec,
        };

        return new RiderStarsProfile(
            primary.Style,
            primary.StyleLabel,
            primary.Stars,
            primary.StarsDisplay,
            climber,
            sprinter,
            classics,
            tt,
            domestique);
    }
}
