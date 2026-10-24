using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.App.Services;
using DnDInitiative.Core.Parties;

namespace DnDInitiative.App.ViewModels;

public sealed partial class PartySetupViewModel : PageViewModel
{
    private readonly IPartyRepository _repository;
    private readonly IConfirmationService _confirmation;
    private bool _isLoaded;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private PartyEditorViewModel? _selectedParty;

    [ObservableProperty]
    private string _statusMessage = "";

    public override string Title => "Party Setup";

    public override string ShortTitle => "Party";

    public ObservableCollection<PartyEditorViewModel> Parties { get; } = [];

    private bool HasSelectedParty => SelectedParty is not null;

    public override IReadOnlyList<string> UnsavedChanges => Parties
        .Where(party => party.HasUnsavedChanges)
        .Select(party => $"Party: {party.Name}")
        .ToList();

    public PartySetupViewModel(IPartyRepository repository, IConfirmationService confirmation, IUiStateStore uiState)
    {
        _repository = repository;
        _confirmation = confirmation;
        PartyListWidth = new PanelSize(uiState, "PartySetup.PartyList", defaultValue: 240, minimum: 160, maximum: 500);
    }

    /// <summary>Width of the list of parties, on the left.</summary>
    public PanelSize PartyListWidth { get; }

    public override async Task ReloadCampaignDataAsync()
    {
        Parties.Clear();
        SelectedParty = null;
        StatusMessage = "";

        // Makes the next load read from storage again instead of returning early.
        _isLoaded = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        // The view asks for a load every time it appears. Only the first
        // one hits the disk, so navigating away and back keeps unsaved edits.
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;

        IReadOnlyList<Party> parties = await _repository.GetAllAsync();
        foreach (Party party in parties)
        {
            Parties.Add(PartyEditorViewModel.FromParty(party));
        }

        SelectedParty = Parties.FirstOrDefault();
    }

    [RelayCommand]
    private void NewParty()
    {
        var party = new PartyEditorViewModel(Guid.NewGuid()) { Name = "New party" };
        Parties.Add(party);
        SelectedParty = party;
        StatusMessage = "";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedParty))]
    private async Task SaveAsync()
    {
        if (SelectedParty is not { } editor)
        {
            return;
        }

        if (!editor.Validate())
        {
            StatusMessage = "Fix the highlighted fields before saving.";
            return;
        }

        await StoreAsync(editor);
        StatusMessage = $"Saved {editor.Name}.";
    }

    public override async Task<IReadOnlyList<string>> SaveAllAsync()
    {
        var failures = new List<string>();
        foreach (PartyEditorViewModel editor in Parties.Where(party => party.HasUnsavedChanges))
        {
            if (editor.Validate())
            {
                await StoreAsync(editor);
            }
            else
            {
                string name = string.IsNullOrWhiteSpace(editor.Name) ? "(no name)" : editor.Name;
                failures.Add($"Party: {name} has fields that need fixing");
            }
        }

        return failures;
    }

    private async Task StoreAsync(PartyEditorViewModel editor)
    {
        await _repository.SaveAsync(editor.ToParty());
        editor.MarkSaved();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedParty))]
    private async Task DeleteAsync()
    {
        if (SelectedParty is not { } editor)
        {
            return;
        }

        if (!_confirmation.Confirm("Delete party", $"Delete {editor.Name}? This cannot be undone."))
        {
            return;
        }

        await _repository.DeleteAsync(editor.Id);
        Parties.Remove(editor);
        SelectedParty = Parties.FirstOrDefault();
        StatusMessage = $"Deleted {editor.Name}.";
    }
}