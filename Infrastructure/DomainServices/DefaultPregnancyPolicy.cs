using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class DefaultPregnancyPolicy : IPregnancyPolicy
    {
        private readonly IRandomService _rng;
        public DefaultPregnancyPolicy(IRandomService rng) => _rng = rng;

        public bool ShouldHaveTwins(Character mother, int y, int m)
        {
            // ~2% baseline; slight bump if Extraversion+Openness high (totally optional flare)
            double b = 0.02 + (mother.Personality.Extraversion + mother.Personality.Openness - 100) * 0.0001;
            return _rng.NextDouble() < Math.Clamp(b, 0.01, 0.05);
        }

        public bool ShouldHaveComplications(Character mother, int y, int m, out string? note)
        {
            // ~5% baseline; higher Neuroticism nudges up (stress proxy)
            double p = 0.05 + mother.Personality.Neuroticism * 0.0003; // max +3%
            bool hit = _rng.NextDouble() < Math.Clamp(p, 0.03, 0.10);
            note = hit ? "Complications at birth." : null;
            return hit;
        }
    }
}
