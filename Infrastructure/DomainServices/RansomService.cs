using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Provides operations for settling ransom payments between characters.
    /// </summary>
    public class RansomService : IRansomService
    {
        private readonly ICharacterRepository _cmdRepo;
        private readonly ICharacterFundsService _funds;
        private readonly IFactionFundsRepository _factionFunds;
        private readonly IPlanetRepository _planetRepo;
        private readonly IFleetRepository _fleetRepo;
        private readonly IRansomDecisionService _decision;
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public RansomService(
            ICharacterRepository characterRepository,
            ICharacterFundsService characterFundsService,
            IFactionFundsRepository fleetRepository,
            IPlanetRepository planetRepo,
            IFleetRepository fleetRepo,
            IRansomDecisionService decisionService,
            IFactionService factions,
            IRandomService rng)
        {
            _cmdRepo = characterRepository;
            _funds = characterFundsService;
            _factionFunds = fleetRepository;
            _planetRepo = planetRepo;
            _fleetRepo = fleetRepo;
            _decision = decisionService;
            _factions = factions;
            _rng = rng;
        }

        /// <summary>
        /// Attempts to settle a ransom between a payer and a captive.
        /// The payer is first evaluated for willingness via <see cref="IRansomDecisionService"/>
        /// and then charged if sufficient funds exist.
        /// </summary>
        public bool TryResolveRansom(Guid payerId, Guid captiveId, int amount)
        {
            if (!_decision.WillPayRansom(payerId, captiveId, amount))
                return false;
            if (!_funds.DeductCharacter(payerId, amount))
                return false;
            _funds.CreditCharacter(captiveId, amount);
            return true;
        }

        public void HandleUnpaidRansom(Guid captiveId, Guid captorFaction)
        {
            var captive = _cmdRepo.GetById(captiveId);
            if (captive == null)
                return;

            var outcome = _rng.NextInt(0, 3);
            switch (outcome)
            {
                case 0:
                    // Sold to a slavery market; no specific owner.
                    captive.Enslave(null);
                    break;
                case 1:
                    // Transferred to captor's harem/crew.
                    var owner = _factions.GetLeaderId(captorFaction);
                    captive.Enslave(owner);
                    _factions.MoveCharacterToFaction(captiveId, captorFaction);
                    break;
                default:
                    // Execution.
                    captive.MarkDead();
                    break;
            }

            _cmdRepo.Save(captive);
        }
    }
}
