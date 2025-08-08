namespace SkyHorizont.Domain.Services
{
    public record CharacterRelationship(Guid TargetCharacterId, RelationshipType Type);

    public enum RelationshipType
    {
        Spouse,
        Lover,
        Fiance,
        Partner,
        ExSpouse
    }
}
