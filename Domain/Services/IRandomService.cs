namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Wraps randomness so sims are deterministic per seed; easy to test.
    /// </summary>
    public interface IRandomService
    {
        int NextInt(int minInclusive, int maxExclusive);
        double NextDouble(); // [0,1)
        void Reseed(int seed);
        int CurrentSeed { get; }
    }
}
