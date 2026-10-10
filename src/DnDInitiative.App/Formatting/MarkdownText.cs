using System.Windows;
using System.Windows.Documents;

namespace DnDInitiative.App.Formatting;

/// <summary>
/// Turns Markdown text into WPF text elements: runs for plain, bold and italic text, and
/// hyperlinks for links to spells.
/// </summary>
public static class MarkdownText
{
    /// <summary>
    /// The resource key of the style given to links. Set from code because a link made in
    /// code does not pick up an implicit style from the view around it.
    /// </summary>
    public const string LinkStyleKey = "MarkdownLink";

    /// <summary>Adds the text to the end of a paragraph's or span's content.</summary>
    public static void AppendTo(InlineCollection inlines, string? markdown)
    {
        ArgumentNullException.ThrowIfNull(inlines);

        foreach (TextSegment segment in InlineMarkdown.Parse(markdown))
        {
            var run = new Run(segment.Text);
            if (segment.IsBold)
            {
                run.FontWeight = FontWeights.Bold;
            }

            if (segment.IsItalic)
            {
                run.FontStyle = FontStyles.Italic;
            }

            // Only links to spells lead anywhere in this app. Any other link is shown as its text.
            if (SpellLink.ToUri(segment.LinkTarget) is { } uri)
            {
                // Clicking raises Hyperlink.RequestNavigate with this URI. The event travels up
                // through the page to the main window, which opens the spell.
                var link = new Hyperlink(run) { NavigateUri = uri };
                link.SetResourceReference(FrameworkContentElement.StyleProperty, LinkStyleKey);
                inlines.Add(link);
            }
            else
            {
                inlines.Add(run);
            }
        }
    }
}
