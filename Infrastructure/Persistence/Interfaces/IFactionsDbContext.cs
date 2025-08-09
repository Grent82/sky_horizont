namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IFactionsDbContext : IBaseDbContext
    {
        /// <summary>Character Faction mapping.</summary>
        Dictionary<Guid, Guid> CharacterFaction { get; }

        /// <summary>Faction Leader</summary>
        Dictionary<Guid, Guid?> FactionLeaders { get; }

        /// <summary>Undirected war pairs between factions.</summary>
        HashSet<(Guid a, Guid b)> WarPairs { get; }

        /// <summary>Undirected rival pairs between factions.</summary>
        HashSet<(Guid a, Guid b)> RivalPairs { get; }
    }
}