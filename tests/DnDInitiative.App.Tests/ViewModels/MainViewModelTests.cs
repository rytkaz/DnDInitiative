using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;

namespace DnDInitiative.App.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly FakeConfirmation _confirmation = new();
    private readonly InitiativeTrackerViewModel _tracker;
    private readonly EncounterBuilderViewModel _encounterBuilder;
    private readonly PartySetupViewModel _partySetup;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        var parties = new InMemoryPartyRepository();
        var statBlocks = new InMemoryStatBlockRepository();

        _tracker = new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
            parties,
            statBlocks,
            _messenger,
            _confirmation);
        _encounterBuilder = new EncounterBuilderViewModel(
            new InMemoryEncounterRepository(),
            statBlocks,
            parties,
            _messenger,
            new FakeClipboard(),
            _confirmation);
        _partySetup = new PartySetupViewModel(parties, _confirmation);
        _viewModel = new MainViewModel(
            _tracker,
            new StatBlocksViewModel(statBlocks, new FakeStatBlockImporter(), new FakeFolderPicker(), _confirmation),
            _encounterBuilder,
            _partySetup,
            _messenger,
            _confirmation);
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
}