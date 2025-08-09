namespace SkyHorizont.Domain.Intrigue
{
    public interface IPlotRepository
    {
        Plot? GetById(Guid id);
        IEnumerable<Plot> GetAll();
        Plot Create(Guid leaderId, string goal, IEnumerable<Guid> conspirators, IEnumerable<Guid> targets);
        void Save(Plot plot);
        void Remove(Guid plotId);
    }
}
