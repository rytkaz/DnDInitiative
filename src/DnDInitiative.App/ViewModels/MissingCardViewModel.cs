using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// Stands in for a spell or ability that is not in the library, because it was deleted or has
/// not been imported on this computer. Keeps its place in a spellbook instead of vanishing.
/// </summary>
public sealed class MissingCardViewModel : CardViewModel
{
    private MissingCardViewModel(string key, string explanation)
        : base(key, "", new CardPage(CardLayout.MaxFontSize, [new MarkdownBlock(MarkdownBlockKind.Paragraph, explanation)]), isContinuation: false)
    {
    }

    public static MissingCardViewModel ForSpell(string key)
    {
        return new MissingCardViewModel(
            key,
            "This spell is not in the library. Spells are brought in by Convert and import on the 5eTools Import page.");
    }

    public static MissingCardViewModel ForAbility(string key)
    {
        return new MissingCardViewModel(
            key,
            "This ability is not in the library. Abilities are brought in by the GM Binder import.");
    }
}
