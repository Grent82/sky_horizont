namespace SkyHorizont.Domain.Entity
{
    public interface IAffectionRepository
    {
        void AdjustAffection(Guid sourceCharacterId, Guid targetCharacterId, int delta);
    }
}