using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Domain.Services
{
    public interface IMoraleService
    {
        void AdjustMoraleForConquest(Guid id, Planet planet);
        void AdjustMoraleForDefeat(Guid defenderCmdId, BattleResult result);
        void AdjustMoraleForVictory(Guid id, BattleResult result);
        void ApplyMoraleEffects();
    }
}