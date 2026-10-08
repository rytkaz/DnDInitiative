using DnDInitiative.Core.Dice;

namespace DnDInitiative.Core.Tests.Dice;

internal sealed class SequenceDiceRoller : IDiceRoller
{
    public int RollCount => _currentIndex;

    private readonly int[] _dice;
    private int _currentIndex;

    public SequenceDiceRoller(params int[] dice)
    {
        _dice = dice;
    }

    public int Roll(int sides)
    {
        if (_dice.Length <= _currentIndex)
        {
            throw new InvalidOperationException("SequenceDiceRoller out of dice");
        }

        return _dice[_currentIndex++];
    }
}