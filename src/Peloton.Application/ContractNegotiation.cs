using System;
using Peloton.Domain;

namespace Peloton.Application;

public sealed record ContractNegotiationProjection(
    WorldEntityId RiderCareerId,
    string RiderName,
    WorldEntityId? CurrentClubId,
    string? CurrentClubName,
    int CurrentWage,
    int? OfferAnnualWage,
    int? OfferContractEndDay,
    bool OfferSet);

internal sealed record ContractNegotiationDraft(
    WorldEntityId RiderCareerId,
    int? OfferAnnualWage,
    int? OfferContractEndDay)
{
    public bool OfferSet => OfferAnnualWage is not null && OfferContractEndDay is not null;
}

public static class ContractNegotiationQueries
{
    public static int ComputeAcceptThreshold(int currentWage, double loyalty01)
    {
        if (currentWage == 0)
        {
            return 100_000;
        }

        return (int)Math.Floor(currentWage * (1.10 - (0.20 * loyalty01)));
    }

    public static bool WouldAcceptOffer(
        int currentWage,
        double loyalty01,
        int offerWage,
        int offerEndDay,
        int currentDayNumber)
    {
        if (offerWage <= 0 || offerEndDay <= currentDayNumber)
        {
            return false;
        }

        int threshold = ComputeAcceptThreshold(currentWage, loyalty01);
        return offerWage >= threshold;
    }
}
