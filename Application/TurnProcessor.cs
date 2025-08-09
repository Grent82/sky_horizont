using System;
using System.Linq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Application
{
    public interface ITurnProcessor
    {
        void ProcessAllTurnEvents(int turnNumber);
    }

    /// <summary>
    /// Orchestrates the monthly turn: clock → lifecycle → social interactions → affection → ransom → morale.
    /// </summary>
    public sealed class TurnProcessor : ITurnProcessor
    {
        private readonly IGameClockService _clock;
        private readonly ICharacterRepository _characters;

        private readonly IIntentPlanner _planner;
        private readonly IInteractionResolver _resolver;
        private readonly ISocialEventLog _socialLog;

        private readonly IAffectionService _affection;
        private readonly IRansomService _ransom;
        private readonly IMoraleService _morale;
        private readonly ICharacterLifecycleService _lifecycle;

        public TurnProcessor(
            IGameClockService clock,
            ICharacterRepository characters,
            IIntentPlanner planner,
            IInteractionResolver resolver,
            ISocialEventLog socialLog,
            IAffectionService affectionService,
            IRansomService ransom,
            IMoraleService morale,
            ICharacterLifecycleService lifecycle)
        {
            _clock      = clock;
            _characters = characters;

            _planner  = planner;
            _resolver = resolver;
            _socialLog = socialLog;

            _affection = affectionService;
            _ransom    = ransom;
            _morale    = morale;
            _lifecycle = lifecycle;
        }

        public void ProcessAllTurnEvents(int turnNumber)
        {
            // 1) Advance game time (month → possibly year rollover)
            _clock.AdvanceTurn();

            // 2) Lifecycle first (age up, pregnancies, births, deaths)
            _lifecycle.ProcessLifecycleTurn();

            // 3) Social layer: plan intents per living character and resolve them
            foreach (var actor in _characters.GetAll().Where(c => c.IsAlive))
            {
                // Plan
                var intents = _planner.PlanMonthlyIntents(actor);

                // Resolve
                foreach (var intent in intents)
                {
                    try
                    {
                        var events = _resolver.Resolve(intent, turnNumber);
                        foreach (var ev in events)
                        {
                            _socialLog.Append(ev);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Non-fatal: log and continue. Swap to your logger if you have one.
                        Console.WriteLine($"[TurnProcessor] Error resolving intent {intent.Type} for {actor.Id}: {ex}");
                    }
                }
            }

            // 4) Affection drift / captive affection adjustments (monthly)
            _affection.UpdateAffection();

            // 5) Ransom attempts / hostage negotiations this month
            _ransom.TryRequestRansoms();

            // 6) Morale (apply per-fleet / per-garrison modifiers)
            _morale.ApplyMoraleEffects();

            // 7) (Optional) Economy, taxes, upkeep, research ticks, etc. would go here.
        }
    }
}
