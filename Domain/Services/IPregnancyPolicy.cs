using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Domain.Services
{
    public interface IPregnancyPolicy
    {
        bool ShouldHaveTwins(Character mother, int year, int month);
        bool ShouldHaveComplications(Character mother, int year, int month, out string? note);
    }
}
