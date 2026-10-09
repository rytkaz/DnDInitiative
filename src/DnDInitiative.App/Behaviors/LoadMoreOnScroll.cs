using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DnDInitiative.App.Behaviors;

/// <summary>
/// Runs a command when a scrollable control is scrolled close to its end:
/// LoadMoreOnScroll.Command="{Binding LoadMoreCommand}".
/// </summary>
public static class LoadMoreOnScroll
{
    // How close to the end counts as "near". The unit is items or pixels, depending on the control.
    private const double Threshold = 10;

    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command",
        typeof(ICommand),
        typeof(LoadMoreOnScroll),
        new PropertyMetadata(null, OnCommandChanged));

    public static ICommand? GetCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (ICommand?)element.GetValue(CommandProperty);
    }

    public static void SetCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(CommandProperty, value);
    }

    private static void OnCommandChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not UIElement uiElement)
        {
            return;
        }

        var handler = new ScrollChangedEventHandler(OnScrollChanged);
        uiElement.RemoveHandler(ScrollViewer.ScrollChangedEvent, handler);
        if (e.NewValue is not null)
        {
            uiElement.AddHandler(ScrollViewer.ScrollChangedEvent, handler);
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not DependencyObject element || e.ExtentHeight <= 0)
        {
            return;
        }

        bool nearEnd = e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - Threshold;
        ICommand? command = GetCommand(element);
        if (nearEnd && command is not null && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}