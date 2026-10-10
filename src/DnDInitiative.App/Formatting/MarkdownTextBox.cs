using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDInitiative.App.Formatting;

/// <summary>
/// Shows a piece of Markdown text that can be selected and copied, with working links.
/// A TextBlock cannot be selected from, and a TextBox cannot show bold text or links, so this
/// is a read-only RichTextBox that builds its own content from three bindable properties.
/// Its look (no border, no background) comes from a style in Styles/AppStyles.xaml.
/// </summary>
public sealed class MarkdownTextBox : RichTextBox
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(MarkdownTextBox),
        new PropertyMetadata(null, OnContentChanged));

    public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register(
        nameof(LabelStyle),
        typeof(Style),
        typeof(MarkdownTextBox),
        new PropertyMetadata(null, OnContentChanged));

    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
        nameof(Markdown),
        typeof(string),
        typeof(MarkdownTextBox),
        new PropertyMetadata(null, OnContentChanged));

    public MarkdownTextBox()
    {
        IsReadOnly = true;

        // Without this, a read-only RichTextBox shows links but does not let them be clicked.
        IsDocumentEnabled = true;
    }

    /// <summary>Plain text shown first, such as "Armor Class" or a trait's name. Not read as Markdown.</summary>
    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>How the label looks. A style for a <see cref="Run"/>.</summary>
    public Style? LabelStyle
    {
        get => (Style?)GetValue(LabelStyleProperty);
        set => SetValue(LabelStyleProperty, value);
    }

    /// <summary>The text after the label. Emphasis and links to spells are shown as such.</summary>
    public string? Markdown
    {
        get => (string?)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    /// <summary>
    /// This box is always as tall as its text, so it has nothing to scroll. Left alone it would
    /// still swallow the mouse wheel, and the card would stop scrolling whenever the mouse was
    /// over text. The wheel is handed to whatever contains the box instead.
    /// </summary>
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        e.Handled = true;

        if (VisualTreeHelper.GetParent(this) is UIElement parent)
        {
            parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = this,
            });
        }
    }

    private static void OnContentChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        ((MarkdownTextBox)element).Rebuild();
    }

    // A RichTextBox shows a document made of paragraphs. This one always has exactly one.
    private void Rebuild()
    {
        // The margins are removed because the view decides the spacing between pieces of text.
        var paragraph = new Paragraph { Margin = new Thickness(0) };

        string markdown = Markdown ?? "";

        if (!string.IsNullOrEmpty(Label))
        {
            // The space belongs to the label, so a label on its own does not end in one.
            string label = markdown.Length > 0 ? Label + " " : Label;
            paragraph.Inlines.Add(new Run(label) { Style = LabelStyle });
        }

        MarkdownText.AppendTo(paragraph.Inlines, markdown);

        Document = new FlowDocument(paragraph) { PagePadding = new Thickness(0) };
    }
}
