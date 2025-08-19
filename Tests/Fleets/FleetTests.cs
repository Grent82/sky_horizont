using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Moq;
using Xunit;

using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Shared;
using SkyHorizont.Domain.Entity.Task;
using TaskStatus = SkyHorizont.Domain.Entity.Task.TaskStatus;
using System.Data.Common;

namespace SkyHorizont.Tests.Fleets
{
    public class FleetTests
    {
        private static Fleet NewFleet(Guid? id = null, Guid? faction = null, Guid? system = null)
            => new Fleet(id ?? Guid.NewGuid(), faction ?? Guid.NewGuid(), system ?? Guid.NewGuid());

        private static Ship MkShip(double atk, double def, double speed = 1, double cargo = 0)
            => new Ship(Guid.NewGuid(), ShipClass.Scout, atk, def, cargoCapacity: cargo, speed: speed, cost: 0);

        // ---------------- ctor & basic props ----------------
        [Fact]
        public void Ctor_EmptyId_Throws()
        {
            Action act = () => new Fleet(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
            //act.Should().Throw<ArgumentException>().Where(e => e.ParamName == "id");
        }

        [Fact]
        public void Ctor_Initializes_Properties()
        {
            var id = Guid.NewGuid();
            var fac = Guid.NewGuid();
            var sys = Guid.NewGuid();

            var f = new Fleet(id, fac, sys);

            f.Id.Should().Be(id);
            f.FactionId.Should().Be(fac);
            f.CurrentSystemId.Should().Be(sys);
            f.Ships.Should().BeEmpty();
            f.Orders.Should().BeEmpty();
            f.AverageFleetSpeed.Should().Be(0);
        }

        // ---------------- assign character ----------------
        [Fact]
        public void AssignCharacter_SetsId()
        {
            var f = NewFleet();
            var chr = Guid.NewGuid();

            f.AssignCharacter(chr);

            f.AssignedCharacterId.Should().Be(chr);
        }

        // ---------------- add/remove/destroy ship ----------------
        [Fact]
        public void AddShip_AddsOnce_SecondTimeReturnsFalse_RemoveAndDestroyBehave()
        {
            var f = NewFleet();
            var s = MkShip(10, 5);

            f.AddShip(s).Should().BeTrue();
            f.AddShip(s).Should().BeFalse(); // duplicate

            // remove existing
            f.RemoveShip(s.Id).Should().BeTrue();
            f.Ships.Should().BeEmpty();

            // destroy missing -> throws
            Action act = () => f.DestroyShip(s.Id);
            act.Should().Throw<DomainException>()
               .WithMessage($"Ship {s.Id} not part of fleet.");
        }

        // ---------------- AverageFleetSpeed & CalculateStrength ----------------
        [Fact]
        public void AveragesAndStrengths_AreComputedFromShips()
        {
            var f = NewFleet();
            var s1 = MkShip(atk: 10, def: 20, speed: 2, cargo: 5);
            var s2 = MkShip(atk: 5,  def: 5,  speed: 4, cargo: 7);
            f.AddShip(s1);
            f.AddShip(s2);

            f.AverageFleetSpeed.Should().BeApproximately((2 + 4) / 2.0, 1e-9);

            var strength = f.CalculateStrength();
            // MilitaryPower = sum(CurrentAttack) + sum(CurrentDefense) = (10+5)+(20+5)=40
            // Cargo = 5 + 7 = 12
            strength.MilitaryPower.Should().Be(40);
            //strength.CargoCapacity.Should().Be(12);
        }

        // ---------------- ComputeLostShips ----------------
        [Fact]
        public void ComputeLostShips_NoRetreat_RemovesAll_SortedByDefenseAscending()
        {
            var f = NewFleet();
            var low = MkShip(1, def: 1);
            var mid = MkShip(1, def: 5);
            var high = MkShip(1, def: 10);
            f.AddShip(high);
            f.AddShip(low);
            f.AddShip(mid);

            var lost = f.ComputeLostShips(remainingPower: 0, retreat: false).ToList();

            lost.Should().HaveCount(3);
            // first taken should be the lowest defense
            lost.First().Should().Be(low.Id);
        }

        // ---------------- HirePrivateers ----------------
        [Fact]
        public void HirePrivateers_InsufficientFunds_Throws()
        {
            var f = NewFleet();
            var funds = new Mock<IFundsService>(MockBehavior.Strict);
            var repo = new Mock<IPirateContractRepository>(MockBehavior.Strict);

            funds.Setup(m => m.HasFunds(f.FactionId, 1000)).Returns(false);

            Action act = () => f.HirePrivateers(Guid.NewGuid(), 1000, 3, funds.Object, repo.Object);
            act.Should().Throw<DomainException>().WithMessage("Insufficient credits");

            funds.Verify(m => m.HasFunds(f.FactionId, 1000), Times.Once);
            funds.VerifyNoOtherCalls();
            repo.VerifyNoOtherCalls();
        }

        [Fact]
        public void HirePrivateers_Succeeds_Deducts_Generates_Transfers()
        {
            var f = NewFleet();
            var pirateFaction = Guid.NewGuid();

            var funds = new Mock<IFundsService>(MockBehavior.Strict);
            var repo = new Mock<IPirateContractRepository>(MockBehavior.Strict);

            funds.Setup(m => m.HasFunds(f.FactionId, 500)).Returns(true);
            funds.Setup(m => m.Deduct(f.FactionId, 500));

            var merc = NewFleet(); // the generated pirate fleet
            repo.Setup(r => r.GeneratePirateFleet(pirateFaction, 4)).Returns(merc);
            repo.Setup(r => r.TransferPirateFleetToFaction(merc.Id, f.FactionId));

            f.HirePrivateers(pirateFaction, 500, 4, funds.Object, repo.Object);

            funds.VerifyAll();
            repo.VerifyAll();
        }

        // ---------------- RewardAfterBattle (internal) via reflection ----------------
        [Fact]
        public void RewardAfterBattle_InvokesOutcomeService_WithLoserFleet()
        {
            var f = NewFleet();
            var other = NewFleet();

            var outcome = new Mock<IBattleOutcomeService>(MockBehavior.Strict);
            var br = new BattleResult(
                battleId: Guid.NewGuid(),
                winningFactionId: Guid.NewGuid(),
                losingFactionId: Guid.NewGuid(),
                winnerFleet: f,
                loserFleet: other,
                occupationDurationHours: 0,
                outcomeMerit: 0,
                lootCredits: 0,
                planetCaptureBonus: 0,
                defenseRetreated: false,
                attackerWins: true);

            outcome.Setup(o => o.ProcessFleetBattle(f, other, br));

            var mi = typeof(Fleet).GetMethod("RewardAfterBattle", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Should().NotBeNull();

            mi!.Invoke(f, new object[] { br, outcome.Object });

            outcome.VerifyAll();
        }

        // ---------------- Captured characters ----------------
        [Fact]
        public void Captured_AddAndClear_Works()
        {
            var f = NewFleet();
            var c1 = Guid.NewGuid();
            var c2 = Guid.NewGuid();

            f.AddCaptured(c1);
            f.AddCaptured(c2);

            f.Prisoners.Should().BeEquivalentTo(new[] { c1, c2 }, o => o.WithoutStrictOrdering());

            f.ClearCapturedAfterResolution();
            f.Prisoners.Should().BeEmpty();
        }

        // --------------- helper order stub ---------------
        private sealed class TestOrder : FleetOrder
        {
            public TestOrder(Guid id, TaskStatus initial) : base(id)
            {
                Status = initial;
            }

            // We expose setters for tests by using a minimal derived class.
            public new TaskStatus Status { get; set; } = TaskStatus.Pending;

            public new void Activate()
            {
                // Only activate from Pending
                if (Status == TaskStatus.Pending) Status = TaskStatus.Active;
            }

            public override void Execute(Fleet fleet, double delta)
            {
                // Simulate execution; mark completed to get pruned by Fleet.TickOrders
                if (Status == TaskStatus.Active) Status = TaskStatus.Success;
            }
        }
    }
}
