using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Utility
{
    public static class FactionRelationSeeder
    {
        public static void MapCharacterToFaction(this IFactionsDbContext ctx, Guid characterId, Guid factionId)
        {
            ctx.CharacterFaction[characterId] = factionId;
        }

        public static void SetLeader(this IFactionsDbContext ctx, Guid factionId, Guid? leaderId)
        {
            ctx.FactionLeaders[factionId] = leaderId;
        }

        public static void AddWar(this IFactionsDbContext ctx, Guid a, Guid b)
        {
            ctx.WarPairs.Add(Normalize(a, b));
        }

        public static void AddRivalry(this IFactionsDbContext ctx, Guid a, Guid b)
        {
            ctx.RivalPairs.Add(Normalize(a, b));
        }

        private static (Guid a, Guid b) Normalize(Guid x, Guid y)
            => x.CompareTo(y) <= 0 ? (x, y) : (y, x);
    }
}
