using SkyHorizont.Domain.Services;

namespace SkyHorizont.Domain.Social
{
    /// <summary>
    /// Resolves a planned intent into concrete social events,
    /// updates opinions/affection, and persists secrets if any.
    /// </summary>
    public interface IInteractionResolver
    {
        IEnumerable<ISocialEvent> Resolve(CharacterIntent intent, int currentYear, int currentMonth);
        void ClearCaches();
    }
}