using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class ClaimPlanetSeatIntentRule : IIntentRule
    {
        private readonly IPlanetRepository _planets;

        public ClaimPlanetSeatIntentRule(IPlanetRepository planets)
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
            if (planet == null)
                yield break;
            if (planet.IsSeatOf(ctx.ActorFactionId))
                yield break;

            var score = ScoreClaimSeat(ctx.Actor, planet, ctx.Config) * ctx.AmbitionBias.ClaimPlanetSeat;
            if (score > 0)
                yield return new ScoredIntent(IntentType.ClaimPlanetSeat, score, null, null, planet.Id);
        }

        private double ScoreClaimSeat(Character actor, Planet planet, PlannerConfig cfg)
        {
            double baseScore = 40.0;
            baseScore += (int)actor.Rank * 10;
            baseScore += planet.InfrastructureLevel * 0.2;
            return Clamp0to100(baseScore * cfg.ClaimPlanetSeatWeight);
        }

        private Guid? GetCharacterPlanetId(Guid characterId)
        {
            foreach (var p in _planets.GetAll())
                if (p.Citizens.Contains(characterId) || p.Prisoners.Contains(characterId))
                    return p.Id;
            return null;
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
