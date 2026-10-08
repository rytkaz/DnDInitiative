using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Parties;

namespace DnDInitiative.App.Tests.ViewModels;

public class PartySetupViewModelTests
{
    [Fact]
    public async Task LoadCommand_ShowsStoredPartiesAndSelectsTheFirst()
    {
        var stored = new Party(Guid.NewGuid(), "Heroes", [new PlayerCharacter("Aria", Level: 3)]);
        var viewModel = new PartySetupViewModel(new InMemoryPartyRepository(stored));

        await viewModel.LoadCommand.ExecuteAsync(null);

        PartyEditorViewModel editor = Assert.Single(viewModel.Parties);
        Assert.Equal("Heroes", editor.Name);
        Assert.Same(editor, viewModel.SelectedParty);
    }

    [Fact]
    public void SaveCommand_NothingSelected_CannotExecute()
    {
        var viewModel = new PartySetupViewModel(new InMemoryPartyRepository());

        Assert.False(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_ValidParty_StoresTrimmedParty()
    {
        var repository = new InMemoryPartyRepository();
        var viewModel = new PartySetupViewModel(repository);
        viewModel.NewPartyCommand.Execute(null);
        PartyEditorViewModel editor = SelectedEditor(viewModel);
        editor.Name = "  Heroes  ";
        editor.AddMemberCommand.Execute(null);
        PlayerCharacterViewModel member = Assert.Single(editor.Members);
        member.Name = "Aria";
        member.Level = 3;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Party saved = Assert.Single(repository.Parties);
        Assert.Equal("Heroes", saved.Name);
        Assert.Equal(new PlayerCharacter("Aria", Level: 3), Assert.Single(saved.Members));
    }

    [Fact]
    public async Task SaveCommand_MemberWithoutName_DoesNotSaveAndExplainsWhy()
    {
        var repository = new InMemoryPartyRepository();
        var viewModel = new PartySetupViewModel(repository);
        viewModel.NewPartyCommand.Execute(null);
        SelectedEditor(viewModel).AddMemberCommand.Execute(null);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Empty(repository.Parties);
        Assert.NotEqual("", viewModel.StatusMessage);
    }

    [Fact]
    public async Task DeleteCommand_RemovesPartyFromStorageAndList()
    {
        var stored = new Party(Guid.NewGuid(), "Heroes", []);
        var repository = new InMemoryPartyRepository(stored);
        var viewModel = new PartySetupViewModel(repository);
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Empty(repository.Parties);
        Assert.Empty(viewModel.Parties);
        Assert.Null(viewModel.SelectedParty);
    }

    private static PartyEditorViewModel SelectedEditor(PartySetupViewModel viewModel)
    {
        return Assert.IsType<PartyEditorViewModel>(viewModel.SelectedParty);
    }
}