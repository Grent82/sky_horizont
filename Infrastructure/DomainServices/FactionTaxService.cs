// SkyHorizont.Infrastructure.DomainServices/FactionTaxService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SkyHorizont.Domain.Economy;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Handles faction taxes and post‑battle loot distribution.
    /// Taxes move credits from a planet's local budget to the controlling faction's treasury.
    /// Loot distribution optionally skims a faction tithe, gives a leader cut, then splits the rest among subordinates.
    /// </summary>
    public class FactionTaxService : IFactionTaxService
    {
        private readonly IFactionFundsRepository _factionFundsRepo;
        private readonly ICharacterFundsRepository _characterFundsRepo;
        private readonly IPlanetRepository _planets;
        private readonly IPlanetEconomyRepository _eco;
        private readonly IFactionService _factions;
        private readonly ICharacterRepository _characters;
        private readonly IGameClockService _clock;

        // Tunables (feel free to externalize)
        // ToDo: move to config file
        private const int DefaultFactionLootTaxPercent = 10; // % skim from loot to faction before shares
        private const int DefaultLeaderCutPercent      = 40; // % of post‑tax loot to leader
        private const int MinPercent                   = 0;
        private const int MaxPercent                   = 90;

        public FactionTaxService(
            IFactionFundsRepository factionFundsRepo,
            ICharacterFundsRepository characterFundsRepo,
            IPlanetRepository planets,
            IPlanetEconomyRepository eco,
            IFactionService factions,
            ICharacterRepository characters,
            IGameClockService clock)
        {
            _factionFundsRepo   = factionFundsRepo;
            _characterFundsRepo = characterFundsRepo;
            _planets            = planets;
            _eco                = eco;
            _factions           = factions;
            _characters         = characters;
            _clock              = clock;
        }

        /// <summary>
        /// Collects a percentage of the planet's current local budget into the controlling faction's treasury.
        /// Effective rate and collection efficiency are modified by the governor's Economy skill and personality facets.
        /// </summary>
        public void TaxPlanet(Guid planetId, int percentage)
        {
            percentage = Math.Clamp(percentage, MinPercent, MaxPercent);

            var planet = _planets.GetById(planetId);
            if (planet is null) return;

            var factionId = planet.ControllingFactionId;
            if (factionId == Guid.Empty) return;

            int budget = _eco.GetPlanetBudget(planet.Id);
            if (budget <= 0 || percentage <= 0) return;

            // Governor influence
            double effectiveRate = percentage;
            double efficiency    = 1.0;

            if (planet.GovernorId.HasValue && planet.GovernorId.Value != Guid.Empty)
            {
                var gov = _characters.GetById(planet.GovernorId.Value);
                if (gov != null && gov.IsAlive)
                {
                    // Economy skill: ±20% swing (Economy=100 → +20%, Economy=0 → -20%)
                    double econBias = (gov.Skills.Economy - 50) / 250.0; // -0.20..+0.20
                    effectiveRate *= (1.0 + econBias);

                    // Personality facets
                    var p = gov.Personality;

                    // Honest/Dutiful → less graft → better efficiency
                    if (PersonalityTraits.Honest(p))                efficiency += 0.05;
                    if (PersonalityTraits.Dutiful(p))               efficiency += 0.05;

                    // SelfEfficient → competent collection
                    if (PersonalityTraits.SelfEfficient(p))         efficiency += 0.02;

                    // Cooperative/Modest → gentler policy (lower rate) but some goodwill (slight efficiency up)
                    if (PersonalityTraits.Cooperative(p))           { effectiveRate *= 0.97; efficiency += 0.01; }
                    if (PersonalityTraits.Modest(p))                { effectiveRate *= 0.98; }

                    // Risky/unstable traits reduce efficiency (leakage/corruption/opposition)
                    if (PersonalityTraits.Impulsive(p))             efficiency -= 0.04;
                    if (PersonalityTraits.EasilyAngered(p))         efficiency -= 0.02;
                    if (PersonalityTraits.StressVulnerable(p))      efficiency -= 0.03;

                    // Keep sane bounds
                    effectiveRate = Math.Clamp(effectiveRate, MinPercent, MaxPercent);
                    efficiency    = Math.Clamp(efficiency, 0.75, 1.20);
                }
            }

            int rawTax   = (int)Math.Floor(budget * (effectiveRate / 100.0));
            int collected = (int)Math.Floor(rawTax * efficiency);
            if (collected <= 0) return;

            if (_eco.TryDebitBudget(planet.Id, collected))
            {
                _factionFundsRepo.AddBalance(factionId, collected);

                // Optional: small stability effect — harsh effective rates hurt stability a bit;
                // good governance (high efficiency) softens the blow.
                // Example curve: -0.002 per percentage point over 25%, scaled by (1.05 - efficiency).
                if (effectiveRate > 25)
                {
                    double harsh = (effectiveRate - 25) * 0.002;           // base penalty
                    double soften = Math.Max(0.85, 1.05 - efficiency);      // better efficiency reduces pain
                    double delta  = -harsh * soften;                        // negative reduces stability
                    planet.AdjustStability(planet.Stability + delta);
                    _planets.Save(planet);
                }

                _eco.AddEventLog(new EconomyEvent(
                    Year:  _clock.CurrentYear,
                    Month: _clock.CurrentMonth,
                    Kind:  "PlanetTax",
                    OwnerId: planet.Id,
                    Amount: -collected,
                    Note:  $"Base={percentage}%, EffRate={effectiveRate:F1}%, Eff={efficiency:F2}, Collected={collected} → faction {factionId}"
                ));
            }
            else
            {
                _eco.AddEventLog(new EconomyEvent(
                    Year:  _clock.CurrentYear,
                    Month: _clock.CurrentMonth,
                    Kind:  "PlanetTaxUnpaid",
                    OwnerId: planet.Id,
                    Amount: 0,
                    Note:  $"Unable to collect. Base={percentage}%, EffRate={effectiveRate:F1}%, Eff={efficiency:F2}"
                ));
            }
        }

        // ToDo: move to some other service
        /// <summary>
        /// Distributes loot
        /// </summary>
        public void DistributeLoot(Guid leaderCharacterId, int totalLoot, IEnumerable<Guid> subCharacterIds)
        {
            if (totalLoot <= 0) return;

            var subs = (subCharacterIds ?? Enumerable.Empty<Guid>())
                .Distinct()
                .Select(_characters.GetById)
                .Where(c => c != null && c.IsAlive)
                .Select(c => c!.Id)
                .ToList();

            var leader = _characters.GetById(leaderCharacterId);
            var leaderFaction = leader != null ? _factions.GetFactionIdForCharacter(leader.Id) : Guid.Empty;

            int tithePct = Math.Clamp(DefaultFactionLootTaxPercent, MinPercent, MaxPercent);
            int tithe = (int)Math.Floor(totalLoot * (tithePct / 100.0));
            int distributable = totalLoot - tithe;

            if (tithe > 0 && leaderFaction != Guid.Empty)
                _factionFundsRepo.AddBalance(leaderFaction, tithe);

            if (distributable <= 0) return;

            int leaderCutPct = Math.Clamp(DefaultLeaderCutPercent, MinPercent, 90);
            int leaderCut = (int)Math.Floor(distributable * (leaderCutPct / 100.0));
            int remaining = distributable - leaderCut;

            if (leader != null)
                _characterFundsRepo.AddBalance(leader.Id, leaderCut);

            if (subs.Count == 0)
            {
                if (leader != null && remaining > 0)
                    _characterFundsRepo.AddBalance(leader.Id, remaining);
                return;
            }

            int perSub = remaining / subs.Count;
            int leftover = remaining - perSub * subs.Count;

            foreach (var subId in subs)
                _characterFundsRepo.AddBalance(subId, perSub);

            if (leader != null && leftover > 0)
                _characterFundsRepo.AddBalance(leader.Id, leftover);
        }
    }
}
