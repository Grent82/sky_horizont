using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Domain.Social
{
    public enum IntentType { Court, Quarrel, Gift, Recruit, Bribe, Spy, Defect, Assassinate, Negotiate, VisitFamily }
    
    /// <summary> Planned monthly action for one actor. </summary>
    public record CharacterIntent(Guid ActorId, IntentType Type, Guid? TargetCharacterId = null, Guid? TargetFactionId = null);
}
