using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.Core.Campaigns;

namespace DnDInitiative.App.ViewModels;

/// <summary>What the window should do after the user was asked about closing.</summary>
public enum CloseAction
{
    Close,
    Stay,
    SaveThenClose,
}

public sealed partial class MainViewModel : ObservableObject, IRecipient<StartCombatMessage>
{
    private const string AppTitle = "DnD Initiative";

    private readonly InitiativeTrackerViewModel _initiativeTracker;
    private readonly IConfirmationService _confirmation;
    private readonly IUnsavedChangesPrompt _unsavedChangesPrompt;
    private readonly ICampaignService _campaigns;
    private readonly IFolderPicker _folderPicker;
    private readonly IUiStateStore _uiState;
    private readonly IWindowService _windows;
    private PageViewModel _currentPage;
    // Brings change notifications from the watcher's background thread to the UI thread.
    private readonly UiProgress<bool> _externalChange;

    /// <summary>Files changed on disk, but reloading now would discard something.</summary>
    [ObservableProperty]
    private bool _hasPendingExternalChanges;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string _campaignName;

    /// <summary>The outcome of the last save, or why the last attempt to open a campaign failed, or empty.</summary>
    [ObservableProperty]
    private string _campaignMessage = "";

    /// <summary>Whether the page list and campaign panel are folded away to give the page more room.</summary>
    [ObservableProperty]
    private bool _isNavigationCollapsed;

    public MainViewModel(
        InitiativeTrackerViewModel initiativeTracker,
        StatBlocksViewModel statBlocks,
        EncounterBuilderViewModel encounterBuilder,
        PartySetupViewModel partySetup,
        SpellbookViewModel spellbook,
        IMessenger messenger,
        IConfirmationService confirmation,
        IUnsavedChangesPrompt unsavedChangesPrompt,
        ICampaignService campaigns,
        IFolderPicker folderPicker,
        IUiStateStore uiState,
        IWindowService windows)
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(uiState);

        _initiativeTracker = initiativeTracker;
        _confirmation = confirmation;
        _unsavedChangesPrompt = unsavedChangesPrompt;
        _campaigns = campaigns;
        _folderPicker = folderPicker;
        _uiState = uiState;
        _windows = windows;

        // Assigned to the field, not the property, so restoring the state does not save it again.
        _isNavigationCollapsed = uiState.Load().IsNavigationCollapsed;
        NavigationWidth = new PanelSize(uiState, "Main.Navigation", defaultValue: 220, minimum: 160, maximum: 480);
        // The imports are not pages: they open in windows of their own from the Import menu.
        Pages = [initiativeTracker, statBlocks, encounterBuilder, partySetup, spellbook];
        _currentPage = initiativeTracker;

        // Storage opens a campaign during start-up, before any window exists.
        _campaignName = campaigns.Current?.Name ?? "";
        messenger.Register<StartCombatMessage>(this);

