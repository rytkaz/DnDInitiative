using System.Windows;
using System.Windows.Documents;

namespace DnDInitiative.App.Formatting;

/// <summary>
/// Turns Markdown text into WPF text elements: runs for plain, bold and italic text, and
/// hyperlinks for links to spells and to web pages.
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

            // Links to spells and to web pages lead somewhere. Any other link is shown as its text.
            Uri? webAddress = WebLink.ToUri(segment.LinkTarget);

            if ((SpellLink.ToUri(segment.LinkTarget) ?? webAddress) is { } uri)
            {
                // Clicking raises Hyperlink.RequestNavigate with this URI. The event travels up
                // through the page to the main window, which opens the spell or the web page.
                var link = new Hyperlink(run) { NavigateUri = uri };
                link.SetResourceReference(FrameworkContentElement.StyleProperty, LinkStyleKey);

                if (webAddress is not null)
                {
                    // A web link leaves the app, so show where it goes before it is clicked.
                    link.ToolTip = webAddress.AbsoluteUri;
                }

                inlines.Add(link);
            }
            else
            {
                inlines.Add(run);
            }
        }
    }
}
