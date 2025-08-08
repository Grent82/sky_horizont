using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Very light "genetic" blend: ~50% parental average, ~50% environment/random,
    /// with slight regression to 50 (the trait midpoint). You can tune weights later.
    /// Twin studies suggest substantial heritability (~40–60%) across Big Five,
    /// which this service loosely nods to via weights.
    /// </summary>
    public sealed class SimplePersonalityInheritanceService : IPersonalityInheritanceService
    {
        private readonly IRandomService _rng;

        public SimplePersonalityInheritanceService(IRandomService rng) => _rng = rng;

        public Personality Inherit(Personality mother, Personality father)
        {
            Personality Avg(Personality a, Personality b) => new(
                Openness:           (a.Openness + b.Openness) / 2,
                Conscientiousness:  (a.Conscientiousness + b.Conscientiousness) / 2,
                Extraversion:       (a.Extraversion + b.Extraversion) / 2,
                Agreeableness:      (a.Agreeableness + b.Agreeableness) / 2,
                Neuroticism:        (a.Neuroticism + b.Neuroticism) / 2
            );

            var avg = Avg(mother, father);

            int Blend(int avgVal)
            {
                // 0.55 heritable weight; 0.30 regression to mean (50); 0.15 random noise.
                var heritable = 0.55 * avgVal;
                var regress   = 0.30 * 50.0;
                var noise     = 0.15 * ( _rng.NextDouble() * 100.0 ); // 0..15 span in effect
                var v = heritable + regress + noise;
                return Clamp01To100((int)System.Math.Round(v));
            }

            return new Personality(
                Openness:          Blend(avg.Openness),
                Conscientiousness: Blend(avg.Conscientiousness),
                Extraversion:      Blend(avg.Extraversion),
                Agreeableness:     Blend(avg.Agreeableness),
                Neuroticism:       Blend(avg.Neuroticism)
            );
        }

        private static int Clamp01To100(int v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
