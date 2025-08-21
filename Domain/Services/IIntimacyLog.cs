namespace SkyHorizont.Domain.Services
{
    public interface IIntimacyLog
    {
        void RecordIntimacyEncounter(Guid charA, Guid charB, int year, int month);

        IReadOnlyList<Guid> GetPartnersForMother(Guid motherId, int year, int month);
        void PurgeOlderThan(int year, int month);
    }
}
