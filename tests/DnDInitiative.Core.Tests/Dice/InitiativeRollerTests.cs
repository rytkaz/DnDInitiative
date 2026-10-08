using DnDInitiative.Core.Dice;

namespace DnDInitiative.Core.Tests.Dice;

public class InitiativeRollerTests
{
    [Fact]
    public void Roll_NormalMode_RollsExactlyOneDie()
    {
        var dice = new SequenceDiceRoller(15, 4);
        var roller = new InitiativeRoller(dice);

        roller.Roll(modifier: 0, RollMode.Normal);

        Assert.Equal(1, dice.RollCount);
    }

    [Fact]
    public void Roll_WithNegativeModifier_SubtractsFromNatural()
    {
        var dice = new SequenceDiceRoller(15);
        var roller = new InitiativeRoller(dice);

        var result = roller.Roll(modifier: -3, RollMode.Normal);

        Assert.Equal(15, result.Natural);
        Assert.Equal(12, result.Total);
    }

    [Theory]
    [InlineData(RollMode.Normal, 15, 4, 15)]
    [InlineData(RollMode.Advantage, 4, 15, 15)]
    [InlineData(RollMode.Advantage, 15, 4, 15)]
    [InlineData(RollMode.Disadvantage, 15, 4, 4)]
    [InlineData(RollMode.Disadvantage, 4, 15, 4)]
    public void Roll_WithDifferentModes_ModesHandledCorrectly(RollMode mode, int dice1, int dice2, int expectedResult)
    {
        var dice = new SequenceDiceRoller(dice1, dice2);
        var roller = new InitiativeRoller(dice);
        Assert.Equal(expectedResult, roller.Roll(modifier: 0, mode).Total);
    }
}