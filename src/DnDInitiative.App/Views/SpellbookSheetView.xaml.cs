using System.Windows;
using System.Windows.Controls;

using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.Views;

/// <summary>One A4 page of a spellbook. The data context is a SpellbookPageViewModel.</summary>
public partial class SpellbookSheetView : UserControl
{
    public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(
        nameof(IsEditable),
        typeof(bool),
        typeof(SpellbookSheetView),
        new PropertyMetadata(true));

    /// <summary>Where the grid of cells starts on the page. XAML cannot build a Thickness from constants.</summary>
    public static Thickness GridMargin { get; } = new(SheetLayout.MarginX, SheetLayout.MarginY, 0, 0);

    /// <summary>Half the gap between cards, around each card in its cell.</summary>
    public static Thickness CardMargin { get; } = new(SheetLayout.Gap / 2);

    public SpellbookSheetView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Whether the remove buttons and the selection show. On by default, for the page preview;
    /// off when the page is printed.
    /// </summary>
    public bool IsEditable
    {
        get => (bool)GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }
}