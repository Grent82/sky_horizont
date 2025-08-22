namespace SkyHorizont.Domain.Social
{
    public enum SocialEventType
    {
        CourtshipAttempt,
        FamilyVisit,
        LoverVisit,
        EspionageOperation,
        BriberyAttempt,
        RecruitmentAttempt,
        DefectionAttempt,
        Negotiation,
        Quarrel,
        AssassinationAttempt,
        TortureAttempt,
        RapeAttempt,
        TravelBooked,
        PirateDefection,
        RaidPlanned,
        FoundGreatHouse,
        FoundPirateClan,
        ClaimPlanet,
        ExpelFromHouse
    }

    public interface ISocialEvent
    {
        Guid EventId { get; }
        int Year { get; }
        int Month { get; }
        SocialEventType Type { get; }
        Guid ActorId { get; }
        Guid? TargetCharacterId { get; }
        Guid? TargetFactionId { get; }
        Guid? TargetPlanetId { get; }
        bool Success { get; }
        int DeltaOpinionActorToTarget { get; }
        int DeltaOpinionTargetToActor { get; }
        IReadOnlyList<Guid> SecretIds { get; }
        string Notes { get; }
    }

    public sealed record SocialEvent(
        Guid EventId,
        int Year,
        int Month,
        SocialEventType Type,
        Guid ActorId,
        Guid? TargetCharacterId,
        Guid? TargetFactionId,
        Guid? TargetPlanetId,
        bool Success,
        int DeltaOpinionActorToTarget,
        int DeltaOpinionTargetToActor,
        IReadOnlyList<Guid> SecretIds,
        string Notes
    ) : ISocialEvent;

    public interface ISocialEventLog
    {
        void Append(ISocialEvent ev);
    }
}
