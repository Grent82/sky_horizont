using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Research;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Aggregates research points per faction+technology.
    /// Uses IFactionInfo to resolve the contributor’s faction.
    /// </summary>
    public class ResearchService : IResearchService
    {
        private readonly IResearchRepository _repository;
        private readonly IFactionInfo _factionInfo;

        public ResearchService(IResearchRepository repository, IFactionInfo factionInfo)
        {
            _repository = repository;
            _factionInfo = factionInfo;
        }

        public void AddProgress(Guid characterId, string technology, int points)
        {
            if (string.IsNullOrWhiteSpace(technology)) return;
            if (points <= 0) return;

            var factionId = _factionInfo.GetFactionIdForCharacter(characterId);
            var key = (factionId, technology.Trim());
            _repository.AddProgress(key, points);
        }

        public int GetProgress(Guid factionId, string technology)
        {
            var key = (factionId, technology.Trim());
            return _repository.GetByKey(key);
        }
    }
}
