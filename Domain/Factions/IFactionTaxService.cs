namespace SkyHorizont.Domain.Factions
{
    public interface IFactionTaxService
    {
        void TaxPlanet(Guid planetId, double percentage);
        void DistributeLoot(Guid leaderCharacterId, int totalLoot, IEnumerable<Guid> subCharacterIds);
    }
}