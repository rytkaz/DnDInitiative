using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using DnDInitiative.App.Behaviors;
using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

/// <summary>
/// The Spellbook page. The code here only handles the mouse: selecting a card, and dragging
/// cards and library rows onto the pages. What a drop does is up to the view model.
/// </summary>
public partial class SpellbookView : UserControl
{
    // What the left button went down on, until the mouse has moved far enough to start a drag.
    private object? _dragSource;
    private Point _dragStart;

    // The card currently showing the insertion line, so it can be cleared when the mouse moves on.
    private FrameworkElement? _dropSlot;

    public SpellbookView()
    {
        InitializeComponent();
    }

    private SpellbookViewModel? ViewModel => DataContext as SpellbookViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel?.LoadCommand.Execute(null);
    }

    // The heading row sits above the list, outside its scroll bar, so it is kept as wide as the
    // rows: narrower by the scroll bar's width whenever one shows.
    private void OnLibraryListLoaded(object sender, RoutedEventArgs e)
    {
        if (FindDescendant<ScrollViewer>(LibraryList) is not { } scroller)
        {
            return;
        }

        void Align()
        {
            Thickness margin = LibraryHeader.Margin;
            double scrollBar = Math.Max(0, scroller.ActualWidth - scroller.ViewportWidth);
            LibraryHeader.Margin = new Thickness(margin.Left, margin.Top, margin.Left + scrollBar, margin.Bottom);
        }

        scroller.ScrollChanged += (_, _) => Align();
        scroller.SizeChanged += (_, _) => Align();
        Align();
    }

    private void OnLibraryDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Only a double-click on a row adds it, not one on the scroll bar.
        if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is not null)
        {
            ViewModel?.AddToSpellbookCommand.Execute(null);
        }
    }

    private void OnLibraryPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragSource = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject)?.DataContext as LibraryEntryViewModel;
        _dragStart = e.GetPosition(this);
    }

    private void OnLibraryPreviewMouseMove(object sender, MouseEventArgs e)
    {
        StartDragIfMoved(sender, e, typeof(LibraryEntryViewModel), DragDropEffects.Copy);
    }

    // A click on a card selects it, and may be the start of a drag. A click on its remove button is left to the button.
    private void OnPagesPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragSource = null;
        if (FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        if (FindCardSlot(e.OriginalSource as DependencyObject)?.DataContext is SpellbookCardViewModel card && ViewModel is { } viewModel)
        {
            viewModel.SelectedCard = card;
            _dragSource = card;
            _dragStart = e.GetPosition(this);
        }

        PagesScroller.Focus();
    }

    private void OnPagesPreviewMouseMove(object sender, MouseEventArgs e)
    {
        StartDragIfMoved(sender, e, typeof(SpellbookCardViewModel), DragDropEffects.Move);
    }

    private void StartDragIfMoved(object sender, MouseEventArgs e, Type format, DragDropEffects effects)
    {
        if (_dragSource is null || !format.IsInstanceOfType(_dragSource) || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Vector moved = e.GetPosition(this) - _dragStart;
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        object source = _dragSource;
        _dragSource = null;
        DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(format, source), effects);

        // DoDragDrop only returns once the drag is over, however it ended. One clean-up covers all of them.
        ShowDropIndicator(null, DropPosition.None);
    }

    private void OnPagesDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (e.Data.GetDataPresent(typeof(SpellbookCardViewModel)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else if (e.Data.GetDataPresent(typeof(LibraryEntryViewModel)) && ViewModel is { HasSelectedSpellbook: true })
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
            ShowDropIndicator(null, DropPosition.None);
            return;
        }

        FrameworkElement? slot = FindCardSlot(e.OriginalSource as DependencyObject);
        ShowDropIndicator(slot, slot is null ? DropPosition.None : GetDropPosition(slot, e));
    }

    private void OnPagesDragLeave(object sender, DragEventArgs e)
    {
        ShowDropIndicator(null, DropPosition.None);
    }

    // Dropped on a card, the dragged one goes before or after it, by which half of the card the
    // mouse is over. Dropped between the pages, it goes at the end.
    private void OnPagesDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        ShowDropIndicator(null, DropPosition.None);

        if (ViewModel is not { } viewModel)
        {
            return;
        }

        FrameworkElement? slot = FindCardSlot(e.OriginalSource as DependencyObject);
        SpellbookEntryViewModel? target = (slot?.DataContext as SpellbookCardViewModel)?.Entry;
        bool after = slot is not null && GetDropPosition(slot, e) == DropPosition.Below;

        if (e.Data.GetData(typeof(SpellbookCardViewModel)) is SpellbookCardViewModel dragged)
        {
            viewModel.MoveEntry(dragged.Entry, target, after);
        }
        else if (e.Data.GetData(typeof(LibraryEntryViewModel)) is LibraryEntryViewModel row)
        {
            viewModel.AddEntry(row, target, after);
        }
    }

    // In a row of cards, "above" stands for before the card and "below" for after it.
    private static DropPosition GetDropPosition(FrameworkElement slot, DragEventArgs e)
    {
        return e.GetPosition(slot).X < slot.ActualWidth / 2 ? DropPosition.Above : DropPosition.Below;
    }

    // Moves the insertion line to the given card, or removes it when position is None.
    private void ShowDropIndicator(FrameworkElement? slot, DropPosition position)
    {
        if (_dropSlot is not null && !ReferenceEquals(_dropSlot, slot))
        {
            DropIndicator.SetPosition(_dropSlot, DropPosition.None);
        }

        if (slot is not null)
        {
            DropIndicator.SetPosition(slot, position);
        }

        _dropSlot = position == DropPosition.None ? null : slot;
    }

    // The card's outermost element in the sheet's template, marked with a tag.
    private static FrameworkElement? FindCardSlot(DependencyObject? element)
    {
        while (element is not null and not FrameworkElement { Tag: "CardSlot" })
        {
            element = ParentOf(element);
        }

        return element as FrameworkElement;
    }

    // Walks up from whatever was under the mouse to the nearest element of a type.
    private static T? FindAncestor<T>(DependencyObject? element)
        where T : DependencyObject
    {
        while (element is not null and not T)
        {
            element = ParentOf(element);
        }

        return element as T;
    }

    private static T? FindDescendant<T>(DependencyObject element)
        where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, index);
            if ((child as T ?? FindDescendant<T>(child)) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static DependencyObject? ParentOf(DependencyObject element)
    {
        return element is Visual
            ? VisualTreeHelper.GetParent(element)
            : LogicalTreeHelper.GetParent(element);
    }
}