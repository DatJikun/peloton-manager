namespace Peloton.Application;

public sealed record NewGameClubProjection(
    string OriginId,
    string Name,
    string Country,
    string TitleSponsor,
    string Division);
