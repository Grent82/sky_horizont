using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Intrigue;

namespace SkyHorizont.Application
{
    public interface ITurnProcessor
    {
        void ProcessAllTurnEvents();
    }

    /// <summary>
    /// Orchestrates the monthly turn:
    /// Clock → Lifecycle → Social (plan/resolve) → Affection → Morale → Intrigue → Economy.
    /// </summary>
    public sealed class TurnProcessor : ITurnProcessor
    {
        private readonly IGameClockService _clock;
        private readonly ICharacterRepository _characters;
        private readonly IIntentPlanner _planner;
        private readonly IInteractionResolver _resolver;
        private readonly ISocialEventLog _socialLog;
        private readonly IAffectionService _affection;
        private readonly IMoraleService _morale;
        private readonly ICharacterLifecycleService _lifecycle;
        private readonly IIntrigueService _intrigue;
        private readonly IEconomyService _economy;

        public TurnProcessor(
            IGameClockService clock,
            ICharacterRepository characters,
            IIntentPlanner planner,
            IInteractionResolver resolver,
            ISocialEventLog socialLog,
            IAffectionService affectionService,
            IMoraleService morale,
            ICharacterLifecycleService lifecycle,
            IIntrigueService intrigue,
            IEconomyService economy)
        {
            _clock      = clock;
            _characters = characters;

            _planner   = planner;
            _resolver  = resolver;
            _socialLog = socialLog;

            _affection = affectionService;
            _morale    = morale;
            _lifecycle = lifecycle;

            _intrigue  = intrigue;
            _economy   = economy;
        }

        public void ProcessAllTurnEvents()
        {
            // 1) Advance game time (month → year rollover if needed)
            SafeRun("Clock.AdvanceTurn", () => _clock.AdvanceTurn());

            // 2) Lifecycle first (age up, pregnancies, births, deaths)
            SafeRun("Lifecycle.Process", () => _lifecycle.ProcessLifecycleTurn());

            // 3) Social layer: plan intents per living character and resolve them
            SafeRun("Social.Intents", () =>
            {
                _planner.ClearCaches();
                _resolver.ClearCaches();
                foreach (var actor in _characters.GetLiving())
                {
                    var intents = _planner.PlanMonthlyIntents(actor);
                    foreach (var intent in intents)
                    {
                        try
                        {
                            var events = _resolver.Resolve(intent, _clock.CurrentYear, _clock.CurrentMonth);
                            foreach (var ev in events)
                                _socialLog.Append(ev);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TurnProcessor] Error resolving intent {intent.Type} for {actor.Id}: {ex}");
                            throw;
                        }
                    }
                }
            });

            // 4) Affection drift / captive affection adjustments (monthly)
            SafeRun("Affection.Update", () => _affection.UpdateAffection());

            // 5) Morale (apply per-fleet / per-garrison modifiers)
            SafeRun("Morale.Apply", () => _morale.ApplyMoraleEffects());

            // 6) Intrigue (plots progress, exposure, recruitment, defections, blackmail)
            SafeRun("Intrigue.TickPlots", () => _intrigue.TickPlots());

            // 7) Economy (upkeep, trade/tariffs/smuggling, loans)
            SafeRun("Economy.EndOfTurnUpkeep", () => _economy.EndOfTurnUpkeep());
        }

        private static void SafeRun(string label, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                Console.WriteLine($"[TurnProcessor] {label} failed: {ex.Message}");
                throw;
            }
        }
    }
}
