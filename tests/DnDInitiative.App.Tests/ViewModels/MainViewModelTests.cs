using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;

namespace DnDInitiative.App.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void StartCombatMessage_NavigatesToTheTracker()
    {
        var messenger = new WeakReferenceMessenger();
        var parties = new InMemoryPartyRepository();
        var statBlocks = new InMemoryStatBlockRepository();
        var tracker = new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
            parties,
            statBlocks,
            messenger);
        var encounterBuilder = new EncounterBuilderViewModel(
            new InMemoryEncounterRepository(),
            statBlocks, 
            parties, 
            messenger, 
            new FakeClipboard());
        var viewModel = new MainViewModel(
            tracker,
            new StatBlocksViewModel(statBlocks, new FakeStatBlockImporter(), new FakeFolderPicker()),
            encounterBuilder,
            new PartySetupViewModel(parties),
            messenger);
        viewModel.CurrentPage = encounterBuilder;

        messenger.Send(new StartCombatMessage("Ambush", [], Party: null));

        Assert.Same(tracker, viewModel.CurrentPage);
    }
}