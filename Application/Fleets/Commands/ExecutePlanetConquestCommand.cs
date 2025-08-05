using SkyHorizont.Domain.Battle;

namespace SkyHorizont.Application.Fleets.Commands
{
    public class ExecutePlanetConquestCommand
    {
        public Guid PlanetId { get; init; }
        public Guid AttackerFleetId { get; init; }
        public BattleResult BattleResult { get; set; }
    }
}
