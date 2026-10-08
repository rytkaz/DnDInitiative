namespace DnDInitiative.Core.Dice;

public sealed class RandomDiceRoller : IDiceRoller
{
    private readonly Random _random;

    public RandomDiceRoller(Random random)
    {
        _random = random;
    }

    public int Roll(int sides)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 1);
        return _random.Next(0, sides) + 1;
    }
}