using SkyHorizont.Domain.Entity.Task;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Application.Tasks
{
    public sealed class TaskEffectSink : ITaskEffectSink
    {
        private readonly IResearchService _research;
        private readonly IIntelService _intel;
        private readonly IPlanetService _planets;
        private readonly IEconomyService _economy;

        public TaskEffectSink(
            IResearchService research,
            IIntelService intel,
            IPlanetService planets,
            IEconomyService economy)
        {
            _research = research;
            _intel = intel;
            _planets = planets;
            _economy = economy;
        }

        public void Publish(TaskEffect effect)
        {
            switch (effect)
            {
                case ResearchUnlockEffect r:
                    _research.AddProgress(r.CharacterId, r.TechnologyName, r.ResearchPoints);
                    break;

                case IntelReportEffect i:
                    _intel.RecordReport(i.CharacterId, i.TargetFactionId, i.Summary, i.IntelValue, i.CompletedYear, i.CompletedMonth);
                    break;

                case GovernanceChangeEffect g:
                    _planets.AdjustStability(g.PlanetId, g.StabilityDelta);
                    _economy.CreditPlanetBudget(g.PlanetId, g.CreditsGenerated);
                    break;
            }
        }
    }
}
