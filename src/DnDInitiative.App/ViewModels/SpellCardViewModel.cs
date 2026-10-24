using DnDInitiative.App.Formatting;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.ViewModels;

/// <summary>A spell on a card: the short facts in fields, then the description.</summary>
public sealed class SpellCardViewModel : CardViewModel
{
    private const string Ticked = "☑";
    private const string Empty = "☐";

    private SpellCardViewModel(Spell spell, CardPage page, bool isContinuation)
        : base(NameOf(spell.Name, isContinuation), FooterOf(spell), page, isContinuation)
    {
        CastingTime = spell.CastingTime.Trim();
        Range = spell.Range.Trim();
        Duration = spell.Duration.Trim();
        Components = spell.Components.Trim();
        Ritual = spell.IsRitual ? Ticked : Empty;
        Concentration = spell.IsConcentration ? Ticked : Empty;
        Level = Capitalize(spell.Level.Trim());
    }

    public string CastingTime { get; }

    public string Range { get; }

    public string Duration { get; }

    /// <summary>V, S, M and the material text, as the library words it.</summary>
    public string Components { get; }

    /// <summary>A ticked or an empty box.</summary>
    public string Ritual { get; }

    /// <summary>A ticked or an empty box.</summary>
    public string Concentration { get; }

    /// <summary>"Cantrip", "3rd-level".</summary>
    public string Level { get; }

    /// <summary>The card for a spell, followed by the cards its description spills onto.</summary>
    public static IReadOnlyList<SpellCardViewModel> CreateCards(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);

        IReadOnlyList<CardPage> pages = CardLayout.Paginate(
            MarkdownBlocks.Parse(spell.Description),
            CardLayout.SpellDescriptionHeight,
            CardLayout.ContinuationDescriptionHeight);

        return [.. pages.Select((page, index) => new SpellCardViewModel(spell, page, index > 0))];
    }

    private static string FooterOf(Spell spell)
    {
        return JoinFooter(spell.School.Trim(), spell.Source.Trim(), string.Join(", ", spell.Classes));
    }

    private static string Capitalize(string text)
    {
        return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
    }
}
