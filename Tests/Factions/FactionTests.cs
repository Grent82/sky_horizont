using System;
using System.Linq;
using FluentAssertions;
using Xunit;

using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Tests.Factions
{
    public class FactionTests
    {
        private static (Faction a, Faction b, Guid c1, Guid c2, Guid p1) Setup()
        {
            var a = new Faction(Guid.NewGuid(), "Alpha", Guid.NewGuid());
            var b = new Faction(Guid.NewGuid(), "Beta", Guid.NewGuid());
            var c1 = Guid.NewGuid();
            var c2 = Guid.NewGuid();
            var p1 = Guid.NewGuid();
            return (a, b, c1, c2, p1);
        }

        // ---- ctor & basic props ----
        [Fact]
        public void Ctor_NullName_Throws()
        {
            Action act = () => new Faction(Guid.NewGuid(), null!, Guid.NewGuid());
            act.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "name");
        }

        [Fact]
        public void Ctor_Initializes_Collections_And_Properties()
        {
            var id = Guid.NewGuid();
            var leader = Guid.NewGuid();
            var f = new Faction(id, "Alpha", leader);

            f.Id.Should().Be(id);
            f.Name.Should().Be("Alpha");
            f.LeaderId.Should().Be(leader);
            f.CharacterIds.Should().BeEmpty();
            f.PlanetIds.Should().BeEmpty();
            f.Diplomacy.Should().BeEmpty();
        }

        // ---- characters & leader ----
        [Fact]
        public void Add_Remove_Character_And_ChangeLeader_Succeeds()
        {
            var (a, _, c1, _, _) = Setup();

            a.AddCharacter(c1);
            a.CharacterIds.Should().ContainSingle().Which.Should().Be(c1);

            // idempotent add
            a.AddCharacter(c1);
            a.CharacterIds.Should().ContainSingle();

            // leader must be member
            a.ChangeLeader(c1);
            a.LeaderId.Should().Be(c1);

            a.RemoveCharacter(c1);
            a.CharacterIds.Should().BeEmpty();
        }

        [Fact]
        public void ChangeLeader_Fails_When_NotMember()
        {
            var (a, _, _, c2, _) = Setup();

            Action act = () => a.ChangeLeader(c2);
            act.Should().Throw<DomainException>()
               .WithMessage($"Character {c2} is not part of faction {a.Name}.");
        }

        // ---- planets ----
        [Fact]
        public void ConquerPlanet_Adds_And_Duplicate_Throws()
        {
            var (a, _, _, _, p1) = Setup();

            a.ConquerPlanet(p1);
            a.PlanetIds.Should().ContainSingle().Which.Should().Be(p1);

            Action again = () => a.ConquerPlanet(p1);
            again.Should().Throw<DomainException>()
                 .WithMessage($"Faction {a.Name} already owns planet {p1}.");
        }

        [Fact]
        public void LosePlanet_Removes_IfPresent_NoThrow_WhenMissing()
        {
            var (a, _, _, _, p1) = Setup();

            a.ConquerPlanet(p1);
            a.LosePlanet(p1);
            a.PlanetIds.Should().BeEmpty();

            // losing again is a no-op
            a.LosePlanet(p1);
            a.PlanetIds.Should().BeEmpty();
        }

        // ---- diplomacy ----
        [Fact]
        public void ProposeDiplomacy_WithSelf_Throws()
        {
            var (a, _, _, _, _) = Setup();
            Action act = () => a.ProposeDiplomacy(a, +10);
            act.Should().Throw<DomainException>()
               .WithMessage("Cannot modify diplomacy with self.");
        }

        [Fact]
        public void ProposeDiplomacy_CreatesStanding_Then_Adjusts_And_Clamps()
        {
            var (a, b, _, _, _) = Setup();

            // no standing yet
            a.Diplomacy.ContainsKey(b.Id).Should().BeFalse();
            a.GetStandingWith(b).Value.Should().Be(0);

            // first proposal creates and clamps
            a.ProposeDiplomacy(b, 95);
            a.Diplomacy[b.Id].Value.Should().Be(95);

            // adjust upward beyond cap -> clamp to +100
            a.ProposeDiplomacy(b, +10);
            a.Diplomacy[b.Id].Value.Should().Be(100);

            // adjust downward, still in range
            a.ProposeDiplomacy(b, -30);
            a.Diplomacy[b.Id].Value.Should().Be(70);

            // query reflects stored
            a.GetStandingWith(b).Value.Should().Be(70);
        }

        [Fact]
        public void GetStandingWith_UnknownFaction_ReturnsNeutralZero_AndDoesNotCreateEntry()
        {
            var (a, b, _, _, _) = Setup();

            a.Diplomacy.Should().BeEmpty();
            var s = a.GetStandingWith(b);
            s.Value.Should().Be(0);
            a.Diplomacy.Should().BeEmpty(); // still empty; method didn't mutate
        }

        // ---- read-only exposures are copies ----
        [Fact]
        public void ExposedCollections_AreReadOnlySnapshots()
        {
            var (a, _, c1, _, p1) = Setup();

            a.AddCharacter(c1);
            a.ConquerPlanet(p1);

            var charsView = a.CharacterIds;
            var planetsView = a.PlanetIds;

            // they’re read-only; you can't cast back to mutate; just ensure contents match snapshot
            charsView.Should().ContainSingle().Which.Should().Be(c1);
            planetsView.Should().ContainSingle().Which.Should().Be(p1);

            // mutate faction; views are snapshots (won't gain new items automatically)
            var c2 = Guid.NewGuid();
            a.AddCharacter(c2);
            a.CharacterIds.Should().HaveCount(2); // live view from property
            charsView.Should().HaveCount(1);      // old snapshot remains 1
        }
    }
}
