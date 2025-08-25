namespace SkyHorizont.Domain.Social;

public sealed record AmbitionBias
{
    public double Court { get; init; } = 1.0;
    public double Quarrel { get; init; } = 1.0;
    public double Gift { get; init; } = 1.0;
    public double Recruit { get; init; } = 1.0;
    public double Bribe { get; init; } = 1.0;
    public double Spy { get; init; } = 1.0;
    public double Defect { get; init; } = 1.0;
    public double Assassinate { get; init; } = 1.0;
    public double Negotiate { get; init; } = 1.0;
    public double VisitFamily { get; init; } = 1.0;
    public double VisitLover { get; init; } = 1.0;
    public double TorturePrisoner { get; init; } = 1.0;
    public double RapePrisoner { get; init; } = 1.0;
    public double TravelToPlanet { get; init; } = 1.0;
    public double BecomePirate { get; init; } = 1.0;
    public double RaidConvoy { get; init; } = 1.0;
    public double FoundHouse { get; init; } = 1.0;
    public double FoundPirateClan { get; init; } = 1.0;
    public double ExpelFromHouse { get; init; } = 1.0;
    public double ClaimPlanetSeat { get; init; } = 1.0;
    public double BuildFleet { get; init; } = 1.0;

    public double this[IntentType intent] => intent switch
    {
        IntentType.Court => Court,
        IntentType.Quarrel => Quarrel,
        IntentType.Gift => Gift,
        IntentType.Recruit => Recruit,
        IntentType.Bribe => Bribe,
        IntentType.Spy => Spy,
        IntentType.Defect => Defect,
        IntentType.Assassinate => Assassinate,
        IntentType.Negotiate => Negotiate,
        IntentType.VisitFamily => VisitFamily,
        IntentType.VisitLover => VisitLover,
        IntentType.TorturePrisoner => TorturePrisoner,
        IntentType.RapePrisoner => RapePrisoner,
        IntentType.TravelToPlanet => TravelToPlanet,
        IntentType.BecomePirate => BecomePirate,
        IntentType.RaidConvoy => RaidConvoy,
        IntentType.FoundHouse => FoundHouse,
        IntentType.FoundPirateClan => FoundPirateClan,
        IntentType.ExpelFromHouse => ExpelFromHouse,
        IntentType.ClaimPlanetSeat => ClaimPlanetSeat,
        IntentType.BuildFleet => BuildFleet,
        _ => 1.0
    };
}
