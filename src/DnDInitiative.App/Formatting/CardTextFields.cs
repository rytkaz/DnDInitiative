using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DnDInitiative.App.Formatting;

/// <summary>
/// A spell card's duration and components, each under its small label and wrapping over as many
/// lines as it needs. Shown by <see cref="Views.CardView"/> and measured by <see cref="CardLayout"/>,
/// so the description below starts right under the components however long they are.
/// </summary>
public sealed class CardTextFields : StackPanel
{
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration),
        typeof(string),
        typeof(CardTextFields),
        new PropertyMetadata("", OnContentChanged));

    public static readonly DependencyProperty ComponentsProperty = DependencyProperty.Register(
        nameof(Components),
        typeof(string),
        typeof(CardTextFields),
        new PropertyMetadata("", OnContentChanged));

    public CardTextFields()
    {
        // Set here and not by the view, so a measurement made without a view agrees with the drawing.
        TextElement.SetFontFamily(this, new FontFamily("Segoe UI"));
        Rebuild();
    }

    public string Duration
    {
        get => (string)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>V, S, M and the material text, as the library words it.</summary>
    public string Components
    {
        get => (string)GetValue(ComponentsProperty);
        set => SetValue(ComponentsProperty, value);
    }

    private static void OnContentChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        ((CardTextFields)element).Rebuild();
    }

    private void Rebuild()
    {
        Children.Clear();
        Children.Add(Label("Duration", top: 0));
        Children.Add(Value(Duration));
        Children.Add(Label("Components", top: 2));
        Children.Add(Value(Components));
    }

    // The same look as the card's other field labels: small, bold capitals.
    private static TextBlock Label(string text, double top)
    {
        var label = new TextBlock
        {
            FontSize = 5.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, top, 0, 0),
            Text = text,
        };
        Typography.SetCapitals(label, FontCapitals.AllSmallCaps);
        return label;
    }

    private static TextBlock Value(string? text)
    {
        return new TextBlock
        {
            FontSize = 7,
            Text = text ?? "",
            TextWrapping = TextWrapping.Wrap,
            ToolTip = string.IsNullOrEmpty(text) ? null : text,
        };
    }
}