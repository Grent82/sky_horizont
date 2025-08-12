using FluentAssertions;
using Xunit;
using SkyHorizont.Domain.Factions;

namespace SkyHorizont.Tests.Factions
{
    public class DiplomaticStandingTests
    {
        // --- Constructor clamping ---
        [Theory]
        [InlineData(-150, -100)]
        [InlineData(-100, -100)]
        [InlineData(0,     0)]
        [InlineData(42,    42)]
        [InlineData(100,   100)]
        [InlineData(150,   100)]
        public void Ctor_ClampsInput_ToRangeMinus100ToPlus100(int input, int expected)
        {
            var s = new DiplomaticStanding(input);
            s.Value.Should().Be(expected);
        }

        // --- Adjust within bounds ---
        [Fact]
        public void Adjust_AddsDelta_WhenWithinBounds()
        {
            var s = new DiplomaticStanding(0);
            var s2 = s.Adjust(+10);

            s2.Value.Should().Be(10);
            s.Value.Should().Be(0); // original unchanged (immutability)
            s2.Should().NotBeSameAs(s); // returns a new instance
        }

        // --- Adjust clamps high ---
        [Fact]
        public void Adjust_ClampsAboveUpperBound()
        {
            var s = new DiplomaticStanding(90);
            var s2 = s.Adjust(+20);

            s2.Value.Should().Be(100);
        }

        // --- Adjust clamps low ---
        [Fact]
        public void Adjust_ClampsBelowLowerBound()
        {
            var s = new DiplomaticStanding(-90);
            var s2 = s.Adjust(-20);

            s2.Value.Should().Be(-100);
        }

        // --- Zero delta still returns a new instance (method is pure/immutable) ---
        [Fact]
        public void Adjust_ZeroDelta_ReturnsEqualValue_NewInstance()
        {
            var s = new DiplomaticStanding(15);
            var s2 = s.Adjust(0);

            s2.Value.Should().Be(15);
            s2.Should().NotBeSameAs(s);
        }

        // --- Chained adjustments clamp at each step ---
        [Fact]
        public void Adjust_ChainedOperations_RespectClampingEachStep()
        {
            var s = new DiplomaticStanding(95)
                .Adjust(+10)  // -> 100
                .Adjust(+5)   // stays 100
                .Adjust(-250) // -> -100
                .Adjust(+30); // -> -70

            s.Value.Should().Be(-70);
        }
    }
}
