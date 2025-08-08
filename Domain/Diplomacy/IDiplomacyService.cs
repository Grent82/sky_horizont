namespace SkyHorizont.Domain.Diplomacy
{
    public interface IDiplomacyService
    {
        void ProposeTreaty(Guid fromFaction, Guid toFaction, TreatyType type);
        void TickTreaties(); // decay, violations, casus belli
    }
}