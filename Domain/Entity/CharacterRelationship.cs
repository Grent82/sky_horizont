namespace SkyHorizont.Domain.Entity
{
    public record CharacterRelationship(Guid TargetCharacterId, RelationshipType Type);

    public enum RelationshipType
    {
        Spouse,
        Lover,
        Fiance,
        Partner,
        ExSpouse,
        Friend,
        Rival,
        HaremMember
    }
}
