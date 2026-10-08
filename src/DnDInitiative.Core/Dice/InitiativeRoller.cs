namespace DnDInitiative.Core.Dice;

public sealed class InitiativeRoller
{
    private readonly IDiceRoller _diceRoller;

    public InitiativeRoller(IDiceRoller diceRoller)
    {
        _diceRoller = diceRoller;
    }

    public InitiativeRoll Roll(int modifier, RollMode mode)
    {
        int natural = mode switch
        {
            RollMode.Normal => _diceRoller.Roll(20),
            RollMode.Advantage => Math.Max(_diceRoller.Roll(20), _diceRoller.Roll(20)),
            RollMode.Disadvantage => Math.Min(_diceRoller.Roll(20), _diceRoller.Roll(20)),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

        return new InitiativeRoll(natural, modifier);
    }
}