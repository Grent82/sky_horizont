namespace SkyHorizont.Domain.Services
{
    public interface IGameClockService
    {
        int CurrentYear { get; }
        int CurrentMonth { get; }
        int MonthsPerYear  { get;}

        void AdvanceTurn();
    }
}