using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Domain.Social
{
    public enum IntentType { Court, Quarrel, Gift, Recruit, Bribe, Spy, Defect, Assassinate, Negotiate, VisitFamily }
    public record CharacterIntent(Guid ActorId, IntentType Type, Guid? TargetId = null, Guid? ContextId = null);

    public interface IIntentPlanner
    {
        IEnumerable<CharacterIntent> PlanMonthlyIntents(Character actor);
    }
}
