using System.Globalization;
using DnDInitiative.App.Formatting;
using DnDInitiative.Core.Abilities;

namespace DnDInitiative.App.ViewModels;

/// <summary>An ability on a card: a line saying what it is, then the description.</summary>
public sealed class AbilityCardViewModel : CardViewModel
{
    private AbilityCardViewModel(Ability ability, CardPage page, bool isContinuation, double subtitleHeight)
        : base(NameOf(ability.Name, isContinuation), FooterOf(ability), page, isContinuation)
    {
        Subtitle = SubtitleOf(ability);
        Prerequisites = PrerequisitesOf(ability);
        SubtitleHeight = subtitleHeight;
    }

    /// <summary>"2nd-Degree Martial Exploit".</summary>
    public string Subtitle { get; }

    /// <summary>"Prerequisite: 5th-level, Strength of 13", or empty when there are none.</summary>
    public string Prerequisites { get; }

    /// <summary>How tall the subtitle and prerequisites are on this ability's card, as measured.</summary>
    public double SubtitleHeight { get; }

    /// <summary>The card for an ability, followed by the cards its description spills onto.</summary>
    public static IReadOnlyList<AbilityCardViewModel> CreateCards(Ability ability)
    {
        ArgumentNullException.ThrowIfNull(ability);

        double subtitle = CardLayout.MeasureSubtitle(SubtitleOf(ability), PrerequisitesOf(ability));
        IReadOnlyList<CardPage> pages = CardLayout.Paginate(
            MarkdownBlocks.Parse(ability.Description),
            CardLayout.AbilityDescriptionHeight(subtitle),
            CardLayout.ContinuationDescriptionHeight);

        return [.. pages.Select((page, index) => new AbilityCardViewModel(ability, page, index > 0, subtitle))];
    }

    private static string SubtitleOf(Ability ability)
    {
        return ability.Degree > 0
            ? $"{Ordinal(ability.Degree)}-Degree {ability.Category}"
            : ability.Category;
    }

    private static string PrerequisitesOf(Ability ability)
    {
        return string.IsNullOrWhiteSpace(ability.Prerequisites)
            ? ""
            : $"Prerequisite: {ability.Prerequisites.Trim()}";
    }

    private static string FooterOf(Ability ability)
    {
        return JoinFooter(ability.Source.Trim(), string.Join(", ", ability.Classes));
    }

    private static string Ordinal(int number)
    {
        return number switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => string.Create(CultureInfo.InvariantCulture, $"{number}th"),
        };
    }
}
