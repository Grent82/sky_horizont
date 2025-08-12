using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Domain.Services
{
    public interface ISkillInheritanceService
    {
        SkillSet Inherit(SkillSet mother, SkillSet? father, IRandomService rng);
    }
}