namespace SkyHorizont.Domain.Intrigue
{
     public sealed record Secret( // ToDo:  Keep it simple and expand later.
        Guid Id,
        SecretType Type,
        string Summary,
        Guid? AboutCharacterId,
        Guid? AboutFactionId,
        int Severity,
        int Year,
        int Month
    );
}