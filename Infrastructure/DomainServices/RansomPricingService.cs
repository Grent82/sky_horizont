using System;
using System.Collections.Generic;
using System.Linq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Calculates ransom prices using rank, parentage and faction strength.
    /// </summary>
    public class RansomPricingService : IRansomPricingService
    {
        private readonly ICharacterRepository _characters;
        private readonly IFactionService _factions;

        private static readonly Dictionary<Rank, int> BaseValues = new()
        {
            { Rank.Civilian, 100 },
            { Rank.Courtesan, 200 },
            { Rank.Lieutenant, 400 },
            { Rank.Captain, 800 },
            { Rank.Major, 1600 },
            { Rank.Colonel, 3200 },
            { Rank.General, 6400 },
            { Rank.Leader, 10000 }
        };

        public RansomPricingService(ICharacterRepository characters, IFactionService factions)
        {
            _characters = characters;
            _factions = factions;
        }

        public int EstimateRansomValue(Guid captiveId)
        {
            var captive = _characters.GetById(captiveId);
            if (captive == null)
                return 0;

            var baseValue = BaseValues.TryGetValue(captive.Rank, out var value) ? value : 100;

            var parents = _characters.GetFamilyMembers(captiveId)
                .Where(p => p.Age > captive.Age)
                .Take(2)
                .ToList();

            double parentModifier = 1.0;
            if (parents.Count > 0)
            {
                double sum = 0;
                foreach (var parent in parents)
                {
                    sum += 1 + (int)parent.Rank * 0.1;
                }
                parentModifier = sum / parents.Count;
            }

            var factionId = _factions.GetFactionIdForCharacter(captiveId);
            var strength = _factions.GetEconomicStrength(factionId);
            double factionFactor = strength >= 1000 ? 2.0 : strength >= 500 ? 1.5 : 1.0;

            return (int)(baseValue * parentModifier * factionFactor);
        }
    }
}
