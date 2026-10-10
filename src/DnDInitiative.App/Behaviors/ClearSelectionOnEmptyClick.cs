using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDInitiative.App.Behaviors;

/// <summary>
/// Lets XAML give a DataGrid Explorer's habit of clearing the selection when the empty space
/// under the last row is clicked: behaviors:ClearSelectionOnEmptyClick.IsEnabled="True".
/// </summary>
public static class ClearSelectionOnEmptyClick
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(ClearSelectionOnEmptyClick),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not DataGrid grid)
        {
            return;
        }

        // Removed first, so turning it on twice does not listen twice.
        grid.PreviewMouseLeftButtonDown -= OnGridMouseDown;
        if ((bool)e.NewValue)
        {
            grid.PreviewMouseLeftButtonDown += OnGridMouseDown;
        }
    }

    private static void OnGridMouseDown(object sender, MouseButtonEventArgs e)
    {
        var grid = (DataGrid)sender;

        // Setting SelectedItem also updates whatever view model property it is bound to.
        if (IsEmptySpace(grid, e.OriginalSource as DependencyObject))
        {
            grid.SelectedItem = null;
        }
    }

    // True when the click landed on the grid itself: not on a row, a column header or a scroll bar.
    private static bool IsEmptySpace(DataGrid grid, DependencyObject? element)
    {
        while (element is not null && !ReferenceEquals(element, grid))
        {
            if (element is DataGridRow or DataGridColumnHeader or ScrollBar)
            {
                return false;
            }

            // Text inside a cell is not a visual; it has only a logical parent.
            element = element is Visual
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return element is not null;
    }
}
