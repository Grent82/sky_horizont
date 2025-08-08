using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class BattleOutcomeService : IBattleOutcomeService
    {
        private readonly ICharacterFundsService _characterFundsService;
        private readonly IFactionFundsRepository _factionFundRepo;
        private readonly IFactionTaxService _factionTaxService;
        private readonly ICharacterRepository _characterRepo;
        private readonly IMoraleService _moraleService;

        public BattleOutcomeService(
            ICharacterFundsService characterFundsService,
            IFactionFundsRepository factionFundRepo,
            IFactionTaxService factionTaxService,
            ICharacterRepository characterRepo,
            IMoraleService moraleService)
        {
            _characterFundsService = characterFundsService;
            _factionFundRepo = factionFundRepo;
            _factionTaxService = factionTaxService;
            _characterRepo = characterRepo;
            _moraleService = moraleService;
        }

        public void ProcessFleetBattle(Fleet attacker, Fleet defender, BattleResult result)
        {
            if (attacker.AssignedCharacterId is Guid attackerCmdId)
            {
                var character = _characterRepo.GetById(attackerCmdId);
                if (character != null)
                {
                    character.GainMerit(result.OutcomeMerit);
                    _characterFundsService.CreditCharacter(character.Id, result.LootCredits);

                    // Bonus for thrill-seeking characters
                    if (PersonalityTraits.ThrillSeeker(character.Personality))
                    {
                        int bonus = (int)(result.LootCredits * 0.1);
                        _characterFundsService.CreditCharacter(character.Id, bonus);
                    }

                    _moraleService.AdjustMoraleForVictory(character.Id, result);
                }
            }

            if (defender.AssignedCharacterId is Guid defenderCmdId)
            {
                _moraleService.AdjustMoraleForDefeat(defenderCmdId, result);
            }
        }

        public void ProcessPlanetConquest(Planet planet, Fleet attackerFleet, BattleResult result)
        {
            planet.ChangeControl(attackerFleet.FactionId);
            planet.StationFleet(attackerFleet);
            planet.SetStationedTroops(result.OccupationDurationHours * 10);

            if (attackerFleet.AssignedCharacterId is Guid leaderId)
            {
                var leader = _characterRepo.GetById(leaderId);
                if (leader != null)
                {
                    leader.GainMerit(result.OutcomeMerit);

                    // Bonus for achievement-oriented characters
                    if (PersonalityTraits.AchievementOriented(leader.Personality))
                    {
                        int bonus = (int)(result.PlanetCaptureBonus * 0.2);
                        _characterFundsService.CreditCharacter(leader.Id, bonus);
                        result = result with { PlanetCaptureBonus = result.PlanetCaptureBonus - bonus };
                    }

                    _moraleService.AdjustMoraleForConquest(leader.Id, planet);
                }
            }

            _factionFundRepo.AddBalance(attackerFleet.FactionId, result.PlanetCaptureBonus);

            if (attackerFleet.AssignedCharacterId.HasValue)
            {
                var subordinates = planet.GetAssignedSubCharacters();
                _factionTaxService.DistributeLoot(attackerFleet.AssignedCharacterId.Value, result.LootCredits, subordinates);
            }
        }
    }
}
