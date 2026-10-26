using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using DnDInitiative.App.Formatting;
using DnDInitiative.App.ViewModels;

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

    // The Spellbook page this sheet is shown on, which does the work. Null for a printed page.
    private SpellbookViewModel? Spellbook
    {
        get
        {
            DependencyObject? element = this;
            while (element is not null and not SpellbookView)
            {
                element = VisualTreeHelper.GetParent(element);
            }

            return (element as SpellbookView)?.DataContext as SpellbookViewModel;
        }
    }

    // A right-click selects the card it opens on, so the menu clearly acts on that one.
    private void OnCardContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (!IsEditable || Spellbook is not { } spellbook)
        {
            e.Handled = true;
            return;
        }

        if (((FrameworkElement)sender).DataContext is SpellbookCardViewModel card)
        {
            spellbook.SelectedCard = card;
        }
    }

    // The menu's data context is the card it was opened on. Removing it removes all of its spell's cards.
    private void OnRemoveCardClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is SpellbookCardViewModel card)
        {
            Spellbook?.RemoveEntryCommand.Execute(card.Entry);
        }
    }
}