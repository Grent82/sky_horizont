using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Moq;
using Xunit;

using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Shared;
using SkyHorizont.Infrastructure.DomainServices;

namespace SkyHorizont.Tests.Battle
{
    public class BattleSimulatorTests
    {
        private readonly Guid _attackerFaction = Guid.NewGuid();
        private readonly Guid _defenderFaction = Guid.NewGuid();
        private readonly Guid _system = Guid.NewGuid();

        // --- helpers wired for most tests ---
        private static Fleet MakeFleet(Guid faction, Guid system, int ships, double atkPerShip, double defPerShip, bool withCharacter = false, Character? character = null)
        {
            var piracy = new PiracyService(new Mock<IFactionService>().Object, new RandomService(0), Guid.NewGuid());
            var f = new Fleet(Guid.NewGuid(), faction, system, piracy);
            for (int i = 0; i < ships; i++)
                f.AddShip(ShipFactory.Create(atkPerShip, defPerShip));
            if (withCharacter && character != null)
                f.AssignCharacter(character.Id);
            return f;
        }

        private static Planet MakePlanet(Guid system, Guid faction, double baseDef = 0, int troops = 0)
        {
            var chrRepo = new Mock<ICharacterRepository>().Object;
            var planetRepo = new Mock<IPlanetRepository>().Object;
            return new Planet(Guid.NewGuid(), "Gaia", system, faction, new Resources(100,100,100), chrRepo, planetRepo,
                initialStability: 1.0, infrastructureLevel: 10, baseAtk: 0, baseDef: baseDef, troops: troops);
        }

        // ---------------- Fleet Battle: no defenders -> trivial attacker win ----------------
        [Fact]
        public void SimulateFleetBattle_NoDefenders_AttackerWins_Trivial()
        {
            // Arrange
            var rand = new StubRandomService(always: 0.0);
            var characterRepo = new Mock<ICharacterRepository>(MockBehavior.Strict);
            var sim = new BattleSimulator(characterRepo.Object, rand);

            var attacker = MakeFleet(_attackerFaction, _system, ships: 2, atkPerShip: 10, defPerShip: 10);
            var defenders = Array.Empty<Fleet>();

            // Act
            var result = sim.SimulateFleetBattle(attacker, defenders);

            // Assert
            result.AttackerWins.Should().BeTrue();
            result.WinnerFleet.Should().Be(attacker);
            result.LoserFleet.Should().BeNull(); // none
            result.WinningFactionId.Should().Be(_attackerFaction);
            result.LosingFactionId.Should().Be(Guid.Empty);
            result.DefenseRetreated.Should().BeFalse();
            result.OutcomeMerit.Should().Be(50);
            result.LootCredits.Should().BeGreaterThan(0);
        }

        // ---------------- Fleet Battle: attacker wins against defenders ----------------
        [Fact]
        public void SimulateFleetBattle_AttackerWins_ReducesDefenderShips()
        {
            // Arrange
            var rand = new StubRandomService(always: 0.99); // retreat check won't matter if attacker wins
            var characterRepo = new Mock<ICharacterRepository>(MockBehavior.Strict);
            var sim = new BattleSimulator(characterRepo.Object, rand);

            var attacker = MakeFleet(_attackerFaction, _system, ships: 3, atkPerShip: 30, defPerShip: 20);
            var defender = MakeFleet(_defenderFaction, _system, ships: 5, atkPerShip: 5, defPerShip: 5);

            var defenders = new[] { defender };
            int before = defender.Ships.Count;

            // Act
            var result = sim.SimulateFleetBattle(attacker, defenders);

            // Assert
            result.AttackerWins.Should().BeTrue();
            result.WinnerFleet.Should().Be(attacker);
            result.LoserFleet.Should().BeNull(); // loserFleet is null when attacker wins
            defender.Ships.Count.Should().Be(0, "no retreat => full destruction path in ComputeLostShips");
            before.Should().BeGreaterThan(0);
        }

        // ---------------- Fleet Battle: attacker loses and defenders DO NOT retreat ----------------
        [Fact]
        public void SimulateFleetBattle_AttackerLoses_NoRetreat_FullDestructionAppliedToDefendersList()
        {
            // Arrange: attacker weaker
            var rand = new StubRandomService(always: 0.99); // high -> fails any retreat chance
            var characterRepo = new Mock<ICharacterRepository>(MockBehavior.Strict);
            var sim = new BattleSimulator(characterRepo.Object, rand);

            var attacker = MakeFleet(_attackerFaction, _system, ships: 2, atkPerShip: 5, defPerShip: 5);
            var defender = MakeFleet(_defenderFaction, _system, ships: 4, atkPerShip: 25, defPerShip: 25);
            int before = defender.Ships.Count;

            // Act
            var result = sim.SimulateFleetBattle(attacker, new[] { defender });

            // Assert
            result.AttackerWins.Should().BeFalse();
            result.DefenseRetreated.Should().BeFalse();
            // No retreat => defenders lose all in ComputeLostShips (from their list)
            defender.Ships.Count.Should().Be(0);
            result.LoserFleet.Should().Be(attacker);
            result.WinnerFleet.Should().BeNull();
            result.WinningFactionId.Should().Be(_defenderFaction);
            result.OutcomeMerit.Should().Be(10);
            before.Should().BeGreaterThan(0);
        }

