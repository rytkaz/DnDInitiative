namespace DnDInitiative.Core.Encounters;

public enum EncounterDifficulty
{
    None,
    Low,
    Moderate,
    High,
    AboveHigh,
}

/// <summary>
/// The XP a party can face at each difficulty, from the 2024 Dungeon Master's Guide.
/// </summary>
public sealed record XpBudget(int Low, int Moderate, int High)
{
    // XP per character, indexed by level - 1.
    private static readonly (int Low, int Moderate, int High)[] s_perCharacter =
    [
        (50, 75, 100),
        (100, 150, 200),
        (150, 225, 400),
        (250, 375, 500),
        (500, 750, 1100),
        (600, 1000, 1400),
        (750, 1300, 1700),
        (1000, 1700, 2100),
        (1300, 2000, 2600),
        (1600, 2300, 3100),
        (1900, 2900, 4100),
        (2200, 3700, 4700),
        (2600, 4200, 5400),
        (2900, 4900, 6200),
        (3300, 5400, 7800),
        (3800, 6100, 9800),
        (4500, 7200, 11700),
        (5000, 8700, 14200),
        (5500, 10700, 17200),
        (6400, 13200, 22000),
    ];

    public static XpBudget ForParty(IEnumerable<int> characterLevels)
    {
        ArgumentNullException.ThrowIfNull(characterLevels);

        int low = 0;
        int moderate = 0;
        int high = 0;
        foreach (int level in characterLevels)
        {
            (int Low, int Moderate, int High) row = s_perCharacter[Math.Clamp(level, 1, 20) - 1];
            low += row.Low;
            moderate += row.Moderate;
            high += row.High;
        }

        return new XpBudget(low, moderate, high);
    }

    /// <summary>The lowest difficulty whose budget covers the given XP total.</summary>
    public EncounterDifficulty Classify(int totalXp)
    {
        if (totalXp <= 0 || High <= 0)
        {
            return EncounterDifficulty.None;
        }

        if (totalXp <= Low)
        {
            return EncounterDifficulty.Low;
        }

        if (totalXp <= Moderate)
        {
            return EncounterDifficulty.Moderate;
        }

        return totalXp <= High ? EncounterDifficulty.High : EncounterDifficulty.AboveHigh;
    }
}