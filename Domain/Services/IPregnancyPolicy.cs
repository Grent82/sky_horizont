using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Domain.Services
{
    public interface IPregnancyPolicy
    {
        bool ShouldHaveTwins(Character mother, int year, int month);
        bool ShouldHaveComplications(Character mother, int year, int month, out string? note);
        bool IsPostpartumProtected(Character mother, int year, int month);
        void RecordDelivery(Guid motherId, int year, int month);
        bool CanConceiveWith(Character potentialMother, Character partner, int year, int month);
    }
}
