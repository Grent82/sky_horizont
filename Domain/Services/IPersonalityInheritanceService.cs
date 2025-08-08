using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Combines parents' personalities + randomness to produce a child's baseline.
    /// </summary>
    public interface IPersonalityInheritanceService
    {
        Personality Inherit(Personality mother, Personality father);
    }
}
