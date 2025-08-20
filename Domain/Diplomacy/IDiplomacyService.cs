namespace SkyHorizont.Domain.Diplomacy
{
    public interface IDiplomacyService
    {
        void ProposeTreaty(Guid fromFaction, Guid toFaction, TreatyType type);
        void TickTreaties();
        void AdjustRelations(Guid factionA, Guid factionB, int delta);
    }
}