using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Application
{
    public interface ITurnProcessor
    {
        void ProcessAllTurnEvents(int turnNumber);
    }

    public class TurnProcessor : ITurnProcessor
    {
        private readonly IAffectionService _affection;
        private readonly IRansomService _ransom;
        private readonly IMoraleService _morale;

        public TurnProcessor(IAffectionService affectionService, IRansomService transom, IMoraleService morale)
        {
            _affection = affectionService;
            _ransom = transom;
            _morale = morale;
        }

        public void ProcessAllTurnEvents(int turn)
        {
            _ransom.TryRequestRansoms();
            _affection.UpdateAffection();
            _morale.ApplyMoraleEffects();
            // ... other per-turn events
        }
    }

}