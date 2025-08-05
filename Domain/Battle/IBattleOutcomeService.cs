using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Domain.Battle
{
    public interface IBattleOutcomeService
    {
        void ProcessFleetBattle(Fleet attacker, Fleet defender, BattleResult result);
        void ProcessPlanetConquest(Planet planet, Fleet attackerFleet, BattleResult result);
    }
}
