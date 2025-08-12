using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class SimpleSkillInheritanceService : ISkillInheritanceService
    {
        public SkillSet Inherit(SkillSet mother, SkillSet? father, IRandomService rng)
        {
            father ??= new SkillSet(40, 40, 40, 40);
            int Mix(int a, int b)
            {
                var avg = (a + b) / 2;
                var jitter = (int)Math.Round((rng.NextDouble() - 0.5) * 10); // ±5
                return Math.Clamp(avg + jitter, 0, 100);
            }

            return new SkillSet(
                Mix(mother.Research, father.Research),
                Mix(mother.Economy, father.Economy),
                Mix(mother.Intelligence, father.Intelligence),
                Mix(mother.Military, father.Military)
            );
        }
    }
}