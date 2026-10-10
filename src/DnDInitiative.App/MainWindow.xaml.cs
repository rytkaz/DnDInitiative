using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using DnDInitiative.App.Services;
using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IUiStateStore _uiState;

    public MainWindow(MainViewModel viewModel, IUiStateStore uiState)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _uiState = uiState;
        DataContext = viewModel;

        // Before the window is shown, so it opens at its old size instead of jumping to it.
        RestoreSize();
    }

    private void RestoreSize()
    {
        UiState state = _uiState.Load();

        if (state.WindowWidth is { } width && state.WindowHeight is { } height)
        {
            // Not smaller than the window allows, and not larger than the whole desktop:
            // the size may have been saved on a bigger monitor than the one in use now.
            Width = Math.Max(MinWidth, Math.Min(width, SystemParameters.VirtualScreenWidth));
            Height = Math.Max(MinHeight, Math.Min(height, SystemParameters.VirtualScreenHeight));
        }

        if (state.IsWindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void SaveSize()
    {
        // While maximized (or minimized) Width and Height are not the size the window returns
        // to. RestoreBounds is, so the next run can open maximized and still un-maximize sensibly.
        Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        UiState state = _uiState.Load() with { IsWindowMaximized = WindowState == WindowState.Maximized };

        if (!bounds.IsEmpty)
        {
            state = state with { WindowWidth = bounds.Width, WindowHeight = bounds.Height };
        }

        _uiState.Save(state);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        // Most text boxes only write to their view model when they lose focus, and clicking
        // the window's close button does not take focus away. Push the text across first,
        // or an edit still being typed would be missed.
        if (Keyboard.FocusedElement is TextBox textBox)
        {
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }

        if (!e.Cancel && !_viewModel.ConfirmClose())
        {
            e.Cancel = true;
        }

        // Only once the window is really closing; a cancelled close will come round again.
        if (!e.Cancel)
        {
            SaveSize();
        }
    }
}