namespace SkyHorizont.Tests.Entity
{
    public class EntityLineageTests
    {
        [Fact]
        public void Ctor_EmptyGuid_Throws()
        {
            Action act = () => new EntityLineage(Guid.Empty);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*CharacterId must be non‑empty*");
        }

        [Fact]
        public void Ctor_ValidGuid_Initializes()
        {
            var child = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            lineage.CharacterId.Should().Be(child);
            lineage.BiologicalFatherId.Should().BeNull();
            lineage.BiologicalMotherId.Should().BeNull();
            lineage.AdoptiveParentIds.Should().BeEmpty();
            lineage.ToString().Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void SetBiologicalFather_Self_Throws()
        {
            var child = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            Action act = () => lineage.SetBiologicalFather(child);
            act.Should().Throw<DomainException>()
               .WithMessage("Cannot assign self as a parent.");
        }

        [Fact]
        public void SetBiologicalMother_Self_Throws()
        {
            var child = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            Action act = () => lineage.SetBiologicalMother(child);
            act.Should().Throw<DomainException>()
               .WithMessage("Cannot assign self as a parent.");
        }

        [Fact]
        public void SamePersonAsFatherAndMother_Throws_WhenFatherSetFirst()
        {
            var child = Guid.NewGuid();
            var parent = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            lineage.SetBiologicalFather(parent);
            Action act = () => lineage.SetBiologicalMother(parent);

            act.Should().Throw<DomainException>()
               .WithMessage("Biological father and mother cannot be the same character.");
        }

        [Fact]
        public void SamePersonAsFatherAndMother_Throws_WhenMotherSetFirst()
        {
            var child = Guid.NewGuid();
            var parent = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            lineage.SetBiologicalMother(parent);
            Action act = () => lineage.SetBiologicalFather(parent);

            act.Should().Throw<DomainException>()
               .WithMessage("Biological father and mother cannot be the same character.");
        }

        [Fact]
        public void SetBiologicalParents_SetsBoth_WhenValid()
        {
            var child = Guid.NewGuid();
            var father = Guid.NewGuid();
            var mother = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            lineage.SetBiologicalParents(father, mother);

            lineage.BiologicalFatherId.Should().Be(father);
            lineage.BiologicalMotherId.Should().Be(mother);
        }

        [Fact]
        public void SetBiologicalFather_Null_ClearsFatherOnly()
        {
            var child = Guid.NewGuid();
            var father = Guid.NewGuid();
            var mother = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            lineage.SetBiologicalParents(father, mother);
            lineage.SetBiologicalFather(null);

            lineage.BiologicalFatherId.Should().BeNull();
            lineage.BiologicalMotherId.Should().Be(mother);
        }

        [Fact]
        public void SetBiologicalMother_Null_ClearsMotherOnly()
        {
            var child = Guid.NewGuid();
            var father = Guid.NewGuid();
            var mother = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            lineage.SetBiologicalParents(father, mother);
            lineage.SetBiologicalMother(null);

            lineage.BiologicalFatherId.Should().Be(father);
            lineage.BiologicalMotherId.Should().BeNull();
        }

        [Fact]
        public void ClearBiologicalParents_RemovesBoth()
        {
            var child = Guid.NewGuid();
            var father = Guid.NewGuid();
            var mother = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            lineage.SetBiologicalParents(father, mother);
            lineage.ClearBiologicalParents();

            lineage.BiologicalFatherId.Should().BeNull();
            lineage.BiologicalMotherId.Should().BeNull();
        }

        [Fact]
        public void AddAdoptiveParent_Self_Throws()
        {
            var child = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            Action act = () => lineage.AddAdoptiveParent(child);
            act.Should().Throw<DomainException>()
               .WithMessage("Cannot assign self as a parent.");
        }

        [Fact]
        public void AddAdoptiveParent_Adds_Unique_And_IgnoresDuplicates()
        {
            var child = Guid.NewGuid();
            var a = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            lineage.AddAdoptiveParent(a);
            lineage.AddAdoptiveParent(a); // duplicate

            lineage.AdoptiveParentIds.Should().HaveCount(1);
            lineage.AdoptiveParentIds.Should().Contain(a);
        }

        [Fact]
        public void RemoveAdoptiveParent_Removes_And_NoOpWhenMissing()
        {
            var child = Guid.NewGuid();
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var lineage = new EntityLineage(child);

            lineage.AddAdoptiveParent(a);
            lineage.RemoveAdoptiveParent(b); // not present
            lineage.AdoptiveParentIds.Should().ContainSingle().Which.Should().Be(a);

            lineage.RemoveAdoptiveParent(a);
            lineage.AdoptiveParentIds.Should().BeEmpty();
        }

        [Fact]
        public void GetParents_WithoutAdoptive_ReturnsOnlyBio_IfSet()
        {
            var child = Guid.NewGuid();
            var father = Guid.NewGuid();
            var mother = Guid.NewGuid();
            var lineage = new EntityLineage(child);
            lineage.SetBiologicalParents(father, mother);
            lineage.AddAdoptiveParent(Guid.NewGuid()); // should be ignored

            var parents = lineage.GetParents(includeAdoptive: false);

            parents.Should().HaveCount(2);
            parents.Should().Contain(p => p.ParentId == father && p.Type == LineageType.Biological);
            parents.Should().Contain(p => p.ParentId == mother && p.Type == LineageType.Biological);
        }

        [Fact]
        public void GetParents_WithAdoptive_ReturnsBioPlusAdoptive()
        {
            var child = Guid.NewGuid();
            var father = Guid.NewGuid();
            var mother = Guid.NewGuid();
            var a1 = Guid.NewGuid();
            var a2 = Guid.NewGuid();

            var lineage = new EntityLineage(child);
            lineage.SetBiologicalParents(father, mother);
            lineage.AddAdoptiveParent(a1);
            lineage.AddAdoptiveParent(a2);

            var parents = lineage.GetParents(includeAdoptive: true);

            // We ignore ordering for adoptives (HashSet iteration order is unspecified)
            parents.Should().HaveCount(4);

            var bio = parents.Where(p => p.Type == LineageType.Biological).Select(p => p.ParentId).ToHashSet();
            bio.Should().BeEquivalentTo(new HashSet<Guid> { father, mother });

            var adopt = parents.Where(p => p.Type == LineageType.Adoptive).Select(p => p.ParentId).ToHashSet();
            adopt.Should().BeEquivalentTo(new HashSet<Guid> { a1, a2 });
        }
    }
}
