namespace SkyHorizont.Domain.Entity
{
    public interface ICommanderRepository
    {
        Commander? GetById(Guid commanderId);
        void Save(Commander commander);
    }
}