        _externalChange = new UiProgress<bool>(_ => HandleExternalChangeAsync().Forget());
        campaigns.FilesChangedExternally += (_, _) => _externalChange.Report(true);
    }

    public IReadOnlyList<PageViewModel> Pages { get; }

    /// <summary>Width of the page list while it is open. Folded away, it is only as wide as its tabs.</summary>
    public PanelSize NavigationWidth { get; }

    public PageViewModel CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public string WindowTitle => CampaignName.Length == 0 ? AppTitle : $"{CampaignName} - {AppTitle}";

    public void Receive(StartCombatMessage message)
    {
        CurrentPage = _initiativeTracker;
    }

    [RelayCommand]
    private void ShowImport(ImportKind kind)
    {
        _windows.ShowImport(kind);
    }

    [RelayCommand]
    private void ToggleNavigation()
    {
        IsNavigationCollapsed = !IsNavigationCollapsed;
    }

    partial void OnIsNavigationCollapsedChanged(bool value)
    {
        // "with" copies the saved state and changes one property, so the pages' panel sizes survive.
        _uiState.Save(_uiState.Load() with { IsNavigationCollapsed = value });
    }

    // The Reload button lives in the panel. If the panel is folded away when files change
    // on disk, bring it back so the offer to reload is not missed.
    partial void OnHasPendingExternalChangesChanged(bool value)
    {
        if (value)
        {
            IsNavigationCollapsed = false;
        }
    }

    /// <summary>Asks about unsaved changes, if there are any, before the window closes.</summary>
    public CloseAction ConfirmClose()
    {
        List<string> unsaved = Pages.SelectMany(page => page.UnsavedChanges).ToList();
        if (unsaved.Count == 0)
        {
            return CloseAction.Close;
        }

        return _unsavedChangesPrompt.Ask("Unsaved changes", DescribeUnsaved(unsaved, combatIsLost: false)) switch
        {
            UnsavedChangesChoice.Save => CloseAction.SaveThenClose,
            UnsavedChangesChoice.Discard => CloseAction.Close,
            _ => CloseAction.Stay,
        };
    }

    /// <summary>
    /// Saves every unsaved encounter, party and stat block, and reports the outcome in
    /// <see cref="CampaignMessage"/>. Returns false if anything could not be saved.
    /// </summary>
    public async Task<bool> SaveAllAsync()
    {
        SaveOutcome outcome = await SavePagesAsync();
        CampaignMessage = outcome.Describe();
        return outcome.Failures.Count == 0;
    }

    [RelayCommand]
    private async Task SaveCampaignAsync()
    {
        await SaveAllAsync();
    }

    [RelayCommand]
    private async Task SaveCampaignAsAsync()
    {
        string? folder = _folderPicker.PickFolder("Pick an empty folder for the copy of the campaign");
        if (folder is null)
        {
            return;
        }

        try
        {
            // The saved files are copied first and the unsaved edits saved after, into the
            // copy only. The original stays as it was last saved.
            Campaign campaign = await _campaigns.SaveAsAsync(folder);
            CampaignName = campaign.Name;
        }
        catch (CampaignException exception)
        {
            // Nothing was switched; the current campaign is still open.
            CampaignMessage = exception.Message;
            ShowCampaignPanel();
            return;
        }

        // The pages are not reloaded: they show the same campaign, now in its new folder.
        SaveOutcome outcome = await SavePagesAsync();
        CampaignMessage = outcome.Failures.Count == 0
            ? $"Saved a copy in {folder}."
            : $"Copied the campaign to {folder}. {outcome.Describe()}";
    }

    private async Task<SaveOutcome> SavePagesAsync()
    {
        int unsavedCount = Pages.Sum(page => page.UnsavedChanges.Count);

        var failures = new List<string>();
        foreach (PageViewModel page in Pages)
        {
            failures.AddRange(await page.SaveAllAsync());
        }

        // The message saying what failed is in the campaign panel, so it must not stay folded away.
        if (failures.Count > 0)
        {
            ShowCampaignPanel();
        }

        return new SaveOutcome(unsavedCount - failures.Count, failures);
    }

    private void ShowCampaignPanel()
    {
        IsNavigationCollapsed = false;
    }

    [RelayCommand]
    private async Task OpenCampaignAsync()
    {
        string? folder = _folderPicker.PickFolder("Open a campaign folder, or pick an empty folder to start a new campaign");
        if (folder is null)
        {
            return;
        }

        if (_campaigns.Inspect(folder) == CampaignFolderState.HasOtherFiles
            && !_confirmation.Confirm(
                "Not a campaign folder",
                $"'{folder}' already contains other files. Campaign files will be added next to them. Use this folder anyway?"))
        {
            return;
        }

        if (!await ConfirmLeavingCampaignAsync("Open another campaign", "Open the campaign anyway?"))
        {
            return;
        }

        try
        {
            Campaign campaign = await _campaigns.OpenAsync(folder);
            CampaignName = campaign.Name;
            CampaignMessage = "";
            HasPendingExternalChanges = false;
        }
        catch (CampaignException exception)
        {
            // The previous campaign is still open; say why the new one is not.
            CampaignMessage = exception.Message;
            return;
        }

        await ReloadPagesAsync();
    }

    /// <summary>
    /// Before the pages drop what they hold: offers to save unsaved changes, and warns that
    /// a combat in progress, which cannot be saved, will be lost. Returns false to stay.
    /// </summary>
    private async Task<bool> ConfirmLeavingCampaignAsync(string title, string question)
    {
        List<string> unsaved = Pages.SelectMany(page => page.UnsavedChanges).ToList();
        bool hasCombat = _initiativeTracker.Combatants.Count > 0;

        if (unsaved.Count == 0)
        {
            // Nothing that could be saved, so there is nothing to offer but going ahead or not.
            return !hasCombat
                || _confirmation.Confirm(title, DescribeLoss(["The combat in the initiative tracker"], question));
        }

        return _unsavedChangesPrompt.Ask(title, DescribeUnsaved(unsaved, hasCombat)) switch
        {
            // If anything could not be saved, stay, so it is not lost after all.
            UnsavedChangesChoice.Save => await SaveAllAsync(),
            UnsavedChangesChoice.Discard => true,
            _ => false,
        };
    }

    private static string DescribeUnsaved(List<string> items, bool combatIsLost)
    {
        string blankLine = Environment.NewLine + Environment.NewLine;
        string combat = combatIsLost
            ? blankLine + "The combat in the initiative tracker cannot be saved and will be lost either way."
            : "";

        return "These have unsaved changes:"
            + blankLine
            + string.Join(Environment.NewLine, items)
            + combat
            + blankLine
            + "Save them first?";
    }

    private static string DescribeLoss(List<string> items, string question)
    {
        string blankLine = Environment.NewLine + Environment.NewLine;
        return "These will be lost:"
            + blankLine
            + string.Join(Environment.NewLine, items)
            + blankLine
            + question;
    }

    [RelayCommand]
    private async Task ReloadCampaignAsync()
    {
        if (!await ConfirmLeavingCampaignAsync("Reload the campaign", "Reload anyway?"))
        {
            return;
        }

        await ReloadPagesAsync();
        HasPendingExternalChanges = false;
        CampaignMessage = "";
    }

    private async Task HandleExternalChangeAsync()
    {
        // Never reload over the user's work without asking.
        if (HasSomethingToLose())
        {
            HasPendingExternalChanges = true;
            CampaignMessage = "Campaign files changed on disk.";
            return;
        }

        await ReloadPagesAsync();
        CampaignMessage = "Loaded changes from disk.";
    }

    private async Task ReloadPagesAsync()
    {
        foreach (PageViewModel page in Pages)
        {
            await page.ReloadCampaignDataAsync();
        }
    }

    private bool HasSomethingToLose()
    {
        return _initiativeTracker.Combatants.Count > 0 || Pages.Any(page => page.UnsavedChanges.Count > 0);
    }

    /// <summary>How many things a save stored, and a line for each one it could not.</summary>
    private sealed record SaveOutcome(int SavedCount, IReadOnlyList<string> Failures)
    {
        public string Describe()
        {
            if (Failures.Count > 0)
            {
                string saved = SavedCount > 0 ? DescribeSaved() + " " : "";
                return saved + "Not saved:" + Environment.NewLine + string.Join(Environment.NewLine, Failures);
            }

            return SavedCount == 0 ? "Nothing to save." : DescribeSaved();
        }

        private string DescribeSaved()
        {
            return SavedCount == 1
                ? "Saved 1 item."
                : string.Create(CultureInfo.InvariantCulture, $"Saved {SavedCount} items.");
        }
    }
}
