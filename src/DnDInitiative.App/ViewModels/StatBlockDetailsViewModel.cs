using System.Globalization;

using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

public sealed record LabeledValue(string Label, string Value);

public sealed record FeatureSection(string Title, IReadOnlyList<LabeledValue> Entries);

/// <summary>
/// A stat block prepared for display: text is formatted and empty parts are left out.
/// </summary>
public sealed class StatBlockDetailsViewModel
{
    public StatBlockDetailsViewModel(StatBlock statBlock)
    {
        ArgumentNullException.ThrowIfNull(statBlock);

        Name = statBlock.Name;
        Summary = JoinNonEmpty(", ", JoinNonEmpty(" ", statBlock.Size, statBlock.CreatureType), statBlock.Alignment);

        string hitPoints = statBlock.HitPoints.ToString(CultureInfo.InvariantCulture);
        var coreStats = new List<LabeledValue>();
        AddIfPresent(coreStats, "Armor Class", statBlock.ArmorClass.ToString(CultureInfo.InvariantCulture));
        AddIfPresent(coreStats, "Initiative", statBlock.InitiativeModifier.ToString("+0;-0", CultureInfo.InvariantCulture));
        AddIfPresent(coreStats, "Hit Points", JoinNonEmpty(" ", hitPoints, Parenthesize(statBlock.HitDice)));
        AddIfPresent(coreStats, "Speed", statBlock.Speed);
        AddIfPresent(coreStats, "Challenge", statBlock.ChallengeRating);
        CoreStats = coreStats;

        AbilityScores abilities = statBlock.Abilities;
        Abilities =
        [
            new LabeledValue("STR", FormatAbility(abilities.Strength)),
            new LabeledValue("DEX", FormatAbility(abilities.Dexterity)),
            new LabeledValue("CON", FormatAbility(abilities.Constitution)),
            new LabeledValue("INT", FormatAbility(abilities.Intelligence)),
            new LabeledValue("WIS", FormatAbility(abilities.Wisdom)),
            new LabeledValue("CHA", FormatAbility(abilities.Charisma)),
        ];

        var otherStats = new List<LabeledValue>();
        AddIfPresent(otherStats, "Saving Throws", statBlock.SavingThrows);
        AddIfPresent(otherStats, "Skills", statBlock.Skills);
        AddIfPresent(otherStats, "Vulnerabilities", statBlock.DamageVulnerabilities);
        AddIfPresent(otherStats, "Resistances", statBlock.DamageResistances);
        AddIfPresent(otherStats, "Immunities", statBlock.DamageImmunities);
        AddIfPresent(otherStats, "Condition Immunities", statBlock.ConditionImmunities);
        AddIfPresent(otherStats, "Senses", statBlock.Senses);
        AddIfPresent(otherStats, "Languages", statBlock.Languages);
        AddIfPresent(otherStats, "Source", statBlock.Source);
        OtherStats = otherStats;

        var sections = new List<FeatureSection>();
        AddSection(sections, "Traits", statBlock.Traits);
        AddSection(sections, "Actions", statBlock.Actions);
        AddSection(sections, "Bonus Actions", statBlock.BonusActions);
        AddSection(sections, "Reactions", statBlock.Reactions);
        AddSection(sections, "Legendary Actions", statBlock.LegendaryActions);
        Sections = sections;
    }

    public string Name { get; }

    public string Summary { get; }

    public IReadOnlyList<LabeledValue> CoreStats { get; }

    public IReadOnlyList<LabeledValue> Abilities { get; }

    public IReadOnlyList<LabeledValue> OtherStats { get; }

    public IReadOnlyList<FeatureSection> Sections { get; }

    private static string FormatAbility(int score)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{score} ({AbilityScores.Modifier(score):+0;-0})");
    }

    private static string Parenthesize(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? "" : "(" + text.Trim() + ")";
    }

    private static string JoinNonEmpty(string separator, params string[] parts)
    {
        return string.Join(separator, parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part.Trim()));
    }

    private static void AddIfPresent(List<LabeledValue> target, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            target.Add(new LabeledValue(label, value.Trim()));
        }
    }

    private static void AddSection(List<FeatureSection> target, string title, IReadOnlyList<StatBlockFeature> features)
    {
        if (features.Count > 0)
        {
            target.Add(new FeatureSection(
                title,
                features.Select(feature => new LabeledValue(feature.Name + ".", WithoutBlankLines(feature.Description))).ToList()));
        }
    }

    /// <summary>
    /// A feature with several paragraphs is stored with an empty line between them, which is
    /// how Markdown separates paragraphs. On the card that empty line reads as a gap between
    /// two features, so each paragraph simply starts on a new line, as in the printed books.
    /// Only the display changes; the stored text and the editor keep the empty lines.
    /// </summary>
    private static string WithoutBlankLines(string text)
    {
        IEnumerable<string> lines = text
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

        return string.Join('\n', lines);
    }
}