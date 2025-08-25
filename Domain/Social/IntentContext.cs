using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Domain.Social
{
    /// <summary>Shared data available to intent rules for a single actor.</summary>
    public sealed class IntentContext
    {
        public Character Actor { get; }
        public Guid ActorFactionId { get; }
        public FactionStatus FactionStatus { get; }
        public Guid? ActorSystemId { get; }
        public SystemSecurity? SystemSecurity { get; }
        public Guid? ActorLeaderId { get; }
        public IReadOnlyList<Character> SameFactionCharacters { get; }
        public IReadOnlyList<Character> OtherFactionCharacters { get; }
        public IReadOnlyList<Character> Captives { get; }
        public CharacterAmbition Ambition { get; }
        public (double Court, double Family, double Spy, double Bribe, double Recruit, double Defect, double Negotiate, double Quarrel, double Assassinate, double Torture, double Rape, double Travel, double BecomePirate, double RaidConvoy, double FoundHouse, double FoundPirateClan, double ExpelFromHouse, double ClaimPlanet) AmbitionBias { get; }
        public Func<Guid, int> OpinionOf { get; }
        public Func<Guid, Guid> FactionOf { get; }
        public PlannerConfig Config { get; }

        public IntentContext(
            Character actor,
            Guid actorFactionId,
            FactionStatus factionStatus,
            Guid? actorSystemId,
            SystemSecurity? systemSecurity,
            Guid? actorLeaderId,
            IReadOnlyList<Character> sameFactionCharacters,
            IReadOnlyList<Character> otherFactionCharacters,
            IReadOnlyList<Character> captives,
            CharacterAmbition ambition,
            (double Court, double Family, double Spy, double Bribe, double Recruit, double Defect, double Negotiate, double Quarrel, double Assassinate, double Torture, double Rape, double Travel, double BecomePirate, double RaidConvoy, double FoundHouse, double FoundPirateClan, double ExpelFromHouse, double ClaimPlanet) ambitionBias,
            Func<Guid, int> opinionOf,
            Func<Guid, Guid> factionOf,
            PlannerConfig config)
        {
            Actor = actor;
            ActorFactionId = actorFactionId;
            FactionStatus = factionStatus;
            ActorSystemId = actorSystemId;
            SystemSecurity = systemSecurity;
            ActorLeaderId = actorLeaderId;
            SameFactionCharacters = sameFactionCharacters;
            OtherFactionCharacters = otherFactionCharacters;
            Captives = captives;
            Ambition = ambition;
            AmbitionBias = ambitionBias;
            OpinionOf = opinionOf;
            FactionOf = factionOf;
            Config = config;
        }
    }
}