        // ---------------- Character attack/defense modifiers are applied when AssignedCharacterId present ----------------
        [Fact]
        public void CharacterAttackAndDefenseModifiers_AreUsed_WhenAssignedCharacterPresent()
        {
            // Arrange: commander gives +0.5 attack, +0.5 defense (via Character methods)
            var repo = new Mock<ICharacterRepository>(MockBehavior.Strict);
            var commanderAtk = CharacterFactory.Create(retreatModifier: 0.0, attackBonus: 0.5, defenseBonus: 0.0);
            var commanderDef = CharacterFactory.Create(retreatModifier: 0.0, attackBonus: 0.0, defenseBonus: 0.5);

            repo.Setup(r => r.GetById(commanderAtk.Id)).Returns(commanderAtk);
            repo.Setup(r => r.GetById(commanderDef.Id)).Returns(commanderDef);

            var sim = new BattleSimulator(repo.Object, new StubRandomService(always: 0.99));

            // make attacker with commanderAtk and defender with commanderDef
            var attacker = MakeFleet(_attackerFaction, _system, ships: 2, atkPerShip: 20, defPerShip: 10, withCharacter: true, character: commanderAtk);
            var defender = MakeFleet(_defenderFaction, _system, ships: 2, atkPerShip: 20, defPerShip: 10, withCharacter: true, character: commanderDef);

            // Act
            var result = sim.SimulateFleetBattle(attacker, new[] { defender });

            // Assert: not validating exact numeric outcome (loop dampens), but we hit both modifiers & repo.GetById
            repo.Verify(r => r.GetById(commanderAtk.Id), Times.AtLeastOnce);
            repo.Verify(r => r.GetById(commanderDef.Id), Times.AtLeastOnce);
            result.BattleId.Should().NotBe(Guid.Empty);
        }

        // ---------------- Planet Conquest: no stationed fleets, attacker wins ----------------
        [Fact]
        public void SimulatePlanetConquest_NoFleets_AttackerWins()
        {
            var rand = new StubRandomService(always: 0.5);
            var repo = new Mock<ICharacterRepository>(MockBehavior.Strict);
            var sim = new BattleSimulator(repo.Object, rand);

            var attacker = MakeFleet(_attackerFaction, _system, ships: 3, atkPerShip: 30, defPerShip: 10);
            var planet = MakePlanet(_system, _defenderFaction, baseDef: 10, troops: 10);

            var result = sim.SimulatePlanetConquest(attacker, planet, researchAtkPct: 10, researchDefPct: 0);

            result.AttackerWins.Should().BeTrue();
            result.WinnerFleet.Should().Be(attacker);
            result.DefenseRetreated.Should().BeFalse();
            result.OccupationDurationHours.Should().Be(24);
            result.PlanetCaptureBonus.Should().Be(200);
            result.OutcomeMerit.Should().Be(100);
            result.WinningFactionId.Should().Be(_attackerFaction);
        }

        // ---------------- Planet Conquest: no stationed fleets, attacker loses ----------------
        [Fact]
        public void SimulatePlanetConquest_NoFleets_AttackerLoses()
        {
            var rand = new StubRandomService(always: 0.5);
            var repo = new Mock<ICharacterRepository>(MockBehavior.Strict);
            var sim = new BattleSimulator(repo.Object, rand);

            var attacker = MakeFleet(_attackerFaction, _system, ships: 1, atkPerShip: 5, defPerShip: 5);
            var planet = MakePlanet(_system, _defenderFaction, baseDef: 200, troops: 100);

            var result = sim.SimulatePlanetConquest(attacker, planet, researchAtkPct: 0, researchDefPct: 0.2);

            result.AttackerWins.Should().BeFalse();
            result.WinnerFleet.Should().BeNull();
            result.LoserFleet.Should().Be(attacker);
            result.OccupationDurationHours.Should().Be(0);
            result.PlanetCaptureBonus.Should().Be(0);
            result.OutcomeMerit.Should().Be(20);
            result.WinningFactionId.Should().Be(_defenderFaction);
        }

