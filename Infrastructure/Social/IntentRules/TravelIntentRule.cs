using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class TravelIntentRule : IIntentRule
    {
        private readonly IPlanetRepository _planets;
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public TravelIntentRule(IPlanetRepository planets, IFactionService factions, IRandomService rng)
        {
            _planets = planets;
            _factions = factions;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var dest = PickTravelDestinationPlanet(ctx.Actor, ctx.ActorFactionId, ctx.FactionOf, ctx.OpinionOf);
            if (!dest.HasValue)
                yield break;

            var score = ScoreTravel(ctx.Actor, dest.Value, ctx.FactionStatus, ctx.SystemSecurity, ctx.Config) * ctx.AmbitionBias.TravelToPlanet;
            if (score > 0)
                yield return new ScoredIntent(IntentType.TravelToPlanet, score, null, null, dest.Value);
        }

        private Guid? PickTravelDestinationPlanet(Character actor, Guid actorFactionId, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var currentPlanetId = GetCharacterPlanetId(actor.Id);
            if (!currentPlanetId.HasValue) return null;

            var loved = actor.Relationships
                .Where(r => r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse)
                .Select(r => r.TargetCharacterId)
                .Concat(actor.FamilyLinkIds)
                .Distinct()
                .ToList();

            var pool = _planets.GetAll()
                .Where(p => p.Id != currentPlanetId.Value)
                .Select(p => new
                {
                    Planet = p,
                    Score = (loved.Any(id => p.Citizens.Contains(id) || p.Prisoners.Contains(id)) ? 50 : 0) +
                            (fac(p.FactionId) == actorFactionId ? 20 : 0) +
                            (_factions.HasAlliance(actorFactionId, p.FactionId) ? 15 : 0) +
                            (p.IsTradeHub ? 10 : 0) -
                            (p.UnrestLevel > 50 ? p.UnrestLevel * 0.1 : 0) +
                            _rng.NextInt(0, 20)
                })
                .OrderByDescending(x => x.Score)
                .Take(5)
                .ToList();

            return pool.Count > 0 ? pool[_rng.NextInt(0, pool.Count)].Planet.Id : null;
        }

        private double ScoreTravel(Character actor, Guid destinationPlanetId, FactionStatus factionStatus, SystemSecurity? systemSecurity, PlannerConfig cfg)
        {
            var baseScore = 20.0;
            baseScore += (actor.Personality.Extraversion - 50) * 0.3;
            baseScore += (actor.Personality.Openness - 50) * 0.3;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Openness"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            var destPlanet = _planets.GetById(destinationPlanetId);
            if (destPlanet != null && destPlanet.UnrestLevel > 50)
                baseScore -= 10;
            if (factionStatus.HasAlliance && _factions.GetFactionIdForPlanet(destinationPlanetId) == _factions.GetFactionIdForCharacter(actor.Id))
                baseScore += 15;
            if (systemSecurity != null && systemSecurity.PirateActivity > 50)
                baseScore -= systemSecurity.PirateActivity * 0.1;
            return Clamp0to100(baseScore * cfg.TravelWeight);
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
