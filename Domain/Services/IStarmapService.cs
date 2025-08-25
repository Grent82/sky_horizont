namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Provides access to basic starmap information such as system distances
    /// and locating nearby pirate factions.
    /// </summary>
    public interface IStarmapService
    {
        /// <summary>Returns the distance between two star systems.</summary>
        double GetDistance(Guid systemA, Guid systemB);

        /// <summary>
        /// Finds the pirate faction whose base is closest to the given system.
        /// Returns null if no pirate factions are known.
        /// </summary>
        Guid? GetNearestPirateFaction(Guid systemId);
    }
}
