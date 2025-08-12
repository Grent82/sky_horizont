namespace SkyHorizont.Domain.Diplomacy
{
    public interface IDiplomacyService
    {
        // ToDo: Diplomacy implementation
        void ProposeTreaty(Guid fromFaction, Guid toFaction, TreatyType type);
        void TickTreaties(); // decay, violations, casus belli
    }
}