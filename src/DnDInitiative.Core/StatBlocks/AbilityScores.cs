namespace DnDInitiative.Core.StatBlocks;

public sealed record AbilityScores(
    int Strength,
    int Dexterity,
    int Constitution,
    int Intelligence,
    int Wisdom,
    int Charisma)
{
    /// <summary>The modifier for a score: (score - 10) / 2, rounded down.</summary>
    public static int Modifier(int score)
    {
        return (int)Math.Floor((score - 10) / 2.0);
    }
}