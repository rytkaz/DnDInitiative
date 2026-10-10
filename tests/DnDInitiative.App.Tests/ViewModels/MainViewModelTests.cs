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
    private readonly InitiativeTrackerViewModel _tracker;
    private readonly EncounterBuilderViewModel _encounterBuilder;
    private readonly PartySetupViewModel _partySetup;
    private readonly MainViewModel _viewModel;
    private readonly InMemoryPartyRepository _parties = new();
    private readonly FakeCampaignService _campaigns = new();
    private readonly FakeFolderPicker _folderPicker = new();
    private readonly InMemoryUiStateStore _uiState = new();

    public MainViewModelTests()
    {
        var statBlocks = new InMemoryStatBlockRepository();

        _tracker = new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
            _parties,
            statBlocks,
            _messenger,
            _confirmation,
            new InMemoryUiStateStore());
        _encounterBuilder = new EncounterBuilderViewModel(
            new InMemoryEncounterRepository(),
            statBlocks,
            _parties,
            _messenger,
            new FakeClipboard(),
            _confirmation);
        _partySetup = new PartySetupViewModel(_parties, _confirmation);
        var import = new ImportViewModel(
            new InMemoryConversionSettingsStore(),
            new FakeStatBlockConverter(),
            new FakeStatBlockImporter(),
            new FakeFolderPicker(),
            new FakeFilePicker(),
            _messenger,
            new FakeConverterInstaller(),
            new FakeSourceDataService(),
            new FakeConverterConfigWriter(),
            new InMemoryUiStateStore(),
            new FakeSpellImporter());
        _viewModel = new MainViewModel(
            _tracker,
            new StatBlocksViewModel(statBlocks, _confirmation, _messenger),
            _encounterBuilder,
            _partySetup,
            import,
            _messenger,
            _confirmation,
            _campaigns,
            _folderPicker,
            _uiState);
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
        Assert.True(_viewModel.ConfirmClose());
        Assert.Empty(_confirmation.Questions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConfirmClose_UnsavedParty_NamesItAndFollowsTheAnswer(bool answer)
    {
        _partySetup.NewPartyCommand.Execute(null);
        _confirmation.Answer = answer;

        bool canClose = _viewModel.ConfirmClose();

        Assert.Equal(answer, canClose);
        Assert.Contains("New party", Assert.Single(_confirmation.Questions), StringComparison.Ordinal);
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
        _confirmation.Answer = false;

        await _viewModel.OpenCampaignCommand.ExecuteAsync(null);

        Assert.Equal("First", _viewModel.CampaignName);
        Assert.Contains("New party", Assert.Single(_confirmation.Questions), StringComparison.Ordinal);
        Assert.Single(_partySetup.Parties);
    }

    [Fact]
    public async Task OpenCampaignCommand_CombatInProgress_AsksAndThenClearsIt()
    {
        _tracker.NewName = "Goblin";
        _tracker.AddCreatureCommand.Execute(null);
        _folderPicker.FolderToReturn = @"C:\campaigns\Strahd";

        await _viewModel.OpenCampaignCommand.ExecuteAsync(null);

        Assert.Contains("combat", Assert.Single(_confirmation.Questions), StringComparison.Ordinal);
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
        Assert.Single(_confirmation.Questions);
    }

    [Fact]
    public void ToggleNavigationCommand_FoldsThePanelAwayAndBringsItBack()
    {
        Assert.False(_viewModel.IsNavigationCollapsed);

        _viewModel.ToggleNavigationCommand.Execute(null);
        Assert.True(_viewModel.IsNavigationCollapsed);

        _viewModel.ToggleNavigationCommand.Execute(null);
        Assert.False(_viewModel.IsNavigationCollapsed);
    }

    [Fact]
    public void FilesChangedExternally_PanelFoldedAwayAndEditsUnsaved_ShowsThePanelSoReloadCanBeSeen()
    {
        _partySetup.NewPartyCommand.Execute(null);
        _viewModel.ToggleNavigationCommand.Execute(null);

        _campaigns.RaiseFilesChanged();

        Assert.True(_viewModel.HasPendingExternalChanges);
        Assert.False(_viewModel.IsNavigationCollapsed);
    }

    [Fact]
    public void ToggleNavigationCommand_SavesTheNewStateEachTime()
    {
        _viewModel.ToggleNavigationCommand.Execute(null);
        Assert.True(_uiState.State.IsNavigationCollapsed);

        _viewModel.ToggleNavigationCommand.Execute(null);
        Assert.False(_uiState.State.IsNavigationCollapsed);
        Assert.Equal(2, _uiState.SaveCount);
    }

    [Fact]
    public void ToggleNavigationCommand_KeepsThePanelSizesTheOtherPagesSaved()
    {
        _uiState.State = new UiState { StatBlockPanelWidth = 500, SourcesPanelHeight = 300 };

        _viewModel.ToggleNavigationCommand.Execute(null);

        Assert.Equal(new UiState { IsNavigationCollapsed = true, StatBlockPanelWidth = 500, SourcesPanelHeight = 300 }, _uiState.State);
    }

    [Fact]
    public void Constructor_PanelWasFoldedAwayLastTime_StartsFoldedAwayWithoutSavingAgain()
    {
        var uiState = new InMemoryUiStateStore { State = new UiState { IsNavigationCollapsed = true } };
        var parties = new InMemoryPartyRepository();
        var statBlocks = new InMemoryStatBlockRepository();
        var messenger = new WeakReferenceMessenger();

        var viewModel = new MainViewModel(
            new InitiativeTrackerViewModel(
                new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
                parties,
                statBlocks,
                messenger,
                _confirmation,
                new InMemoryUiStateStore()),
            new StatBlocksViewModel(statBlocks, _confirmation, messenger),
            _encounterBuilder,
            _partySetup,
            (ImportViewModel)_viewModel.Pages[4],
            messenger,
            _confirmation,
            _campaigns,
            _folderPicker,
            uiState);

        Assert.True(viewModel.IsNavigationCollapsed);
        Assert.Equal(0, uiState.SaveCount);
    }

    [Fact]
    public void Pages_HaveShortNamesForTheFoldedAwayPanel()
    {
        string[] expected = ["Initiative", "StatBlocks", "Encounters", "Party", "Import"];

        Assert.Equal(expected, _viewModel.Pages.Select(page => page.ShortTitle).ToArray());
    }
}
