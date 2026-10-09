using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.Core.Parties;

namespace DnDInitiative.App.ViewModels;

public sealed partial class PartyEditorViewModel : ObservableValidator
{
    private readonly SavedState<Party> _savedState;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "A party needs a name.")]
    private string _name = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveMemberCommand))]
    private PlayerCharacterViewModel? _selectedMember;

    public bool HasUnsavedChanges => _savedState.HasChanges;

    public PartyEditorViewModel(Guid id)
    {
        Id = id;
        _savedState = new SavedState<Party>(ToParty);
    }

    public void MarkSaved()
    {
        _savedState.MarkSaved();
    }

    public Guid Id { get; }

    public ObservableCollection<PlayerCharacterViewModel> Members { get; } = [];

    private bool HasSelectedMember => SelectedMember is not null;

    public static PartyEditorViewModel FromParty(Party party)
    {
        ArgumentNullException.ThrowIfNull(party);

        var editor = new PartyEditorViewModel(party.Id) { Name = party.Name };
        foreach (PlayerCharacter member in party.Members)
        {
            editor.Members.Add(PlayerCharacterViewModel.FromPlayerCharacter(member));
        }

        editor.MarkSaved();
        return editor;
    }

    public Party ToParty()
    {
        return new Party(Id, Name.Trim(), Members.Select(member => member.ToPlayerCharacter()).ToList());
    }

    public bool Validate()
    {
        ValidateAllProperties();

        // Validate every member, not just up to the first failure,
        // so all invalid cells get highlighted at once.
        bool membersAreValid = true;
        foreach (PlayerCharacterViewModel member in Members)
        {
            membersAreValid &= member.Validate();
        }

        return !HasErrors && membersAreValid;
    }

    [RelayCommand]
    private void AddMember()
    {
        var member = new PlayerCharacterViewModel();
        Members.Add(member);
        SelectedMember = member;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMember))]
    private void RemoveMember()
    {
        if (SelectedMember is { } member)
        {
            Members.Remove(member);
        }
    }
}