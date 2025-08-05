using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Domain.Battle
{
    public interface IBattleSimulator
    {
        BattleResult SimulateFleetBattle(Fleet attacker, Fleet defender);
        BattleResult SimulatePlanetConquest(Fleet attacker, Planet defenderPlanet,
            double researchAtkPct, double researchDefPct);
    }
}