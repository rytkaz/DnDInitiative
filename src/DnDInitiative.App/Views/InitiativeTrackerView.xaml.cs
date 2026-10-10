using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using DnDInitiative.App.Behaviors;
using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

public partial class InitiativeTrackerView : UserControl
{
    private CombatantViewModel? _dragRow;
    private Point _dragStart;
    // The row currently showing the insertion line, so it can be cleared when the mouse moves on.
    private DataGridRow? _dropRow;

    public InitiativeTrackerView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InitiativeTrackerViewModel viewModel)
        {
            viewModel.LoadPartiesCommand.Execute(null);
            viewModel.LoadLibraryCommand.Execute(null);
        }
    }

    // A TextBox normally writes its value when it loses focus. This makes Enter do it too.
    private void OnCellTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
    }

    // Puts the cursor in the amount box as soon as the HP menu opens.
    private void OnHpPopupOpened(object? sender, EventArgs e)
    {
        if (sender is Popup { Child: Border { Child: Panel panel } })
        {
            TextBox? amountBox = panel.Children.OfType<TextBox>().FirstOrDefault();
            amountBox?.Focus();
        }
    }


    // Remember which row's handle was pressed, if any.
    private void OnGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragRow = e.OriginalSource is FrameworkElement { Tag: "DragHandle", DataContext: CombatantViewModel row }
            ? row
            : null;
        _dragStart = e.GetPosition(this);
    }

    // Start the drag once the mouse has moved far enough to not be a click.
    private void OnGridPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragRow is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Vector moved = e.GetPosition(this) - _dragStart;
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        CombatantViewModel row = _dragRow;
        _dragRow = null;
        DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(typeof(CombatantViewModel), row), DragDropEffects.Move);

        // DoDragDrop only returns once the drag is over, however it ended: dropped here,
        // dropped elsewhere, or cancelled with Escape. One clean-up covers all three.
        ShowDropIndicator(null, DropPosition.None);
    }

    // Accept our own rows anywhere in the grid, and show where the row would land.
    private void OnGridPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (!e.Data.GetDataPresent(typeof(CombatantViewModel)))
        {
            e.Effects = DragDropEffects.None;
            ShowDropIndicator(null, DropPosition.None);
            return;
        }

        e.Effects = DragDropEffects.Move;

        DataGridRow? row = FindRow(e.OriginalSource as DependencyObject);
        ShowDropIndicator(row, GetDropPosition(e.Data, row));
    }

    // The mouse left the grid while still dragging: there is no target any more.
    private void OnGridPreviewDragLeave(object sender, DragEventArgs e)
    {
        ShowDropIndicator(null, DropPosition.None);
    }

    private void OnGridPreviewDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        ShowDropIndicator(null, DropPosition.None);

        if (DataContext is InitiativeTrackerViewModel viewModel
            && e.Data.GetData(typeof(CombatantViewModel)) is CombatantViewModel dragged
            && FindRow(e.OriginalSource as DependencyObject)?.DataContext is CombatantViewModel target)
        {
            viewModel.MoveCombatant(dragged, target);
        }
    }

    // The dragged row takes the target's place. Coming from above, that puts it below the
    // target; coming from below, above it.
    private DropPosition GetDropPosition(IDataObject data, DataGridRow? targetRow)
    {
        if (DataContext is not InitiativeTrackerViewModel viewModel
            || data.GetData(typeof(CombatantViewModel)) is not CombatantViewModel dragged
            || targetRow?.DataContext is not CombatantViewModel target)
        {
            return DropPosition.None;
        }

        int from = viewModel.Combatants.IndexOf(dragged);
        int to = viewModel.Combatants.IndexOf(target);

        return from.CompareTo(to) switch
        {
            < 0 => DropPosition.Below,
            > 0 => DropPosition.Above,
            _ => DropPosition.None,
        };
    }

    // Moves the insertion line to the given row, or removes it when position is None.
    private void ShowDropIndicator(DataGridRow? row, DropPosition position)
    {
        if (_dropRow is not null && !ReferenceEquals(_dropRow, row))
        {
            DropIndicator.SetPosition(_dropRow, DropPosition.None);
        }

        if (row is not null)
        {
            DropIndicator.SetPosition(row, position);
        }

        _dropRow = position == DropPosition.None ? null : row;
    }

    // Walks up from whatever was under the mouse to the row that contains it.
    private static DataGridRow? FindRow(DependencyObject? element)
    {
        while (element is not null and not DataGridRow)
        {
            element = element is Visual
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return element as DataGridRow;
    }

    private void OnHpButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: CombatantViewModel row } button
            && Resources["HpPopup"] is Popup popup)
        {
            popup.DataContext = row;
            popup.PlacementTarget = button;
            popup.IsOpen = true;
        }
    }

    // Runs just before the button's command, so the menu closes as the change is applied.
    private void OnHpPopupActionClick(object sender, RoutedEventArgs e)
    {
        if (Resources["HpPopup"] is Popup popup)
        {
            popup.IsOpen = false;
        }
    }

    // The strip sits on the panel's left edge, so dragging left (a negative change) makes the panel wider.
    private void OnPanelResizeDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is InitiativeTrackerViewModel viewModel)
        {
            viewModel.StatBlockPanelWidth -= e.HorizontalChange;
        }
    }

    // Opens the popup whose resource key is in the clicked button's Tag, anchored to that button.
    private void OnOpenPopupClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string resourceKey } button
            && Resources[resourceKey] is Popup popup)
        {
            // A popup kept in the resources is not part of the page, so it does not
            // inherit the page's DataContext and has to be given it.
            popup.DataContext = DataContext;
            popup.PlacementTarget = button;
            popup.IsOpen = true;
        }
    }

    // Puts the cursor in the first text box, so you can start typing straight away.
    private void OnAddPopupOpened(object? sender, EventArgs e)
    {
        if (sender is Popup { Child: { } content })
        {
            FindFirst<TextBox>(content)?.Focus();
        }
    }

    // Depth-first search through an element's children for the first one of a given type.
    private static T? FindFirst<T>(DependencyObject parent)
        where T : DependencyObject
    {
        foreach (DependencyObject child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (child is T match)
            {
                return match;
            }

            if (FindFirst<T>(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}