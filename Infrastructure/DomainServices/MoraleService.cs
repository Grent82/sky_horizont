using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class MoraleService : IMoraleService
    {
        private readonly ICommanderRepository _commanderRepository;

        public MoraleService(ICommanderRepository commanderRepository)
        {
            _commanderRepository = commanderRepository;
        }

        public void ApplyMoraleEffects()
        {
            // For each capturing commander, if any captives executed or still held,
            // adjust reputation (neg or pos) accordingly
        }
    }
}
