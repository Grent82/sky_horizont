namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IPlanetEconomyDbContext : IBaseDbContext
    {
        // ToDo: enhance per-planet treasury
        IDictionary<Guid, int> PlanetBudgets { get; }
    }
}
