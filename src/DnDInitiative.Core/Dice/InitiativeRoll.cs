namespace DnDInitiative.Core.Dice;

public readonly record struct InitiativeRoll(int Natural, int Modifier)
{
    public int Total => Natural + Modifier;
}