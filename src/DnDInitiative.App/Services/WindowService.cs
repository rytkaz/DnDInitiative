using System.ComponentModel;
using System.Windows;

using DnDInitiative.App.ViewModels;
using DnDInitiative.App.Views;

namespace DnDInitiative.App.Services;

public sealed class WindowService : IWindowService
{
    private readonly ImportViewModel _fiveEToolsImport;
    private readonly HomebreweryImportViewModel _homebreweryImport;
    private readonly MartialExploitImportViewModel _martialExploitImport;
    private readonly IConfirmationService _confirmation;
    private readonly IUiStateStore _uiState;

    // One window per import. Choosing the menu item again brings the open one forward
    // instead of starting a second import alongside the first.
    private readonly Dictionary<ImportKind, ImportWindow> _importWindows = [];

    private bool _isClosingAll;

    public WindowService(
        ImportViewModel fiveEToolsImport,
        HomebreweryImportViewModel homebreweryImport,
        MartialExploitImportViewModel martialExploitImport,
        IConfirmationService confirmation,
        IUiStateStore uiState)
    {
        _fiveEToolsImport = fiveEToolsImport;
        _homebreweryImport = homebreweryImport;
        _martialExploitImport = martialExploitImport;
        _confirmation = confirmation;
        _uiState = uiState;
    }

    public void ShowImport(ImportKind kind)
    {
        if (_importWindows.TryGetValue(kind, out ImportWindow? openWindow))
        {
            BringToFront(openWindow);
            return;
        }

        // The view models are singletons, so the settings and the book list survive closing the window.
        IImportViewModel viewModel = ViewModelFor(kind);
        var window = new ImportWindow(viewModel, _uiState);

        // Owned, so it stays in front of the main window and opens centred on it. Not modal:
        // a 5eTools conversion takes minutes, and the rest of the app stays usable meanwhile.
        if (Application.Current?.MainWindow is { IsLoaded: true } owner)
        {
            window.Owner = owner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        window.Closing += (_, e) => OnImportWindowClosing(viewModel, e);
        window.Closed += (_, _) => _importWindows.Remove(kind);
        _importWindows[kind] = window;
        window.Show();
    }

    public void CloseAll()
    {
        _isClosingAll = true;
        try
        {
            // A copy, because each window removes itself from the dictionary as it closes.
            foreach (ImportWindow window in _importWindows.Values.ToList())
            {
                window.Close();
            }
        }
        finally
        {
            _isClosingAll = false;
        }
    }

    private void OnImportWindowClosing(IImportViewModel viewModel, CancelEventArgs e)
    {
        if (!viewModel.RunCommand.IsRunning)
        {
            return;
        }

        // Closing the window would otherwise leave the import running unseen, with no way to cancel it.
        if (!_isClosingAll
            && !_confirmation.Confirm("Import running", "The import is still running. Cancel it and close the window?"))
        {
            e.Cancel = true;
            return;
        }

        viewModel.RunCommand.Cancel();
    }

    private IImportViewModel ViewModelFor(ImportKind kind)
    {
        return kind switch
        {
            ImportKind.FiveETools => _fiveEToolsImport,
            ImportKind.Homebrewery => _homebreweryImport,
            ImportKind.MartialExploits => _martialExploitImport,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "There is no window for this import."),
        };
    }

    private static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }
}
