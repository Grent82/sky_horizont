namespace SkyHorizont.Domain.Services
{
    public interface IIntelService
    {
        void RecordReport(Guid characterId, Guid targetFactionId, string summary, int intelValue, int year, int month);
        IEnumerable<IntelReport> GetReportsForFaction(Guid targetFactionId);
    }
}
