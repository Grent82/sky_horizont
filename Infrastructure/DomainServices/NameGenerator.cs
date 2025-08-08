using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Simple syllable-based generator (swap later with culture/Markov models).
    /// </summary>
    public sealed class NameGenerator : INameGenerator
    {
        private readonly IRandomService _rng;

        // Tiny seed lists; replace with culture packs later
        private static readonly string[] Prefixes = { "Ka", "Li", "Ar", "Va", "Xe", "Ny", "Zo", "Qu", "Sa", "Tor", "Ish" };
        private static readonly string[] Middles  = { "ri", "la", "no", "the", "va", "zen", "qui", "ra", "mon", "sha" };
        private static readonly string[] Suffixes = { "n", "ra", "is", "el", "ia", "or", "en", "ix", "ath", "ara" };

        private static readonly string[] ClanPrefixes = { "von", "de", "da", "ul", "nar", "ket", "of", "ix" };
        private static readonly string[] ClanCores    = { "Sol", "Orion", "Voss", "Rahn", "Kade", "Mara", "Zhiv", "Quor", "Tarr" };

        public NameGenerator(IRandomService rng) => _rng = rng;

        public string GenerateGivenName(Sex? sex = null, string? culture = null)
        {
            var parts = new List<string>
            {
                Pick(Prefixes),
                _rng.NextDouble() < 0.6 ? Pick(Middles) : string.Empty,
                Pick(Suffixes)
            };
            var name = string.Concat(parts);
            if (sex == Sex.Female && _rng.NextDouble() < 0.5) name += "a";
            return Capitalize(name);
        }

        public string GenerateSurname(string? culture = null)
        {
            var usePrefix = _rng.NextDouble() < 0.5;
            var core = Pick(ClanCores);
            return usePrefix ? $"{Pick(ClanPrefixes)} {core}" : core;
        }

        public string GenerateFullName(Sex? sex = null, string? culture = null)
            => $"{GenerateGivenName(sex, culture)} {GenerateSurname(culture)}";

        private string Pick(string[] arr) => arr[_rng.NextInt(0, arr.Length)];
        private static string Capitalize(string s) => string.IsNullOrWhiteSpace(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
    }
}
