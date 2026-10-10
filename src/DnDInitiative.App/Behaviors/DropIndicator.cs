using System.Windows;

namespace DnDInitiative.App.Behaviors;

/// <summary>Where a dragged item would land relative to the element it is over.</summary>
public enum DropPosition
{
    None,
    Above,
    Below,
}

/// <summary>
/// Marks an element as the current drop target, so its template can draw an insertion line.
/// </summary>
public static class DropIndicator
{
    public static readonly DependencyProperty PositionProperty = DependencyProperty.RegisterAttached(
        "Position",
        typeof(DropPosition),
        typeof(DropIndicator),
        new PropertyMetadata(DropPosition.None));

    public static DropPosition GetPosition(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (DropPosition)element.GetValue(PositionProperty);
    }

    public static void SetPosition(DependencyObject element, DropPosition value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(PositionProperty, value);
    }
}