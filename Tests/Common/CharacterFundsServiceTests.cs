using System;
using FluentAssertions;
using Moq;
using Xunit;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.DomainServices;

namespace SkyHorizont.Tests.Common
{
    public class CharacterFundsServiceTests
    {
        [Fact]
        public void CreditCharacter_NonPositiveAmount_NoRepositoryCall()
        {
            var repo = new Mock<ICharacterFundsRepository>(MockBehavior.Strict);
            var service = new CharacterFundsService(repo.Object);

            service.CreditCharacter(Guid.NewGuid(), 0);
            service.CreditCharacter(Guid.NewGuid(), -5);

            repo.VerifyNoOtherCalls();
        }

        [Fact]
        public void DeductCharacter_InsufficientFunds_ReturnsFalseAndNoDeduction()
        {
            var repo = new Mock<ICharacterFundsRepository>(MockBehavior.Strict);
            var id = Guid.NewGuid();
            repo.Setup(r => r.GetBalance(id)).Returns(50);
            var service = new CharacterFundsService(repo.Object);

            var success = service.DeductCharacter(id, 100);

            success.Should().BeFalse();
            repo.Verify(r => r.GetBalance(id), Times.Once);
            repo.Verify(r => r.AddBalance(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        }
    }
}
