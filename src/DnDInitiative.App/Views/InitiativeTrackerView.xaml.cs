using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

public partial class InitiativeTrackerView : UserControl
{
    private CombatantViewModel? _dragRow;
    private Point _dragStart;

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
    }

    // Accept our own rows anywhere in the grid, including over the text boxes.
    private void OnGridPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(CombatantViewModel)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnGridPreviewDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (DataContext is InitiativeTrackerViewModel viewModel
            && e.Data.GetData(typeof(CombatantViewModel)) is CombatantViewModel dragged
            && FindRow(e.OriginalSource as DependencyObject)?.DataContext is CombatantViewModel target)
        {
            viewModel.MoveCombatant(dragged, target);
        }
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
}