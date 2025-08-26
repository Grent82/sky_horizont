using System;
using System.Linq;
using FluentAssertions;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Testing;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class CharactersRepositoryTests
{
    [Fact]
    public void GetFamilyMembers_ReturnsLinkedFamily()
    {
        var ctx = new InMemoryCharactersDbContext();
        var repo = new CharactersRepository(ctx);

        var actor = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Actor", Sex.Male, 30, 3000, 1);
        var fam1 = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Fam1", Sex.Female, 25, 3005, 5);
        var fam2 = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Fam2", Sex.Male, 20, 3005, 5);

        actor.LinkFamilyMember(fam1.Id);
        actor.LinkFamilyMember(fam2.Id);

        ctx.Characters[actor.Id] = actor;
        ctx.Characters[fam1.Id] = fam1;
        ctx.Characters[fam2.Id] = fam2;

        var family = repo.GetFamilyMembers(actor.Id).ToList();

        family.Should().HaveCount(2).And.Contain(fam1).And.Contain(fam2);
    }

    [Fact]
    public void GetSecretLovers_ExcludesSpouse()
    {
        var ctx = new InMemoryCharactersDbContext();
        var repo = new CharactersRepository(ctx);

        var actor = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Actor", Sex.Male, 30, 3000, 1);
        var lover = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Lover", Sex.Female, 24, 3002, 3);
        var spouse = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Spouse", Sex.Female, 28, 2998, 7);

        actor.AddRelationship(lover.Id, RelationshipType.Lover);
        lover.AddRelationship(actor.Id, RelationshipType.Lover);

        actor.AddRelationship(spouse.Id, RelationshipType.Spouse);
        spouse.AddRelationship(actor.Id, RelationshipType.Spouse);

        ctx.Characters[actor.Id] = actor;
        ctx.Characters[lover.Id] = lover;
        ctx.Characters[spouse.Id] = spouse;

        var lovers = repo.GetSecretLovers(actor.Id).ToList();

        lovers.Should().ContainSingle().Which.Should().Be(lover);
    }
}
