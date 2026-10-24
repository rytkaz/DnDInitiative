using System.Windows;
using System.Windows.Controls.Primitives;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Behaviors;

/// <summary>
/// Makes a <see cref="Thumb"/> resize a panel: ResizeHandle.PanelWidth="{Binding ListWidth}".
/// Dragging changes the bound <see cref="PanelSize"/>, and letting go saves it. A thumb on a
/// panel's left or top edge sets GrowsLeft or GrowsUp, since dragging towards the panel
/// then makes it smaller. A corner handle sets both a width and a height.
/// </summary>
public static class ResizeHandle
{
    public static readonly DependencyProperty PanelWidthProperty = DependencyProperty.RegisterAttached(
        "PanelWidth",
        typeof(PanelSize),
        typeof(ResizeHandle),
        new PropertyMetadata(null, OnSizeChanged));

    public static readonly DependencyProperty PanelHeightProperty = DependencyProperty.RegisterAttached(
        "PanelHeight",
        typeof(PanelSize),
        typeof(ResizeHandle),
        new PropertyMetadata(null, OnSizeChanged));

    public static readonly DependencyProperty GrowsLeftProperty = DependencyProperty.RegisterAttached(
        "GrowsLeft",
        typeof(bool),
        typeof(ResizeHandle),
        new PropertyMetadata(false));

    public static readonly DependencyProperty GrowsUpProperty = DependencyProperty.RegisterAttached(
        "GrowsUp",
        typeof(bool),
        typeof(ResizeHandle),
        new PropertyMetadata(false));

    public static PanelSize? GetPanelWidth(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (PanelSize?)element.GetValue(PanelWidthProperty);
    }

    public static void SetPanelWidth(DependencyObject element, PanelSize? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(PanelWidthProperty, value);
    }

    public static PanelSize? GetPanelHeight(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (PanelSize?)element.GetValue(PanelHeightProperty);
    }

    public static void SetPanelHeight(DependencyObject element, PanelSize? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(PanelHeightProperty, value);
    }

    public static bool GetGrowsLeft(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(GrowsLeftProperty);
    }

    public static void SetGrowsLeft(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(GrowsLeftProperty, value);
    }

    public static bool GetGrowsUp(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(GrowsUpProperty);
    }

    public static void SetGrowsUp(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(GrowsUpProperty, value);
    }

    private static void OnSizeChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not Thumb thumb)
        {
            return;
        }

        // Removed first, so a thumb with both a width and a height is not handled twice.
        thumb.DragDelta -= OnDragDelta;
        thumb.DragCompleted -= OnDragCompleted;
        thumb.DragDelta += OnDragDelta;
        thumb.DragCompleted += OnDragCompleted;
    }

    // The change is measured from where the thumb is now. The thumb sits on the edge that
    // moves, so it follows the mouse and each change is only the newest bit of the drag.
    private static void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        var thumb = (Thumb)sender;

        if (GetPanelWidth(thumb) is { } width)
        {
            width.Value += GetGrowsLeft(thumb) ? -e.HorizontalChange : e.HorizontalChange;
        }

        if (GetPanelHeight(thumb) is { } height)
        {
            height.Value += GetGrowsUp(thumb) ? -e.VerticalChange : e.VerticalChange;
        }
    }

    private static void OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        var thumb = (Thumb)sender;
        GetPanelWidth(thumb)?.Save();
        GetPanelHeight(thumb)?.Save();
    }
}
