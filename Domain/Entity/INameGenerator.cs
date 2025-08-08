namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Generates character names. Keep interface in Domain; plug
    /// culture/locale/Markov-based implementation in Infrastructure.
    /// </summary>
    public interface INameGenerator
    {
        /// <param name="sex">Optional hint for culture rules.</param>
        /// <param name="culture">E.g., "Terran-3599", "Zyr-Collective".</param>
        string GenerateGivenName(Entity.Sex? sex = null, string? culture = null);

        /// <param name="culture">Culture/house/clan affects surname form.</param>
        string GenerateSurname(string? culture = null);

        /// <summary>Convenience: "Given Surname"</summary>
        string GenerateFullName(Entity.Sex? sex = null, string? culture = null);
    }
}
