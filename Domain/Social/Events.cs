namespace SkyHorizont.Domain.Social
{
    public enum SocialEventType
    {
        CourtshipAttempt,
        FamilyVisit,
        EspionageOperation,
        BriberyAttempt,
        RecruitmentAttempt,
        DefectionAttempt,
        Negotiation,
        Quarrel,
        AssassinationAttempt
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
        bool Success { get; }

        // Opinion/affection deltas for convenience (apply already done by resolver)
        int DeltaOpinionActorToTarget { get; }
        int DeltaOpinionTargetToActor { get; }

        // Any new secrets uncovered or created by this event
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
