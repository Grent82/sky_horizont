namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Generates character names. Keep interface in Domain; plug
    /// culture/locale/Markov-based implementation in Infrastructure.
    /// </summary>
    public interface INameGenerator
    {
        /// <param name="sex">Optional hint for faction rules.</param>
        /// <param name="culture">E.g., "Terran-3599", "Zyr-Collective".</param>
        string GenerateFirstName(Entity.Sex? sex = null, string? faction = null);

        /// <param name="culture">faction/house/clan affects surname form.</param>
        string GenerateSurname(string? culture = null);

        string GenerateFullName(Entity.Sex? sex = null, string? faction = null);
    }
}
