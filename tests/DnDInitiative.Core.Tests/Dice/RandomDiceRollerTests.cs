using DnDInitiative.Core.Dice;

namespace DnDInitiative.Core.Tests.Dice;

public class RandomDiceRollerTests
{
    [Fact]
    public void Roll_MultipleDice_StaysWithinBounds()
    {
        var roller = new RandomDiceRoller(new Random(42));
        for (int i = 0; i < 1000; i++)
        {
            Assert.InRange(roller.Roll(20), 1, 20);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Roll_SidesLessThanOne_Throws(int sides)
    {
        var roller = new RandomDiceRoller(new Random(42));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            roller.Roll(sides);
        });
    }
}