namespace SkyHorizont.Domain.Social
{
    public enum IntentType
    {
        Court, Quarrel, Gift, Recruit, Bribe, Spy, Defect, Assassinate, Negotiate,
        VisitFamily, VisitLover, TorturePrisoner, RapePrisoner,
        TravelToPlanet, BecomePirate, RaidConvoy
    }

    /// <summary> Planned monthly action for one actor. </summary>
    public record CharacterIntent(
        Guid ActorId,
        IntentType Type,
        Guid? TargetCharacterId = null,
        Guid? TargetFactionId = null,
        Guid? TargetPlanetId = null
    );
}