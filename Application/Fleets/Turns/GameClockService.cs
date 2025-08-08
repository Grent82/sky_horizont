using SkyHorizont.Domain.Services;

namespace SkyHorizont.Application.Turns
{
    public class GameClockService : IGameClockService
    {
        public int CurrentYear { get; private set; }
        public int CurrentMonth { get; private set; }
        public int MonthsPerYear  { get; private set; }

        public GameClockService(int startYear = 3599, int startMonth = 1, int monthsPerYear = 12)
        {
            CurrentYear = startYear;
            CurrentMonth = startMonth;
            MonthsPerYear = monthsPerYear;
        }

        public void AdvanceTurn()
        {
            CurrentMonth++;
            if (CurrentMonth > MonthsPerYear)
            {
                CurrentMonth = 1;
                CurrentYear++;
            }
        }
    }
}