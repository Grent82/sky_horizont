using SkyHorizont.Domain.Diplomacy;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class DiplomacyService : IDiplomacyService
    {
        private readonly IDiplomacyRepository _treaties;
        private readonly IFactionService _factions;
        private readonly IGameClockService _clock;
        private readonly IOpinionRepository _opinions;

        private static readonly Dictionary<TreatyType, int?> DurationByType = new()
        {
            { TreatyType.Ceasefire,      6 },
            { TreatyType.NonAggression, 12 },
            { TreatyType.Trade,         24 },
            { TreatyType.ResearchPact,  24 },
            { TreatyType.Alliance,      36 }, // make null for indefinite if you prefer
        };

        public DiplomacyService(
            IDiplomacyRepository treaties,
            IFactionService factions,
            IGameClockService clock,
            IOpinionRepository opinions)
        {
            _treaties = treaties;
            _factions = factions;
            _clock = clock;
            _opinions = opinions;
        }

        public void ProposeTreaty(Guid fromFaction, Guid toFaction, TreatyType type)
        {
            if (fromFaction == Guid.Empty || toFaction == Guid.Empty || fromFaction == toFaction)
                return;

            // If at war, only allow de-escalation treaties
            bool atWar = _factions.IsAtWar(fromFaction, toFaction);
            if (atWar && type is not (TreatyType.Ceasefire or TreatyType.NonAggression))
                return;

            // Abort if an active treaty of the same type already exists
            var activeSameType = _treaties.FindBetween(fromFaction, toFaction)
                .Any(t => t.Type == type && !IsExpired(t));
            if (activeSameType) return;

            var startTurn = CurrentTurn();
            int? endTurn = null;
            if (DurationByType.TryGetValue(type, out var dur) && dur.HasValue)
                endTurn = startTurn + dur.Value;

            var treaty = new Treaty(Guid.NewGuid(), fromFaction, toFaction, type, startTurn, endTurn);
            _treaties.Add(treaty);

            // Light opinion boost between leaders to reflect goodwill
            var aLeader = _factions.GetLeaderId(fromFaction);
            var bLeader = _factions.GetLeaderId(toFaction);
            if (aLeader.HasValue && bLeader.HasValue)
            {
                _opinions.AdjustOpinion(aLeader.Value, bLeader.Value, +10, $"Treaty signed: {type}");
                _opinions.AdjustOpinion(bLeader.Value, aLeader.Value, +10, $"Treaty signed: {type}");
            }
        }

        public void TickTreaties()
        {
            var now = CurrentTurn();
            var all = _treaties.GetAll().ToList();

            foreach (var t in all)
            {
                // Expire by time
                if (t.EndTurn.HasValue && t.EndTurn.Value <= now)
                {
                    _treaties.Remove(t.Id);
                    continue;
                }

                // ToDo: (Hook) Violations / casus belli:
                // If you later track violations (border incidents, espionage during NAP, etc.),
                // call EndTreaty(t, reason) here and maybe adjust opinions / add a CB flag.
            }
        }

        private bool IsExpired(Treaty t)
        {
            if (!t.EndTurn.HasValue) return false;
            return t.EndTurn.Value <= CurrentTurn();
        }

        private int CurrentTurn() => _clock.CurrentYear * _clock.MonthsPerYear + _clock.CurrentMonth;
        
        private void EndTreaty(Treaty t, string reason, int opinionHit = -8)
        {
            _treaties.Remove(t.Id);
            var aLeader = _factions.GetLeaderId(t.FactionA);
            var bLeader = _factions.GetLeaderId(t.FactionB);
            if (aLeader.HasValue && bLeader.HasValue && opinionHit != 0)
            {
                _opinions.AdjustOpinion(aLeader.Value, bLeader.Value, opinionHit, $"Treaty ended: {t.Type} ({reason})");
                _opinions.AdjustOpinion(bLeader.Value, aLeader.Value, opinionHit, $"Treaty ended: {t.Type} ({reason})");
            }
        }
    }
}
