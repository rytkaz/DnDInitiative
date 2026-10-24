using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DnDInitiative.App.Formatting;

/// <summary>
/// The rules text of a card, one block at a time, at a text size the card chooses. Shown by
/// <see cref="Views.CardView"/> and measured by <see cref="CardLayout"/>, which is why it is
/// built in code: both must lay the text out in exactly the same way.
/// </summary>
public sealed class CardDescription : StackPanel
{
    public static readonly DependencyProperty BlocksProperty = DependencyProperty.Register(
        nameof(Blocks),
        typeof(IReadOnlyList<MarkdownBlock>),
        typeof(CardDescription),
        new PropertyMetadata(null, OnContentChanged));

    public static readonly DependencyProperty TextSizeProperty = DependencyProperty.Register(
        nameof(TextSize),
        typeof(double),
        typeof(CardDescription),
        new PropertyMetadata(CardLayout.MaxFontSize, OnContentChanged));

    private static readonly FontFamily HeadingFont = new("Georgia");

    public CardDescription()
    {
        ClipToBounds = true;

        // Set here and not by the view, so a measurement made without a view agrees with the drawing.
        TextElement.SetFontFamily(this, new FontFamily("Segoe UI"));
    }

    public IReadOnlyList<MarkdownBlock>? Blocks
    {
        get => (IReadOnlyList<MarkdownBlock>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    /// <summary>The size of ordinary text; headings are a little larger.</summary>
    public double TextSize
    {
        get => (double)GetValue(TextSizeProperty);
        set => SetValue(TextSizeProperty, value);
    }

    private static void OnContentChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        ((CardDescription)element).Rebuild();
    }

    private void Rebuild()
    {
        Children.Clear();

        foreach (MarkdownBlock block in Blocks ?? [])
        {
            Children.Add(Create(block, TextSize));
        }
    }

    private static UIElement Create(MarkdownBlock block, double size)
    {
        var text = new TextBlock { FontSize = size, TextWrapping = TextWrapping.Wrap };
        MarkdownText.AppendTo(text.Inlines, block.Text);

        switch (block.Kind)
        {
            case MarkdownBlockKind.Heading:
                text.FontFamily = HeadingFont;
                text.FontSize = size + 1.5;
                text.Margin = new Thickness(0, size * 0.6, 0, size * 0.15);
                if (Application.Current?.TryFindResource("StatBlockAccent") is Brush accent)
                {
                    text.Foreground = accent;
                }

                return text;

            case MarkdownBlockKind.Bullet:
                // The bullet sits beside the text, so a wrapped line stays under the text and not the bullet.
                var row = new Grid { Margin = new Thickness(0, size * 0.15, 0, size * 0.15) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition());
                var bullet = new TextBlock { FontSize = size, Margin = new Thickness(size * 0.5, 0, size * 0.5, 0), Text = "•" };
                Grid.SetColumn(text, 1);
                row.Children.Add(bullet);
                row.Children.Add(text);
                return row;

            default:
                text.Margin = new Thickness(0, size * 0.3, 0, size * 0.3);
                return text;
        }
    }
}
