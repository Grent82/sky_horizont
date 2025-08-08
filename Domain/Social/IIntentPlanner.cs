using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Domain.Social
{
    public interface IIntentPlanner
    {
        IEnumerable<CharacterIntent> PlanMonthlyIntents(Character actor);
    }
}
