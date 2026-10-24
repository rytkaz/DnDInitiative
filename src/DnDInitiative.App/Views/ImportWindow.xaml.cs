using System.ComponentModel;
using System.Windows;

using DnDInitiative.App.Services;
using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

public partial class ImportWindow : Window
{
    private readonly IUiStateStore _uiState;

    public ImportWindow(IImportViewModel viewModel, IUiStateStore uiState)
    {
        InitializeComponent();
        _uiState = uiState;
        DataContext = viewModel;

        // Before the window is shown, so it opens at its old size instead of jumping to it.
        RestoreSize();
    }

    // Both import windows share one remembered size: they hold similar forms, and a size
    // dragged for one suits the other.
    private void RestoreSize()
    {
        UiState state = _uiState.Load();

        if (state.ImportWindowWidth is { } width && state.ImportWindowHeight is { } height)
        {
            // The size may have been saved on a bigger monitor than the one in use now.
            Width = Math.Max(MinWidth, Math.Min(width, SystemParameters.VirtualScreenWidth));
            Height = Math.Max(MinHeight, Math.Min(height, SystemParameters.VirtualScreenHeight));
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Raises Closing, where the window service asks about a running import.
        base.OnClosing(e);

        if (!e.Cancel)
        {
            // While maximized or minimized, Width and Height are not the size the window returns to.
            Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
            if (!bounds.IsEmpty)
            {
                _uiState.Save(_uiState.Load() with { ImportWindowWidth = bounds.Width, ImportWindowHeight = bounds.Height });
            }
        }
    }
}
