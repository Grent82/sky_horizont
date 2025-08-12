using FluentAssertions;
using Xunit;

using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Tests.Entity
{
    public class FamilyTreeServiceTests
    {
        // ---------- constructor ----------
        [Fact]
        public void Ctor_NullRepo_Throws()
        {
            Action act = () => new FamilyTreeService(null!);
            act.Should().Throw<ArgumentNullException>()
               .Where(e => e.ParamName == "repo");
        }

        // ---------- GetSiblings ----------
        [Fact]
        public void GetSiblings_NoLineage_Throws()
        {
            var repo = new InMemoryLineageRepository(); // empty
            var svc = new FamilyTreeService(repo);

            var id = Guid.NewGuid();
            Action act = () => svc.GetSiblings(id);
            act.Should().Throw<DomainException>()
               .WithMessage($"Lineage not set for Character {id}");
        }

        [Fact]
        public void GetSiblings_NoParents_ReturnsEmpty()
        {
            var repo = new InMemoryLineageRepository();
            var child = Guid.NewGuid();
            repo.Upsert(new EntityLineage(child)); // no parents set

            var svc = new FamilyTreeService(repo);
            svc.GetSiblings(child).Should().BeEmpty();
        }

        [Fact]
        public void GetSiblings_BioAndAdoptive_AreConsideredDistinct_SelfExcluded_Deduped()
        {
            var repo = new InMemoryLineageRepository();

            var child = Guid.NewGuid();
            var bioFather = Guid.NewGuid();
            var bioMother = Guid.NewGuid();
            var adoptive = Guid.NewGuid();

            // subject
            var self = new EntityLineage(child);
            self.SetBiologicalParents(bioFather, bioMother);
            self.AddAdoptiveParent(adoptive);
            repo.Upsert(self);

            // siblings
            var sibSameFather = new EntityLineage(Guid.NewGuid());
            sibSameFather.SetBiologicalFather(bioFather);
            repo.Upsert(sibSameFather);

            var sibSameMother = new EntityLineage(Guid.NewGuid());
            sibSameMother.SetBiologicalMother(bioMother);
            repo.Upsert(sibSameMother);

            var sibAdoptive = new EntityLineage(Guid.NewGuid());
            sibAdoptive.AddAdoptiveParent(adoptive);
            repo.Upsert(sibAdoptive);

            // non-sibling (no shared parent)
            var stranger = new EntityLineage(Guid.NewGuid());
            stranger.SetBiologicalParents(Guid.NewGuid(), Guid.NewGuid());
            repo.Upsert(stranger);

            // duplicate candidate (appears twice in FindBySomeParentIds), ensure Distinct works
            var dup = new EntityLineage(Guid.NewGuid());
            dup.SetBiologicalFather(bioFather);
            dup.AddAdoptiveParent(adoptive);
            repo.Upsert(dup);

            var svc = new FamilyTreeService(repo);
            var result = svc.GetSiblings(child);

            result.Should().BeEquivalentTo(new[]
            {
                sibSameFather.CharacterId,
                sibSameMother.CharacterId,
                sibAdoptive.CharacterId,
                dup.CharacterId
            }, opts => opts.WithoutStrictOrdering());
            result.Should().NotContain(child);
        }

        // ---------- GetGrandparents ----------
        [Fact]
        public void GetGrandparents_NoLineage_Throws()
        {
            var repo = new InMemoryLineageRepository();
            var svc = new FamilyTreeService(repo);
            var id = Guid.NewGuid();

            Action act = () => svc.GetGrandparents(id);
            act.Should().Throw<DomainException>()
               .WithMessage($"Lineage not set for Character {id}");
        }

        [Fact]
        public void GetGrandparents_NoParents_ReturnsEmpty()
        {
            var repo = new InMemoryLineageRepository();
            var child = Guid.NewGuid();
            repo.Upsert(new EntityLineage(child));
            var svc = new FamilyTreeService(repo);

            svc.GetGrandparents(child).Should().BeEmpty();
        }

        [Fact]
        public void GetGrandparents_UsesBiologicalOnly_SkipsMissingParentLineages()
        {
            var repo = new InMemoryLineageRepository();

            var child = Guid.NewGuid();
            var father = Guid.NewGuid();
            var mother = Guid.NewGuid();

            var self = new EntityLineage(child);
            self.SetBiologicalParents(father, mother);
            repo.Upsert(self);

            // father lineage → paternal grandparents (FF, FM)
            var FF = Guid.NewGuid(); var FM = Guid.NewGuid();
            var fatherLine = new EntityLineage(father);
            fatherLine.SetBiologicalParents(FF, FM);
            repo.Upsert(fatherLine);

            // mother lineage MISSING entirely → should be silently skipped
            // if present, only biological parents would count anyway (not adoptive)

            var svc = new FamilyTreeService(repo);
            var gps = svc.GetGrandparents(child);

            gps.Should().BeEquivalentTo(new[] { FF, FM }, opts => opts.WithoutStrictOrdering());
        }

        // ---------- GetAncestors (BFS up) ----------
        [Fact]
        public void GetAncestors_BFS_IncludesBioAndAdoptive_UniqueAndDepthBounded()
        {
            var repo = new InMemoryLineageRepository();

            // Chain: A <- (bio/adoptive) B <- C, and also a diamond overlap to test uniqueness
            var A = Guid.NewGuid();
            var A2 = Guid.NewGuid();        // second parent of B to create branching
            var Adopt = Guid.NewGuid();     // adoptive parent for B
            var B = Guid.NewGuid();
            var C = Guid.NewGuid();
            var D = Guid.NewGuid();         // sibling of C sharing B

            // B has parents: A (bio father), A2 (bio mother), Adopt (adoptive)
            var lineB = new EntityLineage(B);
            lineB.SetBiologicalParents(A, A2);
            lineB.AddAdoptiveParent(Adopt);
            repo.Upsert(lineB);

            // C has parent B
            var lineC = new EntityLineage(C);
            lineC.SetBiologicalFather(B);
            repo.Upsert(lineC);

            // D has parent B too (sibling of C)
            var lineD = new EntityLineage(D);
            lineD.SetBiologicalMother(B);
            repo.Upsert(lineD);

            // A has no parents (root)
            repo.Upsert(new EntityLineage(A));
            repo.Upsert(new EntityLineage(A2));
            repo.Upsert(new EntityLineage(Adopt));

            var svc = new FamilyTreeService(repo);

            // depth=1 → only parents of C = { B }
            svc.GetAncestors(C, maxDepth: 1).Should().BeEquivalentTo(new[] { B });

            // depth=2 → parents of C plus parents of B = { B, A, A2, Adopt } (order irrelevant)
            svc.GetAncestors(C, maxDepth: 2)
               .Should().BeEquivalentTo(new[] { B, A, A2, Adopt }, o => o.WithoutStrictOrdering());

            // depth large → still bounded by graph
            svc.GetAncestors(C, maxDepth: 10)
               .Should().BeEquivalentTo(new[] { B, A, A2, Adopt }, o => o.WithoutStrictOrdering());
        }

        // ---------- GetDescendants (BFS down) ----------
        [Fact]
        public void GetDescendants_BFS_Downward_IncludesBioAndAdoptive_Unique_ExcludesSelf_DepthBounded()
        {
            var repo = new InMemoryLineageRepository();

            var root = Guid.NewGuid();
            var c1 = Guid.NewGuid();    // child bio
            var c2 = Guid.NewGuid();    // child adoptive
            var g1 = Guid.NewGuid();    // grandchild via c1
            var g2 = Guid.NewGuid();    // grandchild via c2 (adoptive chain)
            var other = Guid.NewGuid(); // unrelated

            // root’s children
            var lineC1 = new EntityLineage(c1);
            lineC1.SetBiologicalFather(root);
            repo.Upsert(lineC1);

            var lineC2 = new EntityLineage(c2);
            lineC2.AddAdoptiveParent(root);
            repo.Upsert(lineC2);

            // grandchildren
            var lineG1 = new EntityLineage(g1);
            lineG1.SetBiologicalMother(c1);
            repo.Upsert(lineG1);

            var lineG2 = new EntityLineage(g2);
            lineG2.AddAdoptiveParent(c2);
            repo.Upsert(lineG2);

            // unrelated
            repo.Upsert(new EntityLineage(other));

            var svc = new FamilyTreeService(repo);

            // depth=1 → direct children only (bio+adoptive)
            svc.GetDescendants(root, maxDepth: 1)
               .Should().BeEquivalentTo(new[] { c1, c2 }, o => o.WithoutStrictOrdering());

            // depth=2 → children + grandchildren
            svc.GetDescendants(root, maxDepth: 2)
               .Should().BeEquivalentTo(new[] { c1, c2, g1, g2 }, o => o.WithoutStrictOrdering());

            // root excluded always
            svc.GetDescendants(root, maxDepth: 5).Should().NotContain(root);
        }

        [Fact]
        public void GetDescendants_NoMatches_ReturnsEmpty()
        {
            var repo = new InMemoryLineageRepository();
            var someone = Guid.NewGuid();
            repo.Upsert(new EntityLineage(Guid.NewGuid())); // unrelated filler

            var svc = new FamilyTreeService(repo);
            svc.GetDescendants(someone).Should().BeEmpty();
        }
    }

    // ---------------- test stub repo ----------------

    internal sealed class InMemoryLineageRepository : ILineageRepository
    {
        private readonly Dictionary<Guid, EntityLineage> _byChild = new();

        public void Upsert(EntityLineage lineage) => _byChild[lineage.CharacterId] = lineage;

        public EntityLineage? FindByChildId(Guid childId)
            => _byChild.TryGetValue(childId, out var l) ? l : null;

        public IEnumerable<EntityLineage> FindBySomeParentIds(IEnumerable<Guid> parentIds)
        {
            var set = parentIds is HashSet<Guid> hs ? hs : new HashSet<Guid>(parentIds);
            return _byChild.Values.Where(l =>
                l.GetParents(includeAdoptive: true).Any(p => set.Contains(p.ParentId)));
        }

        public IEnumerable<EntityLineage> FindAll() => _byChild.Values;

        public IEnumerable<Guid> FindChildrenOfParent(Guid parentId)
        {
            throw new NotImplementedException();
        }

        public bool IsAncestorOf(Guid candidateAncestorId, Guid childId)
        {
            throw new NotImplementedException();
        }
    }
}
