namespace SkyHorizont.Domain.Diplomacy
{
    public interface IDiplomacyRepository
    {
        IEnumerable<Treaty> GetAll();
        IEnumerable<Treaty> FindBetween(Guid factionA, Guid factionB);
        Treaty? GetById(Guid id);
        Treaty Add(Treaty treaty);
        void Save(Treaty treaty);
        void Remove(Guid id);
    }
}
