using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Tests.Entity
{
    public class CharacterTests
    {
        private static Character CreateCharacter(
            Sex sex = Sex.Male,
            Rank rank = Rank.Civilian,
            int merit = 0)
        {
            return new Character(
                id: Guid.NewGuid(),
                name: "Test",
                age: 30,
                birthYear: 2970,
                birthMonth: 1,
                sex: sex,
                personality: new Personality(0,0,0,0,0),
                skills: new SkillSet(0,0,0,0),
                initialRank: rank,
                initialMerit: merit);
        }

        [Fact]
        public void GainMerit_CrossesMultipleThresholds_PromotesThroughRanks()
        {
            var c = CreateCharacter();
            c.GainMerit(4000);
            c.Rank.Should().Be(Rank.General);
        }

        [Fact]
        public void GainMerit_Leader_NoFurtherPromotion()
        {
            var c = CreateCharacter(rank: Rank.Leader);
            c.GainMerit(5000);
            c.Rank.Should().Be(Rank.Leader);
        }

        [Fact]
        public void StartPregnancy_Male_Throws()
        {
            var m = CreateCharacter(sex: Sex.Male);
            Action act = () => m.StartPregnancy(Guid.NewGuid(), 3000, 1);
            act.Should().Throw<DomainException>().WithMessage("Only female characters can be pregnant.");
        }

        [Fact]
        public void StartPregnancy_AlreadyPregnant_Throws()
        {
            var f = CreateCharacter(sex: Sex.Female);
            f.StartPregnancy(Guid.NewGuid(), 3000, 1);
            Action act = () => f.StartPregnancy(Guid.NewGuid(), 3000, 2);
            act.Should().Throw<DomainException>().WithMessage("Already pregnant.");
        }

        [Fact]
        public void Pregnancy_DueDate_SpansYearBoundary()
        {
            var p = Pregnancy.Start(Guid.NewGuid(), 3000, 11, gestationMonths: 3);
            p.DueDate(12).Should().Be((3001, 2));
        }

        [Fact]
        public void ClearPregnancy_RemovesActivePregnancy()
        {
            var f = CreateCharacter(sex: Sex.Female);
            f.StartPregnancy(Guid.NewGuid(), 3000, 1);
            f.ClearPregnancy();
            f.ActivePregnancy.Should().BeNull();
        }

        [Fact]
        public void LinkFamilyMember_Empty_Throws()
        {
            var c = CreateCharacter();
            Action act = () => c.LinkFamilyMember(Guid.Empty);
            act.Should().Throw<ArgumentException>().Where(e => e.ParamName == "otherId");
        }

        [Fact]
        public void LinkFamilyMember_SelfNotAdded()
        {
            var c = CreateCharacter();
            c.LinkFamilyMember(c.Id);
            c.FamilyLinkIds.Should().BeEmpty();
        }

        [Fact]
        public void AddRelationship_EmptyId_Throws()
        {
            var c = CreateCharacter();
            Action act = () => c.AddRelationship(Guid.Empty, RelationshipType.Lover);
            act.Should().Throw<ArgumentException>().Where(e => e.ParamName == "otherCharacterId");
        }

        [Fact]
        public void AddRelationship_Self_Throws()
        {
            var c = CreateCharacter();
            Action act = () => c.AddRelationship(c.Id, RelationshipType.Lover);
            act.Should().Throw<ArgumentException>().WithMessage("Cannot create relationship with self.*");
        }

        [Fact]
        public void AddRelationship_DuplicateIgnored()
        {
            var c = CreateCharacter();
            var other = Guid.NewGuid();
            c.AddRelationship(other, RelationshipType.Lover);
            c.AddRelationship(other, RelationshipType.Spouse);
            c.Relationships.Should().HaveCount(1);
            c.Relationships.First().Type.Should().Be(RelationshipType.Lover);
        }
    }
}
