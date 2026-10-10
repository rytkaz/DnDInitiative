using System.Windows;
using System.Windows.Documents;

namespace DnDInitiative.App.Formatting;

/// <summary>
/// Lets XAML bind Markdown text onto a Span: MarkdownText.Text="{Binding ...}".
/// </summary>
public static class MarkdownText
{
    /// <summary>
    /// The resource key of the style given to links. Set from code because a link inside a
    /// data template does not pick up an implicit style from the view around it.
    /// </summary>
    public const string LinkStyleKey = "MarkdownLink";

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(MarkdownText),
        new PropertyMetadata(null, OnTextChanged));

    public static string? GetText(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (string?)element.GetValue(TextProperty);
    }

    public static void SetText(DependencyObject element, string? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not Span span)
        {
            return;
        }

        span.Inlines.Clear();
        foreach (TextSegment segment in InlineMarkdown.Parse(e.NewValue as string))
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
                span.Inlines.Add(link);
            }
            else
            {
                span.Inlines.Add(run);
            }
        }
    }
}