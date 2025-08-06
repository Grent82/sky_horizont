namespace SkyHorizont.Domain.Factions
{
    public interface IFactionTaxService
    {
        void TaxPlanet(Guid planetId, int percentage);
        void DistributeLoot(Guid leaderCommanderId, int totalLoot, IEnumerable<Guid> subCommanderIds);
    }
}