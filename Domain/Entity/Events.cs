namespace SkyHorizont.Domain.Entity
{
    public sealed record BirthdayOccurred(Guid CharacterId, int Year, int Month);
    public sealed record ChildBorn(Guid ChildId, Guid MotherId, Guid? FatherId, int Year, int Month);
}