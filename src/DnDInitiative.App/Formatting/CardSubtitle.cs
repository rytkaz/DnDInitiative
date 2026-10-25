using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DnDInitiative.App.Formatting;

/// <summary>
/// The lines under an ability card's name: what it is, such as "2nd-Degree Martial Exploit", and
/// its prerequisites, wrapping over as many lines as they need. Shown by <see cref="Views.CardView"/>
/// and measured by <see cref="CardLayout"/>, so the description starts right under them.
/// </summary>
public sealed class CardSubtitle : StackPanel
{
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(CardSubtitle),
        new PropertyMetadata("", OnContentChanged));

    public static readonly DependencyProperty PrerequisitesProperty = DependencyProperty.Register(
        nameof(Prerequisites),
        typeof(string),
        typeof(CardSubtitle),
        new PropertyMetadata("", OnContentChanged));

    public CardSubtitle()
    {
        // Set here and not by the view, so a measurement made without a view agrees with the drawing.
        TextElement.SetFontFamily(this, new FontFamily("Segoe UI"));
        Rebuild();
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>"Prerequisite: 5th-level, Strength of 13", or empty when there are none.</summary>
    public string Prerequisites
    {
        get => (string)GetValue(PrerequisitesProperty);
        set => SetValue(PrerequisitesProperty, value);
    }

    private static void OnContentChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        ((CardSubtitle)element).Rebuild();
    }

    private void Rebuild()
    {
        Children.Clear();
        Children.Add(new TextBlock { FontSize = 7, FontStyle = FontStyles.Italic, Text = Subtitle ?? "" });

        // No line at all without prerequisites, so the space is not kept for them.
        if (!string.IsNullOrEmpty(Prerequisites))
        {
            Children.Add(new TextBlock { FontSize = 6.5, Text = Prerequisites, TextWrapping = TextWrapping.Wrap });
        }
    }
}