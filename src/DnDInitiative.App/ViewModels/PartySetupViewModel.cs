using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.Core.Parties;

namespace DnDInitiative.App.ViewModels;

public sealed partial class PartySetupViewModel : PageViewModel
{
    private readonly IPartyRepository _repository;
    private bool _isLoaded;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private PartyEditorViewModel? _selectedParty;

    [ObservableProperty]
    private string _statusMessage = "";

    public PartySetupViewModel(IPartyRepository repository)
    {
        _repository = repository;
    }

    public override string Title => "Party Setup";

    public ObservableCollection<PartyEditorViewModel> Parties { get; } = [];

    private bool HasSelectedParty => SelectedParty is not null;

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

        await _repository.SaveAsync(editor.ToParty());
        StatusMessage = $"Saved {editor.Name}.";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedParty))]
    private async Task DeleteAsync()
    {
        if (SelectedParty is not { } editor)
        {
            return;
        }

        await _repository.DeleteAsync(editor.Id);
        Parties.Remove(editor);
        SelectedParty = Parties.FirstOrDefault();
        StatusMessage = $"Deleted {editor.Name}.";
    }
}