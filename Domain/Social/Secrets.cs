namespace SkyHorizont.Domain.Social
{
    public enum SecretType
    {
        MilitaryDisposition,
        TechBreakthrough,
        Corruption,
        Infidelity,
        TreasonousContact,
        AssassinationPlot
        // ToDo: Lovers
        // ToDo: Children of Leader
    }

    /// <summary> Abstract “intel” unit.</summary>
    public sealed record Secret( // ToDo:  Keep it simple and expand later.
        Guid Id,
        SecretType Type,
        string Summary,
        Guid? AboutCharacterId,
        Guid? AboutFactionId,
        int Severity,         // 1..100
        int TurnDiscovered
    );

    public interface ISecretsRepository
    {
        Secret Add(Secret secret);
        Secret? GetById(Guid id);
    }
}
