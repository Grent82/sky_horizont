namespace SkyHorizont.Domain.Entity
{
    public interface IAffectionRepository
    {
        void AdjustAffection(Guid sourceCommanderId, Guid targetCommanderId, int delta);
    }
}