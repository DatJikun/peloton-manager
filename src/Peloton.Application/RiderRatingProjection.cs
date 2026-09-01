using System;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record ExactRiderRating(
    int Climb,
    int Hills,
    int Flat,
    int TimeTrial,
    int Sprint,
    int Cobbles,
    int Ovr,
    int PotentialOvr);

public sealed record GuessedRiderRating(
    int ClimbMin,
    int ClimbMax,
    int HillsMin,
    int HillsMax,
    int FlatMin,
    int FlatMax,
    int TimeTrialMin,
    int TimeTrialMax,
    int SprintMin,
    int SprintMax,
    int CobblesMin,
    int CobblesMax,
    int OvrMin,
    int OvrMax,
    int PotentialOvrMin,
    int PotentialOvrMax);

public sealed record RiderRatingProjection(
    WorldEntityId RiderCareerId,
    string Name,
    ExactRiderRating? Exact,
    GuessedRiderRating? Guessed);

public static class RiderRatingProjectionQueries
{
    public static RiderRatingProjection Project(
        RiderCareer career,
        Person person,
        int potentialOvr,
        string attributeVisibility,
        WorldEntityId? viewerOrganizationId)
    {
        RiderRatingSet ratings = RiderRatingQueries.FromPhysiology(career, potentialOvr);
        ExactRiderRating exact = ToExact(ratings);
        bool isOwnClub = viewerOrganizationId is not null &&
                         career.OrganizationId == viewerOrganizationId;
        string visibility = attributeVisibility ?? "Guessed";

        if (isOwnClub || string.Equals(visibility, "All", StringComparison.OrdinalIgnoreCase))
        {
            return new RiderRatingProjection(career.Id, person.Name, exact, null);
        }

        if (string.Equals(visibility, "None", StringComparison.OrdinalIgnoreCase))
        {
            return new RiderRatingProjection(career.Id, person.Name, null, null);
        }

        return new RiderRatingProjection(
            career.Id,
            person.Name,
            null,
            ToGuessed(exact));
    }

    private static ExactRiderRating ToExact(RiderRatingSet ratings) =>
        new(
            ratings.Climb,
            ratings.Hills,
            ratings.Flat,
            ratings.TimeTrial,
            ratings.Sprint,
            ratings.Cobbles,
            ratings.Ovr,
            ratings.PotentialOvr);

    private static GuessedRiderRating ToGuessed(ExactRiderRating exact)
    {
        static (int Min, int Max) Band(int value) =>
            (Math.Clamp(value - 4, 1, 99), Math.Clamp(value + 4, 1, 99));

        (int climbMin, int climbMax) = Band(exact.Climb);
        (int hillsMin, int hillsMax) = Band(exact.Hills);
        (int flatMin, int flatMax) = Band(exact.Flat);
        (int ttMin, int ttMax) = Band(exact.TimeTrial);
        (int sprintMin, int sprintMax) = Band(exact.Sprint);
        (int cobblesMin, int cobblesMax) = Band(exact.Cobbles);
        (int ovrMin, int ovrMax) = Band(exact.Ovr);
        (int potMin, int potMax) = Band(exact.PotentialOvr);

        return new GuessedRiderRating(
            climbMin,
            climbMax,
            hillsMin,
            hillsMax,
            flatMin,
            flatMax,
            ttMin,
            ttMax,
            sprintMin,
            sprintMax,
            cobblesMin,
            cobblesMax,
            ovrMin,
            ovrMax,
            potMin,
            potMax);
    }
}
