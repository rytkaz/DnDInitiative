using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;

using DnDInitiative.App.Formatting;
using DnDInitiative.App.Services;
using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IUiStateStore _uiState;
    private readonly SpellViewerViewModel _spellViewer;
    private readonly ILinkOpener _linkOpener;

    public MainWindow(
        MainViewModel viewModel,
        IUiStateStore uiState,
        SpellViewerViewModel spellViewer,
        ILinkOpener linkOpener)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _uiState = uiState;
        _spellViewer = spellViewer;
        _linkOpener = linkOpener;
        DataContext = viewModel;
        SpellPopup.DataContext = spellViewer;

        // A clicked link raises RequestNavigate, which travels up from the link through
        // everything that contains it. Listening here catches links on every page. The card
        // in the popup listens as well: a spell's text can link to other spells, and a popup
        // is a separate little window that may not pass the event on.
        var onLinkClicked = new RequestNavigateEventHandler(OnLinkClicked);
        AddHandler(Hyperlink.RequestNavigateEvent, onLinkClicked);
        SpellCard.AddHandler(Hyperlink.RequestNavigateEvent, onLinkClicked);

        // Once text in the popup has been clicked, the keyboard belongs to the popup and the
        // window no longer sees its keys. So the card closes itself on Escape as well.
        SpellCard.PreviewKeyDown += (_, e) => CloseSpellOnEscape(e);

        // Before the window is shown, so it opens at its old size instead of jumping to it.
        RestoreSize();
    }

    private void OnLinkClicked(object sender, RequestNavigateEventArgs e)
    {
        if (SpellLink.ToReference(e.Uri) is { } reference)
        {
            _spellViewer.ShowCommand.Execute(reference);

            // Stops the event here, so it is not handled a second time further up.
            e.Handled = true;
        }
        else if (WebLink.IsWebAddress(e.Uri))
        {
            // Nothing to do if no browser starts; the link's tooltip still shows the address.
            _linkOpener.TryOpen(e.Uri);
            e.Handled = true;
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        // Until something in the popup is clicked, the keyboard is still the window's.
        CloseSpellOnEscape(e);
    }

    private void CloseSpellOnEscape(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _spellViewer.IsOpen)
        {
            _spellViewer.CloseCommand.Execute(null);
            e.Handled = true;
        }
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