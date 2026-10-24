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
    private readonly ReferenceViewerViewModel _referenceViewer;
    private readonly ILinkOpener _linkOpener;
    private readonly IWindowService _windows;

    // Set once the unsaved changes have been dealt with, so closing again does not ask again.
    private bool _isCloseConfirmed;

    public MainWindow(
        MainViewModel viewModel,
        IUiStateStore uiState,
        ReferenceViewerViewModel referenceViewer,
        ILinkOpener linkOpener,
        IWindowService windows)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _uiState = uiState;
        _referenceViewer = referenceViewer;
        _linkOpener = linkOpener;
        _windows = windows;
        DataContext = viewModel;
        ReferencePopup.DataContext = referenceViewer;

        // A clicked link raises RequestNavigate, which travels up from the link through
        // everything that contains it. Listening here catches links on every page. The card
        // in the popup listens as well: a spell's text can link to other spells and conditions, and a popup
        // is a separate little window that may not pass the event on.
        var onLinkClicked = new RequestNavigateEventHandler(OnLinkClicked);
        AddHandler(Hyperlink.RequestNavigateEvent, onLinkClicked);
        ReferenceCard.AddHandler(Hyperlink.RequestNavigateEvent, onLinkClicked);

        // A menu item raises Click before it runs its command, so this catches the edit still being typed first.
        AddHandler(MenuItem.ClickEvent, new RoutedEventHandler((_, _) => PushPendingEdit()));

        // Once text in the popup has been clicked, the keyboard belongs to the popup and the
        // window no longer sees its keys. So the card closes itself on Escape as well.
        ReferenceCard.PreviewKeyDown += (_, e) => ClosePopupOnEscape(e);

        // Before the window is shown, so it opens at its old size instead of jumping to it.
        RestoreSize();
    }

    private void OnLinkClicked(object sender, RequestNavigateEventArgs e)
    {
        if (ReferenceLink.Spell.ToReference(e.Uri) is { } spell)
        {
            _referenceViewer.ShowSpellCommand.Execute(spell);

            // Stops the event here, so it is not handled a second time further up.
            e.Handled = true;
        }
        else if (ReferenceLink.Condition.ToReference(e.Uri) is { } condition)
        {
            _referenceViewer.ShowConditionCommand.Execute(condition);
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

        // A shortcut such as Ctrl+S runs its command while the text box being typed in keeps the focus.
        if (InputBindings.OfType<KeyBinding>().Any(binding => binding.Gesture.Matches(this, e)))
        {
            PushPendingEdit();
        }

        // Until something in the popup is clicked, the keyboard is still the window's.
        ClosePopupOnEscape(e);
    }

    private void ClosePopupOnEscape(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _referenceViewer.IsOpen)
        {
            _referenceViewer.CloseCommand.Execute(null);
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

    /// <summary>
    /// Some text boxes only write to their view model when they lose focus, and clicking the
    /// window's close button, a menu or a shortcut does not take focus away. Push the text
    /// across first, or an edit still being typed would be missed.
    /// </summary>
    private void PushPendingEdit()
    {
        // While a menu is open the keyboard is in the menu, but the text box still has the window's focus.
        TextBox? textBox = Keyboard.FocusedElement as TextBox ?? FocusManager.GetFocusedElement(this) as TextBox;
        textBox?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        // Through the normal close, so unsaved changes are asked about.
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        PushPendingEdit();

        if (!e.Cancel && !_isCloseConfirmed)
        {
            switch (_viewModel.ConfirmClose())
            {
                case CloseAction.Close:
                    break;

                case CloseAction.Stay:
                    e.Cancel = true;
                    break;

                case CloseAction.SaveThenClose:
                    // Saving is asynchronous and closing is not, so stay open for now and close again once saved.
                    e.Cancel = true;
                    SaveThenCloseAsync().Forget();
                    break;
            }
        }

        // Only once the window is really closing; a cancelled close will come round again.
        if (!e.Cancel)
        {
            SaveSize();

            // The import windows go with it. A running import is cancelled without a second question.
            _windows.CloseAll();
        }
    }

    private async Task SaveThenCloseAsync()
    {
        // Close() cannot be called while the window is still in OnClosing, which is where this starts.
        await System.Windows.Threading.Dispatcher.Yield();

        // If anything could not be saved, stay open: the campaign panel says what, so it can be fixed.
        if (await _viewModel.SaveAllAsync())
        {
            _isCloseConfirmed = true;
            Close();
        }
    }
}