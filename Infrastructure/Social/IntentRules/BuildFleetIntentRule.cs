using System.Linq;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class BuildFleetIntentRule : IIntentRule
    {
        private readonly IFleetRepository _fleets;
        private readonly IPiracyService _piracy;

        public BuildFleetIntentRule(IFleetRepository fleets, IPiracyService piracy)
        {
            _fleets = fleets;
            _piracy = piracy;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var factionFleets = _fleets.GetFleetsForFaction(ctx.ActorFactionId).ToList();
            double strength = factionFleets.Sum(f => f.CalculateStrength().MilitaryPower);

            double score = 0;
            if (strength < 100) score += 50;
            else if (strength < 300) score += 20;

            if (ctx.FactionStatus.IsAtWar) score += 40;
            if (ctx.SystemSecurity?.PirateActivity > 50) score += 25;
            if (ctx.SystemSecurity?.Traffic > 80) score += 15;
            if (_piracy.IsPirateFaction(ctx.ActorFactionId)) score += 30;

            score *= ctx.AmbitionBias.BuildFleet;

            if (score > 0)
                yield return new ScoredIntent(IntentType.BuildFleet, score, null, null, null);
        }
    }
}
