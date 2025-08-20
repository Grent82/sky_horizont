using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Fleets;

namespace SkyHorizont.Domain.Travel
{
    public sealed record SystemSecurity(Guid SystemId, int PatrolStrength, int PirateActivity, int Traffic);

    public interface IRiskService
    {
        BattleResult ResolveBattle(Fleet pirates, Fleet transport);
    }
}