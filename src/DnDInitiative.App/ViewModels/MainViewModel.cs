using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.Core.Campaigns;

namespace DnDInitiative.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IRecipient<StartCombatMessage>
{
    private const string AppTitle = "DnD Initiative";

    private readonly InitiativeTrackerViewModel _initiativeTracker;
    private readonly IConfirmationService _confirmation;
    private readonly ICampaignService _campaigns;
    private readonly IFolderPicker _folderPicker;
    private readonly IUiStateStore _uiState;
    private PageViewModel _currentPage;
    // Brings change notifications from the watcher's background thread to the UI thread.
    private readonly UiProgress<bool> _externalChange;

    /// <summary>Files changed on disk, but reloading now would discard something.</summary>
    [ObservableProperty]
    private bool _hasPendingExternalChanges;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string _campaignName;

    /// <summary>Why the last attempt to open a campaign failed, or empty.</summary>
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
        ImportViewModel import,
        IMessenger messenger,
        IConfirmationService confirmation,
        ICampaignService campaigns,
        IFolderPicker folderPicker,
        IUiStateStore uiState)
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(uiState);

        _initiativeTracker = initiativeTracker;
        _confirmation = confirmation;
        _campaigns = campaigns;
        _folderPicker = folderPicker;
        _uiState = uiState;

        // Assigned to the field, not the property, so restoring the state does not save it again.
        _isNavigationCollapsed = uiState.Load().IsNavigationCollapsed;
        Pages = [initiativeTracker, statBlocks, encounterBuilder, partySetup, import];
        _currentPage = initiativeTracker;

        // Storage opens a campaign during start-up, before any window exists.
        _campaignName = campaigns.Current?.Name ?? "";
        messenger.Register<StartCombatMessage>(this);

        _externalChange = new UiProgress<bool>(_ => HandleExternalChangeAsync().Forget());
        campaigns.FilesChangedExternally += (_, _) => _externalChange.Report(true);
    }

    public IReadOnlyList<PageViewModel> Pages { get; }

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
    private void ToggleNavigation()
    {
        IsNavigationCollapsed = !IsNavigationCollapsed;
    }

    partial void OnIsNavigationCollapsedChanged(bool value)
    {
        _uiState.Save(new UiState { IsNavigationCollapsed = value });
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

    /// <summary>Returns false if the user chose to stay because of unsaved changes.</summary>
    public bool ConfirmClose()
    {
        List<string> unsaved = Pages.SelectMany(page => page.UnsavedChanges).ToList();
        if (unsaved.Count == 0)
        {
            return true;
        }

        return _confirmation.Confirm("Unsaved changes", DescribeLoss(unsaved, "Close anyway?"));
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

        if (!ConfirmLeavingCampaign())
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

    private bool ConfirmLeavingCampaign()
    {
        List<string> losses = Pages.SelectMany(page => page.UnsavedChanges).ToList();
        if (_initiativeTracker.Combatants.Count > 0)
        {
            losses.Add("The combat in the initiative tracker");
        }

        return losses.Count == 0
            || _confirmation.Confirm("Open another campaign", DescribeLoss(losses, "Open the campaign anyway?"));
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
        if (!ConfirmLeavingCampaign())
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
}