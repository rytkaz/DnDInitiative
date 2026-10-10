using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

public partial class ImportView : UserControl
{
    public ImportView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ImportViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    // Keeps the newest output in view, like a console window.
    private void OnLogTextChanged(object sender, TextChangedEventArgs e)
    {
        ((TextBox)sender).ScrollToEnd();
    }

    // The strip sits under the list, so dragging down (a positive change) makes the list taller.
    private void OnSourcesResizeDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is ImportViewModel viewModel)
        {
            viewModel.SourcesPanelHeight += e.VerticalChange;
        }
    }

    // Saved once, when the mouse button is released, not for every step of the drag.
    private void OnSourcesResizeDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is ImportViewModel viewModel)
        {
            viewModel.SaveLayout();
        }
    }
}