using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Campaigns;
using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;
using DnDInitiative.Core.Parties;

namespace DnDInitiative.App.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly FakeConfirmation _confirmation = new();
    private readonly FakeUnsavedChangesPrompt _unsavedChangesPrompt = new();
    private readonly InitiativeTrackerViewModel _tracker;
    private readonly EncounterBuilderViewModel _encounterBuilder;
    private readonly PartySetupViewModel _partySetup;
    private readonly SpellbookViewModel _spellbook;
    private readonly MainViewModel _viewModel;
    private readonly InMemoryPartyRepository _parties = new();
    private readonly FakeCampaignService _campaigns = new();
    private readonly FakeFolderPicker _folderPicker = new();
    private readonly FakeWindowService _windows = new();

    public MainViewModelTests()
    {
        var statBlocks = new InMemoryStatBlockRepository();

        _tracker = new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
            _parties,
            statBlocks,
            _messenger,
            new InMemoryUiStateStore());
        _encounterBuilder = new EncounterBuilderViewModel(
            new InMemoryEncounterRepository(),
            statBlocks,
            _parties,
            _messenger,
            new FakeClipboard(),
            _confirmation,
            new InMemoryUiStateStore(),
            new FixedTimeProvider());
        _partySetup = new PartySetupViewModel(_parties, _confirmation, new InMemoryUiStateStore());
        _spellbook = new SpellbookViewModel(
            new InMemorySpellbookRepository(),
            new InMemorySpellRepository(),
            new InMemoryAbilityRepository(),
            new InMemoryCustomSpellRepository(),
            new InMemoryCustomAbilityRepository(),
            new FakeCardFactory(),
            new FakeSpellbookPrinter(),
            _confirmation,
            new InMemoryUiStateStore(),
            new FixedTimeProvider());
        _viewModel = new MainViewModel(
            _tracker,
            new StatBlocksViewModel(statBlocks, _confirmation, _messenger, new InMemoryUiStateStore()),
            _encounterBuilder,
            _partySetup,
            _spellbook,
            _messenger,
            _confirmation,
            _unsavedChangesPrompt,
            _campaigns,
            _folderPicker,
            _windows);
    }

    [Fact]
    public void StartCombatMessage_NavigatesToTheTracker()
    {
        _viewModel.CurrentPage = _encounterBuilder;

        _messenger.Send(new StartCombatMessage("Ambush", [], Party: null));

        Assert.Same(_tracker, _viewModel.CurrentPage);
    }

    [Fact]
    public void ConfirmClose_NothingUnsaved_ClosesWithoutAsking()
    {
        Assert.Equal(CloseAction.Close, _viewModel.ConfirmClose());
        Assert.Empty(_unsavedChangesPrompt.Questions);
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Save, CloseAction.SaveThenClose)]
    [InlineData(UnsavedChangesChoice.Discard, CloseAction.Close)]
    [InlineData(UnsavedChangesChoice.Cancel, CloseAction.Stay)]
    public void ConfirmClose_UnsavedParty_NamesItAndFollowsTheAnswer(UnsavedChangesChoice answer, CloseAction expected)
    {
        _partySetup.NewPartyCommand.Execute(null);
        _unsavedChangesPrompt.Answer = answer;

        CloseAction action = _viewModel.ConfirmClose();

        Assert.Equal(expected, action);
        Assert.Contains("New party", Assert.Single(_unsavedChangesPrompt.Questions), StringComparison.Ordinal);
    }

    [Fact]
    public void WindowTitle_ShowsTheCampaignOpenedAtStartUp()
    {
        Assert.Equal("First", _viewModel.CampaignName);
        Assert.Equal("First - DnD Initiative", _viewModel.WindowTitle);
    }

    [Fact]
    public async Task OpenCampaignCommand_FolderChosen_OpensItAndReloadsThePages()
    {
        await _partySetup.LoadCommand.ExecuteAsync(null);
        Assert.Empty(_partySetup.Parties);

        // The "other campaign" has a party the first one did not.
        await _parties.SaveAsync(new Party(Guid.NewGuid(), "Villains", []));
        _folderPicker.FolderToReturn = @"C:\campaigns\Strahd";

        await _viewModel.OpenCampaignCommand.ExecuteAsync(null);

        Assert.Equal("Strahd", _viewModel.CampaignName);
        Assert.Equal("Villains", Assert.Single(_partySetup.Parties).Name);
    }

    [Fact]
    public async Task OpenCampaignCommand_DialogCancelled_DoesNothing()
    {
        _folderPicker.FolderToReturn = null;

        await _viewModel.OpenCampaignCommand.ExecuteAsync(null);

        Assert.Equal("First", _viewModel.CampaignName);
        Assert.Empty(_confirmation.Questions);
    }

    [Fact]
    public async Task OpenCampaignCommand_UnsavedChangesAndDeclined_StaysInTheCurrentCampaign()
    {
        _partySetup.NewPartyCommand.Execute(null);
        _folderPicker.FolderToReturn = @"C:\campaigns\Strahd";
        _unsavedChangesPrompt.Answer = UnsavedChangesChoice.Cancel;

        await _viewModel.OpenCampaignCommand.ExecuteAsync(null);

        Assert.Equal("First", _viewModel.CampaignName);
        Assert.Contains("New party", Assert.Single(_unsavedChangesPrompt.Questions), StringComparison.Ordinal);
        Assert.Single(_partySetup.Parties);
    }

    [Fact]
    public async Task OpenCampaignCommand_CombatInProgress_ClearsItWithoutAsking()
    {
        _tracker.NewName = "Goblin";
        _tracker.AddCreatureCommand.Execute(null);
        _folderPicker.FolderToReturn = @"C:\campaigns\Strahd";

        await _viewModel.OpenCampaignCommand.ExecuteAsync(null);

        Assert.Empty(_confirmation.Questions);
        Assert.Empty(_unsavedChangesPrompt.Questions);
        Assert.Empty(_tracker.Combatants);
    }

    [Fact]
    public async Task OpenCampaignCommand_FolderWithOtherFilesAndDeclined_DoesNotOpenIt()
    {
        _campaigns.FolderState = CampaignFolderState.HasOtherFiles;
        _folderPicker.FolderToReturn = @"C:\Users\me\Documents";
        _confirmation.Answer = false;

        await _viewModel.OpenCampaignCommand.ExecuteAsync(null);

        Assert.Equal("First", _viewModel.CampaignName);
    }

    [Fact]
    public async Task OpenCampaignCommand_CampaignCannotBeOpened_ShowsWhyAndKeepsTheCurrentOne()
    {
        _campaigns.FailureMessage = "Saved by a newer version.";
        _folderPicker.FolderToReturn = @"C:\campaigns\Future";

        await _viewModel.OpenCampaignCommand.ExecuteAsync(null);

        Assert.Equal("First", _viewModel.CampaignName);
        Assert.Equal("Saved by a newer version.", _viewModel.CampaignMessage);
    }

    [Fact]
    public async Task FilesChangedExternally_NothingToLose_ReloadsStraightAway()
    {
        await _partySetup.LoadCommand.ExecuteAsync(null);
        await _parties.SaveAsync(new Party(Guid.NewGuid(), "Pulled from git", []));

        _campaigns.RaiseFilesChanged();

        Assert.Equal("Pulled from git", Assert.Single(_partySetup.Parties).Name);
        Assert.False(_viewModel.HasPendingExternalChanges);
        Assert.Empty(_confirmation.Questions);
    }

    [Fact]
    public async Task FilesChangedExternally_UnsavedEdits_KeepsThemAndOffersAReload()
    {
        await _partySetup.LoadCommand.ExecuteAsync(null);
        _partySetup.NewPartyCommand.Execute(null);
        await _parties.SaveAsync(new Party(Guid.NewGuid(), "Pulled from git", []));

        _campaigns.RaiseFilesChanged();

        Assert.Equal("New party", Assert.Single(_partySetup.Parties).Name);
        Assert.True(_viewModel.HasPendingExternalChanges);
        Assert.Empty(_confirmation.Questions);
    }

    [Fact]
    public async Task ReloadCampaignCommand_Confirmed_DiscardsEditsAndLoadsTheChanges()
    {
        await _partySetup.LoadCommand.ExecuteAsync(null);
        _partySetup.NewPartyCommand.Execute(null);
        await _parties.SaveAsync(new Party(Guid.NewGuid(), "Pulled from git", []));
        _campaigns.RaiseFilesChanged();

        await _viewModel.ReloadCampaignCommand.ExecuteAsync(null);

        Assert.Equal("Pulled from git", Assert.Single(_partySetup.Parties).Name);
        Assert.False(_viewModel.HasPendingExternalChanges);
        Assert.Single(_unsavedChangesPrompt.Questions);
    }

    [Fact]
    public void Pages_HaveShortNamesForTheTopBar()
    {
        string[] expected = ["Initiative", "Stat Blocks", "Encounters", "Party", "Spellbook"];

        Assert.Equal(expected, _viewModel.Pages.Select(page => page.ShortTitle).ToArray());
    }
}
