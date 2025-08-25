using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class BuildInfrastructureIntentRule : IIntentRule
    {
        private readonly IPlanetRepository _planets;

        public BuildInfrastructureIntentRule(IPlanetRepository planets)
        {
            _planets = planets;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            if (ctx.ActorFactionId == Guid.Empty)
                yield break;

            var planetId = GetCharacterPlanetId(ctx.Actor.Id);
            if (!planetId.HasValue)
                yield break;
            var planet = _planets.GetById(planetId.Value);
            if (planet == null || planet.FactionId != ctx.ActorFactionId)
                yield break;

            var score = ScoreBuildInfrastructure(ctx, planet) * ctx.AmbitionBias.BuildInfrastructure;
            if (score > 0)
                yield return new ScoredIntent(IntentType.BuildInfrastructure, score, null, null, planet.Id);
        }

        private Guid? GetCharacterPlanetId(Guid characterId)
        {
            foreach (var p in _planets.GetAll())
                if (p.Citizens.Contains(characterId) || p.Prisoners.Contains(characterId))
                    return p.Id;
            return null;
        }

        private double ScoreBuildInfrastructure(IntentContext ctx, Planet planet)
        {
            double baseScore = 10.0;
            if (ctx.FactionStatus.EconomyWeak)
                baseScore += 20.0;
            baseScore += (100 - planet.InfrastructureLevel) * 0.5;
            if (ctx.Ambition is CharacterAmbition.BuildWealth or CharacterAmbition.EnsureFamilyLegacy)
                baseScore += 15.0;
            return Clamp0to100(baseScore);
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
