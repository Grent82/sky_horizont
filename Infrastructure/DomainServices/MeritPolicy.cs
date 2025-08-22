using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class MeritPolicy : IMeritPolicy
    {
        private readonly MeritPolicyConfig _cfg;
        public MeritPolicy(MeritPolicyConfig? cfg = null) => _cfg = cfg ?? MeritPolicyConfig.Default;

        public int Compute(MeritAction action, MeritContext ctx)
        {
            switch (action)
            {
                case MeritAction.FamilyVisit:
                case MeritAction.LoverVisit:
                case MeritAction.TravelBooked:
                case MeritAction.Rape:
                    return 0;

                case MeritAction.Courtship:
                {
                    if (!ctx.Success)
                        return _cfg.FailSmall;
                    var m = _cfg.RoutineBase;
                    if (ctx.Ambition == CharacterAmbition.EnsureFamilyLegacy)
                        m += 2;
                    return m;
                }

                case MeritAction.Spy:
                {
                    if (!ctx.Success)
                        return _cfg.FailSmall;
                    var m = _cfg.NotableBase;
                    if (ctx.Ambition == CharacterAmbition.SeekAdventure)
                        m += 2;
                    if (ctx.IntelSeverity >= 70)
                        m += 2;
                    return m;
                }

                case MeritAction.Bribe:
                {
                    if (!ctx.Success)
                        return _cfg.FailMedium;
                    var m = _cfg.NotableBase;
                    if (ctx.Ambition == CharacterAmbition.BuildWealth)
                        m += 2;
                    return m;
                }

                case MeritAction.Recruit:
                {
                    if (!ctx.Success)
                        return _cfg.FailSmall;
                    var m = _cfg.NotableBase;
                    if (ctx.Ambition == CharacterAmbition.GainPower)
                        m += 4;
                    return m;
                }

                case MeritAction.Defect:
                {
                    if (!ctx.Success)
                        return _cfg.FailMedium;
                    var m = _cfg.NotableBase;
                    if (ctx.Ambition == CharacterAmbition.GainPower)
                        m += 4;
                    return m;
                }

                case MeritAction.Negotiate:
                {
                    if (!ctx.Success)
                        return _cfg.FailTiny;
                    var m = _cfg.NotableBase;
                    if (ctx.Ambition == CharacterAmbition.BuildWealth)
                        m += 2;
                    if (ctx.AtWar)
                            m += 2;
                    return m;
                }

                case MeritAction.Torture:
                {
                    if (!ctx.Success || !ctx.ProducedIntel)
                        return 0;
                    var m = _cfg.TortureIntel;
                    if (ctx.IntelSeverity >= 75)
                        m += 2;
                    return m;
                }

                case MeritAction.BecomePirate:
                {
                    if (!ctx.Success)
                        return _cfg.FailMedium;
                    var m = _cfg.NotableBase;
                    if (ctx.Ambition == CharacterAmbition.SeekAdventure)
                        m += 4;
                    return m;
                }

                case MeritAction.RaidConvoy:
                {
                    if (!ctx.Success)
                        return _cfg.FailMedium;
                    var m = _cfg.NotableBase + (ctx.EnemyStrength >= 120 ? 2 : 0)
                                          + (ctx.Loot >= 150 ? 2 : 0);
                    return Math.Min(m, 12);
                }

                case MeritAction.Assassinate:
                {
                    if (!ctx.Success) return _cfg.FailLarge;
                    var major = _cfg.MajorBase;
                    if (ctx.Ambition == CharacterAmbition.GainPower)
                        major += 5;
                    if (ctx.EnemyStrength >= 140)
                        major += 3;
                    return Math.Min(major, 25);
                }

                case MeritAction.BattleSmallWin:
                    return ctx.Success ? 8 : _cfg.FailSmall;

                case MeritAction.BattleMajorWin:
                    return ctx.Success ? 18 : _cfg.FailSmall;

                case MeritAction.Legendary:
                    return ctx.Success ? 30 : 0;
                
                case MeritAction.HouseFoundedMajor:
                    return ctx.Success ? 20 : 0;
                
                case MeritAction.PirateClanFounded:
                    return ctx.Success ? 15 : 0;
                
                case MeritAction.PlanetClaimed:
                    return ctx.Success ? 20 : 0;

                default:
                    return 0;
            }
        }
    }

    public sealed class MeritPolicyConfig
    {
        public int RoutineBase { get; init; } = 1;
        public int NotableBase { get; init; } = 6;
        public int MajorBase { get; init; } = 15;
        public int TortureIntel { get; init; } = 5;
        public int FailTiny { get; init; } = -2;
        public int FailSmall { get; init; } = -3;
        public int FailMedium { get; init; } = -5;
        public int FailLarge { get; init; } = -10;

        public static MeritPolicyConfig Default => new();
    }
}
