using System.Text.RegularExpressions;

namespace DnDInitiative.Core.Conditions;

public static partial class ConditionSources
{
    /// <summary>
    /// Whether a condition's source is the official rules: the Player's Handbook (2014 or 2024)
    /// or the System Reference Document. Homebrew must not replace those definitions.
    /// </summary>
    /// <param name="source">As stored, for example "XPHB p. 367" or "Player's Handbook (2024) p. 361".</param>
    public static bool IsCoreRules(string? source)
    {
        return !string.IsNullOrWhiteSpace(source) && CoreRulesSource().IsMatch(source);
    }

    // The abbreviations as whole words, so "SRD" matches but "Hazards" does not. The typographic
    // apostrophe is accepted because converted text sometimes has one.
    [GeneratedRegex(@"\b(?:X?PHB|SRD)\b|Player['’]s Handbook|System Reference Document", RegexOptions.IgnoreCase)]
    private static partial Regex CoreRulesSource();
}
