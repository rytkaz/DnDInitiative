using System.Globalization;

using DnDInitiative.Core.Encounters;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// One row of the XP ranges table: the XP an encounter can total at each difficulty, for a party
/// of the given size whose characters are all the given level.
/// </summary>
public sealed record LevelXpRangesViewModel(
    int Level,
    string Low,
    string Moderate,
    string High,
    string AboveHigh,
    bool IsPartyLevel)
{
    public static IReadOnlyList<LevelXpRangesViewModel> ForParty(IReadOnlyCollection<int> memberLevels)
    {
        ArgumentNullException.ThrowIfNull(memberLevels);

        return Enumerable.Range(1, 20)
            .Select(level => ForLevel(level, memberLevels.Count, memberLevels.Contains(level)))
            .ToList();
    }

    private static LevelXpRangesViewModel ForLevel(int level, int partySize, bool isPartyLevel)
    {
        XpBudget budget = XpBudget.ForParty(Enumerable.Repeat(level, partySize));

        // The bounds match XpBudget.Classify: each difficulty runs up to and including its budget.
        return new LevelXpRangesViewModel(
            level,
            Range(1, budget.Low),
            Range(budget.Low + 1, budget.Moderate),
            Range(budget.Moderate + 1, budget.High),
            string.Create(CultureInfo.InvariantCulture, $"{budget.High + 1:N0}+"),
            isPartyLevel);
    }

    private static string Range(int from, int to)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{from:N0}–{to:N0}");
    }
}
