using DnDInitiative.Core.Dice;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FixedDiceRoller : IDiceRoller
{
    private readonly int _result;

    public FixedDiceRoller(int result)
    {
        _result = result;
    }

    public int Roll(int sides)
    {
        return _result;
    }
}