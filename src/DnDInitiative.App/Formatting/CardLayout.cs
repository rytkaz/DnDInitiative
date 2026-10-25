using System.Windows;

namespace DnDInitiative.App.Formatting;

/// <summary>One printed card's worth of description: the text size that fits and the blocks on it.</summary>
public sealed record CardPage(double FontSize, IReadOnlyList<MarkdownBlock> Blocks);

/// <summary>
/// The size of a spell or ability card and how its text is made to fit. A card is 63 × 88 mm,
/// the size of a trading card, which is 238 × 333 WPF units (1 unit = 1/96 inch). Text that
/// does not fit at the smallest readable size continues on a further card.
/// </summary>
/// <remarks>
/// The heights below are the fixed rows of <see cref="Views.CardView"/>. The description gets
/// whatever is left, so changing a row here must be matched in that view's XAML.
/// </remarks>
public static class CardLayout
{
    public const double Width = 238;
    public const double Height = 333;

    public const double MaxFontSize = 8;
    public const double MinFontSize = 6;

    // The view's border (1 all round) and margin (10 at the sides, 6 above and below).
    public const double ContentWidth = Width - (2 * 1) - (2 * 10);
    public const double ContentHeight = Height - (2 * 1) - (2 * 6);

    public const double NameHeight = 22;
    public const double RuleHeight = 6;
    public const double FieldRowHeight = 20;
    public const double FieldRowsHeight = 2 * FieldRowHeight;

    /// <summary>
    /// The most room the duration and the components can take, so very long material text cannot
    /// crowd out the description. Usually they take less; see <see cref="MeasureTextFields"/>.
    /// </summary>
    public const double MaxTextFieldsHeight = 64;

    /// <summary>The most room an ability's subtitle and prerequisites can take; see <see cref="MeasureSubtitle"/>.</summary>
    public const double MaxSubtitleHeight = 40;

    public const double FooterHeight = 14;

    /// <summary>A continuation card repeats only the name, so its description has the most room.</summary>
    public const double ContinuationDescriptionHeight = ContentHeight - NameHeight - RuleHeight - FooterHeight;

    private const double FontStep = 0.5;

    /// <summary>The room for a spell's description, below fields that take the given height.</summary>
    public static double SpellDescriptionHeight(double textFieldsHeight)
    {
        double header = FieldRowsHeight + textFieldsHeight + RuleHeight;
        return ContentHeight - NameHeight - RuleHeight - header - FooterHeight;
    }

    /// <summary>How tall a spell's duration and components are, wrapped to the card's width.</summary>
    /// <remarks>Measures a real WPF element, so it must run on a thread that may create one.</remarks>
    public static double MeasureTextFields(string duration, string components)
    {
        var fields = new CardTextFields { Duration = duration, Components = components };
        fields.Measure(new Size(ContentWidth, double.PositiveInfinity));
        return Math.Min(Math.Ceiling(fields.DesiredSize.Height), MaxTextFieldsHeight);
    }

    /// <summary>The room for an ability's description, below a subtitle of the given height.</summary>
    public static double AbilityDescriptionHeight(double subtitleHeight)
    {
        return ContentHeight - NameHeight - RuleHeight - (subtitleHeight + RuleHeight) - FooterHeight;
    }

    /// <summary>How tall an ability's subtitle and prerequisites are, wrapped to the card's width.</summary>
    /// <remarks>Measures a real WPF element, so it must run on a thread that may create one.</remarks>
    public static double MeasureSubtitle(string subtitle, string prerequisites)
    {
        var lines = new CardSubtitle { Subtitle = subtitle, Prerequisites = prerequisites };
        lines.Measure(new Size(ContentWidth, double.PositiveInfinity));
        return Math.Min(Math.Ceiling(lines.DesiredSize.Height), MaxSubtitleHeight);
    }

    /// <summary>
    /// Splits a description over as many cards as it needs. Each card first tries the largest text
    /// size and steps down until everything fits; only at the smallest size is the text cut.
    /// </summary>
    /// <param name="blocks">The description.</param>
    /// <param name="firstPageHeight">The room for the description on the first card.</param>
    /// <param name="nextPageHeight">The room on every card after it.</param>
    /// <remarks>Measures real WPF elements, so it must run on a thread that may create them.</remarks>
    public static IReadOnlyList<CardPage> Paginate(
        IReadOnlyList<MarkdownBlock> blocks,
        double firstPageHeight,
        double nextPageHeight)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var pages = new List<CardPage>();
        IReadOnlyList<MarkdownBlock> remaining = blocks;
        double height = firstPageHeight;

        do
        {
            (CardPage page, remaining) = FitPage(remaining, height);
            pages.Add(page);
            height = nextPageHeight;
        }
        while (remaining.Count > 0);

        return pages;
    }

    private static (CardPage Page, IReadOnlyList<MarkdownBlock> Remaining) FitPage(
        IReadOnlyList<MarkdownBlock> blocks,
        double height)
    {
        for (double size = MaxFontSize; size >= MinFontSize; size -= FontStep)
        {
            if (MeasureHeight(blocks, size) <= height)
            {
                return (new CardPage(size, blocks), []);
            }
        }

        // Too long even at the smallest size: keep as many whole blocks as fit.
        int fitting = 0;
        while (fitting < blocks.Count && MeasureHeight([.. blocks.Take(fitting + 1)], MinFontSize) <= height)
        {
            fitting++;
        }

        List<MarkdownBlock> page = [.. blocks.Take(fitting)];
        List<MarkdownBlock> rest = [.. blocks.Skip(fitting)];

        // The first block that did not fit is cut at a word, so the page ends full.
        MarkdownBlock cut = rest[0];
        string[] words = cut.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int low = 1;
        int high = words.Length - 1;
        int headWords = 0;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            List<MarkdownBlock> attempt = [.. page, cut with { Text = string.Join(' ', words.Take(middle)) }];
            if (MeasureHeight(attempt, MinFontSize) <= height)
            {
                headWords = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        // An empty page would repeat forever, so a block that fits nowhere goes on a page of its own.
        if (headWords == 0 && page.Count == 0)
        {
            headWords = Math.Min(1, words.Length);
        }

        if (headWords == 0)
        {
            return (new CardPage(MinFontSize, page), rest);
        }

        if (headWords >= words.Length)
        {
            page.Add(cut);
            return (new CardPage(MinFontSize, page), [.. rest.Skip(1)]);
        }

        page.Add(cut with { Text = string.Join(' ', words.Take(headWords)) });
        List<MarkdownBlock> remaining = [cut with { Text = string.Join(' ', words.Skip(headWords)) }, .. rest.Skip(1)];
        return (new CardPage(MinFontSize, page), remaining);
    }

    // Measured with the same element the card shows, so the fitting cannot drift from the drawing.
    private static double MeasureHeight(IReadOnlyList<MarkdownBlock> blocks, double fontSize)
    {
        var description = new CardDescription { TextSize = fontSize, Blocks = blocks };
        description.Measure(new Size(ContentWidth, double.PositiveInfinity));
        return description.DesiredSize.Height;
    }
}