        // ---------------- Planet Conquest: stationed fleets AND retreat propagated from fleet battle ----------------
        [Fact]
        public void SimulatePlanetConquest_DefenderFleets_RetreatPropagates_AsAttackerWinSide()
        {
            // Arrange: make fleet battle end with defender retreat (like in the earlier test)
            var sequenceRandom = new SequenceRandomService(0.0); // ensure retreat when chance > 0
            var repo = new Mock<ICharacterRepository>(MockBehavior.Strict);
            var commander = CharacterFactory.Create(retreatModifier: 0.6, attackBonus: 0.0, defenseBonus: 0.0);
            repo.Setup(r => r.GetById(commander.Id)).Returns(commander);

            var sim = new BattleSimulator(repo.Object, sequenceRandom);

            var attacker = MakeFleet(_attackerFaction, _system, ships: 2, atkPerShip: 5, defPerShip: 5);
            var planet = MakePlanet(_system, _defenderFaction, baseDef: 50, troops: 20);

            var defendingFleet = MakeFleet(_defenderFaction, _system, ships: 4, atkPerShip: 20, defPerShip: 20,
                                           withCharacter: true, character: commander);
            planet.StationFleet(defendingFleet);

            // Act
            var result = sim.SimulatePlanetConquest(attacker, planet, researchAtkPct: 0, researchDefPct: 0);

            // Assert: even if planet battle would be tough, retreat makes attacker side the "winner" for outcome
            result.DefenseRetreated.Should().BeTrue();
            result.WinningFactionId.Should().Be(_attackerFaction);
            result.WinnerFleet.Should().Be(attacker);
            result.LoserFleet.Should().Be(defendingFleet);
        }

        [Fact]
        public void RetreatChance_AdditiveModifiers_ClampedToOne()
        {
            var repo = new Mock<ICharacterRepository>(MockBehavior.Strict);
            var defenders = new List<Fleet>();

            for (int i = 0; i < 2; i++)
            {
                var ch = new Character(
                    Guid.NewGuid(), $"Cmd{i}", 30, 2970, 1, Sex.Male,
                    new Personality(0,0,-500,0,0),
                    new SkillSet(0,0,0,0));
                repo.Setup(r => r.GetById(ch.Id)).Returns(ch);
                var f = MakeFleet(_defenderFaction, _system, ships: 1, atkPerShip: 1, defPerShip: 1, withCharacter: true, character: ch);
                defenders.Add(f);
            }

            var sim = new BattleSimulator(repo.Object, new StubRandomService(always: 0.0));
            var mi = typeof(BattleSimulator).GetMethod("RetreatChance", BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Should().NotBeNull();
            var chance = (double)mi!.Invoke(sim, new object[] { defenders, 1.0, 1.0 });
            chance.Should().Be(1.0);
        }
    }

    // ===================== Test helpers =====================

    // Deterministic IRandomService
    internal sealed class StubRandomService : IRandomService
    {
        private readonly double _doubleValue;
        private readonly int _intValue;
        public StubRandomService(double always) => _doubleValue = always;
        public StubRandomService(int always) => _intValue = always;

        public int CurrentSeed => throw new NotImplementedException();

        public double NextDouble() => _doubleValue;

        public int NextInt(int minInclusive, int maxExclusive) => _intValue;

        public void Reseed(int seed)
        {
            
        }
    }

    // Sequence-based random (uses the first value repeatedly)
    internal sealed class SequenceRandomService : IRandomService
    {
        private readonly double _val;
        private readonly int _intValue;
        public SequenceRandomService(params double[] values) => _val = values.Length > 0 ? values[0] : 0.0;
        public SequenceRandomService(int always) => _intValue = always;

        public int CurrentSeed => 0;

        public double NextDouble() => _val;

        public int NextInt(int minInclusive, int maxExclusive) => _intValue;

        public void Reseed(int seed)
        {
            
        }
    }

    internal static class ShipFactory
    {
        public static Ship Create(double atk, double def)
        {
            // If your Ship type differs, change this creation accordingly:
            // - set CurrentAttack/CurrentDefense properties after construction if they are settable,
            // - or use the real ctor signature you have.
            return new Ship(Guid.NewGuid(), ShipClass.Scout, atk, def, cargoCapacity: 0, speed: 1, cost: 0);
        }
    }

    internal static class CharacterFactory
    {
        public static Character Create(double retreatModifier, double attackBonus, double defenseBonus)
        {
            var id = Guid.NewGuid();

            var skills = new SkillSet(Military: 0, Intelligence: 0, Research: 0, Economy: 0);

            var extraversion = (int)Math.Clamp(
            Math.Round(50 - (retreatModifier / 0.002)),
            0, 100);

            var personality = new Personality(
                Openness: 50, Conscientiousness: 50, Extraversion: extraversion, Agreeableness: 50, Neuroticism: 50
            );

            return new Character(
                id: id,
                name: "Test Commander",
                age: 30, birthYear: 2990, birthMonth: 1,
                sex: Sex.Male,
                personality: personality,
                skills: skills,
                initialRank: Rank.Captain,
                initialMerit: 0
            );
        }
    }
}
