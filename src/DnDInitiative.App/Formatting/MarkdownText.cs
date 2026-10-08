using System.Windows;
using System.Windows.Documents;

namespace DnDInitiative.App.Formatting;

/// <summary>
/// Lets XAML bind Markdown text onto a Span: MarkdownText.Text="{Binding ...}".
/// </summary>
public static class MarkdownText
{
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

            span.Inlines.Add(run);
        }
    }
}