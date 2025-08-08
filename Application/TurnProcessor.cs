using SkyHorizont.Domain.Services;

namespace SkyHorizont.Application
{
    public interface ITurnProcessor
    {
        void ProcessAllTurnEvents(int turnNumber);
    }

    public class TurnProcessor : ITurnProcessor
    {
        private readonly IGameClockService _clock;
        private readonly IAffectionService _affection;
        private readonly IRansomService _ransom;
        private readonly IMoraleService _morale;
        private readonly ICharacterLifecycleService _lifecycle;

        public TurnProcessor(IGameClockService clock, IAffectionService affectionService, IRansomService transom, IMoraleService morale, ICharacterLifecycleService lifecycle)
        {
            _clock = clock;
            _affection = affectionService;
            _ransom = transom;
            _morale = morale;
            _lifecycle = lifecycle;
        }

        public void ProcessAllTurnEvents(int turn)
        {
             _clock.AdvanceTurn();
            _ransom.TryRequestRansoms();
            _affection.UpdateAffection();
            _morale.ApplyMoraleEffects();
            _lifecycle.ProcessLifecycleTurn();
            // ... other per-turn events
        }
    }

}