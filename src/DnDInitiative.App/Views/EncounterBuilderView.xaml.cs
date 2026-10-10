using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

public partial class EncounterBuilderView : UserControl
{
    public EncounterBuilderView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is EncounterBuilderViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    // The strip sits on the panel's left edge, so dragging left (a negative change) makes the panel wider.
    private void OnPanelResizeDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is EncounterBuilderViewModel viewModel)
        {
            viewModel.StatBlockPanelWidth -= e.HorizontalChange;
        }
    }

    // Saved once, when the mouse button is released, not for every step of the drag.
    private void OnPanelResizeDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is EncounterBuilderViewModel viewModel)
        {
            viewModel.SaveLayout();
        }
    }
}