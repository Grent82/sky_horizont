using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class MoraleService : IMoraleService
    {
        private readonly ICommanderRepository _commanderRepository;

        public MoraleService(ICommanderRepository commanderRepository)
        {
            _commanderRepository = commanderRepository;
        }

        public void AdjustMoraleForConquest(Guid id, Planet planet)
        {
            
        }

        public void AdjustMoraleForDefeat(Guid defenderCmdId, BattleResult result)
        {
            
        }

        public void AdjustMoraleForVictory(Guid id, BattleResult result)
        {
            
        }

        public void ApplyMoraleEffects()
        {
            // For each capturing commander, if any captives executed or still held,
            // adjust reputation (neg or pos) accordingly
        }
    }
}
