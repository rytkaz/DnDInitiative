using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// What a spell or ability card shows. Laid out by <see cref="Views.CardView"/> at a fixed
/// 63 × 88 mm. A description too long for one card is spread over several, each with its own
/// view model, so a card never scrolls.
/// </summary>
public abstract class CardViewModel
{
    protected CardViewModel(string name, string footer, CardPage page, bool isContinuation)
    {
        ArgumentNullException.ThrowIfNull(page);

        Name = name;
        Footer = footer;
        TextSize = page.FontSize;
        Blocks = page.Blocks;
        IsContinuation = isContinuation;
    }

    /// <summary>"Name (cont.)" on the cards after the first.</summary>
    public string Name { get; }

    /// <summary>The very small line at the foot, such as "Evocation · XPHB · Sorcerer, Wizard".</summary>
    public string Footer { get; }

    /// <summary>The size of the rules text, as far as it was stepped down to fit.</summary>
    public double TextSize { get; }

    /// <summary>The part of the rules text on this card.</summary>
    public IReadOnlyList<MarkdownBlock> Blocks { get; }

    /// <summary>A card after the first: it has the name and text only, to leave room for more text.</summary>
    public bool IsContinuation { get; }

    protected static string NameOf(string name, bool isContinuation)
    {
        return isContinuation ? $"{name} (cont.)" : name;
    }

    protected static string JoinFooter(params IEnumerable<string> parts)
    {
        return string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
