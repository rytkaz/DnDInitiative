using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.ViewModels;

public class InitiativeTrackerViewModelTests
{
    [Fact]
    public void AddCreatureCommand_WithQuantity_AddsNumberedCombatantsWithRolledInitiative()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModel(dieResult: 10);
        viewModel.NewName = "Goblin";
        viewModel.NewQuantity = 3;
        viewModel.NewInitiativeModifier = 2;
        viewModel.NewMaxHp = 7;

        viewModel.AddCreatureCommand.Execute(null);

        Assert.Equal(3, viewModel.Combatants.Count);
        Assert.Equal("Goblin 1", viewModel.Combatants[0].Name);
        Assert.Equal(12, viewModel.Combatants[0].Initiative);
        Assert.Equal(7, viewModel.Combatants[0].CurrentHp);
        Assert.True(viewModel.Combatants[0].IsCurrent);
        Assert.False(viewModel.Combatants[1].IsCurrent);
    }

    [Fact]
    public void AddCreatureCommand_BlankName_CannotExecute()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.AddCreatureCommand.CanExecute(null));

        viewModel.NewName = "Goblin";

        Assert.True(viewModel.AddCreatureCommand.CanExecute(null));
    }

    [Fact]
    public void NextCommand_PastLastCombatant_ReturnsToFirstAndAdvancesRound()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelWithGoblins(2);

        viewModel.NextCommand.Execute(null);
        viewModel.NextCommand.Execute(null);

        Assert.Equal(2, viewModel.Round);
        Assert.True(viewModel.Combatants[0].IsCurrent);
        Assert.False(viewModel.Combatants[1].IsCurrent);
    }

    [Fact]
    public void RemoveSelectedCommand_RemovesThatCombatant()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelWithGoblins(2);
        viewModel.SelectedCombatant = viewModel.Combatants[0];

        viewModel.RemoveSelectedCommand.Execute(null);

        CombatantViewModel remaining = Assert.Single(viewModel.Combatants);
        Assert.Equal("Goblin 2", remaining.Name);
        Assert.True(remaining.IsCurrent);
    }

    [Fact]
    public void NewCombatCommand_ClearsCombatantsAndRestartsNumbering()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelWithGoblins(2);
        viewModel.NextCommand.Execute(null);
        viewModel.NextCommand.Execute(null);

        viewModel.NewCombatCommand.Execute(null);
        viewModel.NewQuantity = 1;
        viewModel.AddCreatureCommand.Execute(null);

        Assert.Equal(1, viewModel.Round);
        Assert.Equal("Goblin", Assert.Single(viewModel.Combatants).Name);
    }

    [Fact]
    public void EditingInitiative_MovesRowToMatchingPlace()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelWithGoblins(2);

        viewModel.Combatants[1].Initiative = 20;

        Assert.Equal("Goblin 2", viewModel.Combatants[0].Name);
        Assert.Equal("Goblin 1", viewModel.Combatants[1].Name);
        Assert.True(viewModel.Combatants[1].IsCurrent);
    }

    [Fact]
    public void DamageCommand_ReducesHpAndClearsTheAmount()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModel();
        viewModel.NewName = "Goblin";
        viewModel.NewMaxHp = 7;
        viewModel.AddCreatureCommand.Execute(null);
        CombatantViewModel goblin = Assert.Single(viewModel.Combatants);
        goblin.HpChangeAmount = 3;

        goblin.DamageCommand.Execute(null);

        Assert.Equal("4 / 7", goblin.HpText);
        Assert.Null(goblin.HpChangeAmount);
    }

    [Fact]
    public void DamageCommand_WithoutAmount_CannotExecute()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModel();
        viewModel.NewName = "Goblin";
        viewModel.NewMaxHp = 7;
        viewModel.AddCreatureCommand.Execute(null);

        Assert.False(Assert.Single(viewModel.Combatants).DamageCommand.CanExecute(null));
    }

    [Fact]
    public async Task NewCombatCommand_WithSelectedParty_AddsItsMembers()
    {
        var party = new Party(
            Guid.NewGuid(),
            "Heroes",
            [new PlayerCharacter("Aria", Level: 3, MaxHp: 24), new PlayerCharacter("Bren", Level: 3)]);
        InitiativeTrackerViewModel viewModel = CreateViewModel(10, party);
        await viewModel.LoadPartiesCommand.ExecuteAsync(null);
        viewModel.SelectedParty = Assert.Single(viewModel.Parties);

        viewModel.NewCombatCommand.Execute(null);

        Assert.Equal(2, viewModel.Combatants.Count);
        Assert.Contains(viewModel.Combatants, row => row.Name == "Aria");
        Assert.Contains(viewModel.Combatants, row => row.Name == "Bren");
    }

    [Fact]
    public void MoveCombatant_TakesTargetPositionAndKeepsCurrentCombatant()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelWithGoblins(3);
        CombatantViewModel first = viewModel.Combatants[0];
        CombatantViewModel last = viewModel.Combatants[2];

        viewModel.MoveCombatant(first, last);

        Assert.Equal("Goblin 2", viewModel.Combatants[0].Name);
        Assert.Equal("Goblin 3", viewModel.Combatants[1].Name);
        Assert.Equal("Goblin 1", viewModel.Combatants[2].Name);
        Assert.True(viewModel.Combatants[2].IsCurrent);
    }

    [Fact]
    public void StartCombatMessage_StartsANewCombatWithThePartyAndCreatures()
    {
        var messenger = new WeakReferenceMessenger();
        InitiativeTrackerViewModel viewModel = CreateViewModel(messenger);
        viewModel.NewName = "Leftover";
        viewModel.AddCreatureCommand.Execute(null);
        var party = new Party(Guid.NewGuid(), "Heroes", [new PlayerCharacter("Aria", Level: 3)]);

        messenger.Send(new StartCombatMessage(
            "Ambush",
            [new CombatantBlueprint("Goblin", MaxHp: 7), new CombatantBlueprint("Goblin", MaxHp: 7)],
            party));

        Assert.Equal("Ambush", viewModel.CombatName);
        Assert.Same(party, viewModel.SelectedParty);
        Assert.Equal(3, viewModel.Combatants.Count);
        Assert.Contains(viewModel.Combatants, row => row.Name == "Aria");
        Assert.Contains(viewModel.Combatants, row => row.Name == "Goblin 1");
        Assert.Contains(viewModel.Combatants, row => row.Name == "Goblin 2");
        Assert.DoesNotContain(viewModel.Combatants, row => row.Name == "Leftover");
    }

    [Fact]
    public void SelectingACombatant_WithAStatBlock_ShowsItInThePanel()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelInCombatWithGoblinAndPlayer();

        viewModel.SelectedCombatant = viewModel.Combatants.Single(row => row.Name == "Goblin");

        Assert.Equal("Goblin", viewModel.StatBlockDetails?.Name);
    }

    [Fact]
    public void SelectingACombatant_WithoutAStatBlock_EmptiesThePanel()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelInCombatWithGoblinAndPlayer();
        viewModel.SelectedCombatant = viewModel.Combatants.Single(row => row.Name == "Goblin");

        viewModel.SelectedCombatant = viewModel.Combatants.Single(row => row.Name == "Aria");

        Assert.Null(viewModel.StatBlockDetails);
    }

    [Fact]
    public async Task AddFromLibraryCommand_AddsTheSelectedStatBlockWithItsNumbers()
    {
        var goblin = new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Goblin",
            HitPoints = 10,
            ArmorClass = 15,
            InitiativeModifier = 2,
        };
        InitiativeTrackerViewModel viewModel = CreateViewModelWithLibrary(goblin);
        await viewModel.LoadLibraryCommand.ExecuteAsync(null);
        viewModel.SelectedLibraryStatBlock = Assert.Single(viewModel.LibraryResults);
        viewModel.NewQuantity = 2;

        await viewModel.AddFromLibraryCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Combatants.Count);
        CombatantViewModel first = viewModel.Combatants[0];
        Assert.Equal("Goblin 1", first.Name);
        Assert.Equal(12, first.Initiative);
        Assert.Equal("10 / 10", first.HpText);
        Assert.Equal(15, first.ArmorClass);
        Assert.Equal(goblin.Id, first.Combatant.StatBlockId);
    }

    [Fact]
    public async Task AddFromLibraryCommand_NothingSelected_CannotExecute()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelWithLibrary(new StatBlock { Id = Guid.NewGuid(), Name = "Goblin" });
        await viewModel.LoadLibraryCommand.ExecuteAsync(null);

        Assert.False(viewModel.AddFromLibraryCommand.CanExecute(null));
    }

    [Fact]
    public async Task LibrarySearchText_FiltersTheLibraryList()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelWithLibrary(
            new StatBlock { Id = Guid.NewGuid(), Name = "Goblin" },
            new StatBlock { Id = Guid.NewGuid(), Name = "Ogre" });
        await viewModel.LoadLibraryCommand.ExecuteAsync(null);
        Assert.Equal(2, viewModel.LibraryResults.Count);

        viewModel.LibrarySearchText = "og";

        Assert.Equal("Ogre", Assert.Single(viewModel.LibraryResults).Name);
    }

    [Fact]
    public async Task SelectingALibraryEntry_ShowsItsStatBlockInThePanel()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelWithLibrary(new StatBlock { Id = Guid.NewGuid(), Name = "Goblin" });
        await viewModel.LoadLibraryCommand.ExecuteAsync(null);

        viewModel.SelectedLibraryStatBlock = Assert.Single(viewModel.LibraryResults);

        Assert.Equal("Goblin", viewModel.StatBlockDetails?.Name);
    }

    [Fact]
    public void NewCombatCommand_EmptyTracker_DoesNotAsk()
    {
        var confirmation = new FakeConfirmation();
        InitiativeTrackerViewModel viewModel = CreateViewModel(confirmation);

        viewModel.NewCombatCommand.Execute(null);

        Assert.Empty(confirmation.Questions);
    }

    [Fact]
    public void NewCombatCommand_Declined_KeepsTheCombatInProgress()
    {
        var confirmation = new FakeConfirmation { Answer = false };
        InitiativeTrackerViewModel viewModel = CreateViewModel(confirmation);
        viewModel.NewName = "Goblin";
        viewModel.AddCreatureCommand.Execute(null);
        viewModel.NextCommand.Execute(null);

        viewModel.NewCombatCommand.Execute(null);

        Assert.Single(confirmation.Questions);
        Assert.Equal("Goblin", Assert.Single(viewModel.Combatants).Name);
        Assert.Equal(2, viewModel.Round);
    }

    [Fact]
    public void StartCombatMessage_Declined_KeepsTheCombatAndTheSelectedParty()
    {
        var messenger = new WeakReferenceMessenger();
        var confirmation = new FakeConfirmation { Answer = false };
        InitiativeTrackerViewModel viewModel = CreateViewModel(confirmation, messenger);
        viewModel.NewName = "Leftover";
        viewModel.AddCreatureCommand.Execute(null);
        var party = new Party(Guid.NewGuid(), "Heroes", [new PlayerCharacter("Aria", Level: 3)]);

        messenger.Send(new StartCombatMessage("Ambush", [new CombatantBlueprint("Goblin")], party));

        Assert.Equal("", viewModel.CombatName);
        Assert.Null(viewModel.SelectedParty);
        Assert.Equal("Leftover", Assert.Single(viewModel.Combatants).Name);
    }

    [Fact]
    public async Task ClearPartyCommand_UnselectsTheParty_SoTheNextCombatStartsEmpty()
    {
        var party = new Party(Guid.NewGuid(), "Heroes", [new PlayerCharacter("Aria", Level: 3)]);
        InitiativeTrackerViewModel viewModel = CreateViewModel(10, party);
        await viewModel.LoadPartiesCommand.ExecuteAsync(null);
        Assert.False(viewModel.ClearPartyCommand.CanExecute(null));

        viewModel.SelectedParty = Assert.Single(viewModel.Parties);
        Assert.True(viewModel.ClearPartyCommand.CanExecute(null));

        viewModel.ClearPartyCommand.Execute(null);
        viewModel.NewCombatCommand.Execute(null);

        Assert.Null(viewModel.SelectedParty);
        Assert.Empty(viewModel.Combatants);
    }

    [Fact]
    public void SetHpCommand_SetsCurrentHpAndStaysWithinTheMaximum()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModel();
        viewModel.NewName = "Goblin";
        viewModel.NewMaxHp = 7;
        viewModel.AddCreatureCommand.Execute(null);
        CombatantViewModel goblin = viewModel.Combatants[0];

        goblin.HpChangeAmount = 3;
        goblin.SetHpCommand.Execute(null);
        Assert.Equal(3, goblin.CurrentHp);
        Assert.Null(goblin.HpChangeAmount);

        goblin.HpChangeAmount = 50;
        goblin.SetHpCommand.Execute(null);
        Assert.Equal(7, goblin.CurrentHp);
    }

    [Fact]
    public void SetHpCommand_AcceptsZero_UnlikeDamage()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModel();
        viewModel.NewName = "Goblin";
        viewModel.NewMaxHp = 7;
        viewModel.AddCreatureCommand.Execute(null);
        CombatantViewModel goblin = viewModel.Combatants[0];

        goblin.HpChangeAmount = 0;

        Assert.True(goblin.SetHpCommand.CanExecute(null));
        Assert.False(goblin.DamageCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(500, 500)]
    [InlineData(10, 260)]
    [InlineData(5000, 1200)]
    public void StatBlockPanelWidth_IsKeptWithinLimits(double requested, double expected)
    {
        InitiativeTrackerViewModel viewModel = CreateViewModel();

        viewModel.StatBlockPanelWidth = requested;

        Assert.Equal(expected, viewModel.StatBlockPanelWidth);
    }

    [Fact]
    public void Constructor_UsesTheSavedPanelWidth()
    {
        var uiState = new InMemoryUiStateStore { State = new UiState { StatBlockPanelWidth = 640 } };

        InitiativeTrackerViewModel viewModel = CreateViewModel(uiState);

        Assert.Equal(640, viewModel.StatBlockPanelWidth);
    }

    [Fact]
    public void Constructor_SavedPanelWidthOutsideTheLimits_IsClamped()
    {
        var uiState = new InMemoryUiStateStore { State = new UiState { StatBlockPanelWidth = 99999 } };

        InitiativeTrackerViewModel viewModel = CreateViewModel(uiState);

        Assert.Equal(1200, viewModel.StatBlockPanelWidth);
    }

    [Fact]
    public void Constructor_NothingSaved_ShowsTheStatBlockPanel()
    {
        Assert.True(CreateViewModel(new InMemoryUiStateStore()).ShowStatBlockPanel);
    }

    [Fact]
    public void Constructor_PanelWasHidden_KeepsItHiddenWithoutSavingAgain()
    {
        var uiState = new InMemoryUiStateStore { State = new UiState { ShowStatBlockPanel = false } };

        InitiativeTrackerViewModel viewModel = CreateViewModel(uiState);

        Assert.False(viewModel.ShowStatBlockPanel);
        Assert.Equal(0, uiState.SaveCount);
    }

    [Fact]
    public void ShowStatBlockPanel_Changed_IsSavedAndKeepsTheRestOfTheState()
    {
        var uiState = new InMemoryUiStateStore { State = new UiState { IsNavigationCollapsed = true, StatBlockPanelWidth = 500 } };
        InitiativeTrackerViewModel viewModel = CreateViewModel(uiState);

        viewModel.ShowStatBlockPanel = false;

        Assert.Equal(new UiState { IsNavigationCollapsed = true, StatBlockPanelWidth = 500, ShowStatBlockPanel = false }, uiState.State);
    }

    [Fact]
    public void SaveLayout_SavesThePanelWidthAndKeepsTheRestOfTheState()
    {
        var uiState = new InMemoryUiStateStore
        {
            State = new UiState { IsNavigationCollapsed = true, SourcesPanelHeight = 300 },
        };
        InitiativeTrackerViewModel viewModel = CreateViewModel(uiState);

        viewModel.StatBlockPanelWidth = 500;
        Assert.Equal(0, uiState.SaveCount);

        viewModel.SaveLayout();

        Assert.Equal(new UiState { IsNavigationCollapsed = true, SourcesPanelHeight = 300, StatBlockPanelWidth = 500 }, uiState.State);
    }

    [Fact]
    public void RerollInitiativeCommand_RerollsCreaturesButNotPlayers()
    {
        // Rolls in order: Aria 10, Goblin 10 (+2), then the reroll for the goblin: 3 (+2).
        var messenger = new WeakReferenceMessenger();
        var viewModel = new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new SequenceDiceRoller(10, 10, 3))),
            new InMemoryPartyRepository(),
            new InMemoryStatBlockRepository(),
            messenger,
            new FakeConfirmation(),
            new InMemoryUiStateStore());
        var party = new Party(Guid.NewGuid(), "Heroes", [new PlayerCharacter("Aria", Level: 3)]);
        messenger.Send(new StartCombatMessage("Ambush", [new CombatantBlueprint("Goblin", InitiativeModifier: 2)], party));
        Assert.Equal("Goblin", viewModel.Combatants[0].Name);

        viewModel.RerollInitiativeCommand.Execute(null);

        Assert.Equal("Aria", viewModel.Combatants[0].Name);
        Assert.Equal(10, viewModel.Combatants[0].Initiative);
        Assert.Equal("Goblin", viewModel.Combatants[1].Name);
        Assert.Equal(5, viewModel.Combatants[1].Initiative);
    }

    private static InitiativeTrackerViewModel CreateViewModelWithLibrary(params StatBlock[] statBlocks)
    {
        return new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
            new InMemoryPartyRepository(),
            new InMemoryStatBlockRepository(statBlocks),
            new WeakReferenceMessenger(),
            new FakeConfirmation(),
            new InMemoryUiStateStore());
    }

    private static InitiativeTrackerViewModel CreateViewModel(InMemoryUiStateStore uiState)
    {
        return new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
            new InMemoryPartyRepository(),
            new InMemoryStatBlockRepository(),
            new WeakReferenceMessenger(),
            new FakeConfirmation(),
            uiState);
    }

    private static InitiativeTrackerViewModel CreateViewModelInCombatWithGoblinAndPlayer()
    {
        var goblin = new StatBlock { Id = Guid.NewGuid(), Name = "Goblin", HitPoints = 10 };
        var messenger = new WeakReferenceMessenger();
        var viewModel = new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
            new InMemoryPartyRepository(),
            new InMemoryStatBlockRepository(goblin),
            messenger,
            new FakeConfirmation(),
            new InMemoryUiStateStore());

        messenger.Send(new StartCombatMessage("Ambush", [goblin.ToBlueprint(), new CombatantBlueprint("Aria")], Party: null));
        return viewModel;
    }

    private static InitiativeTrackerViewModel CreateViewModel(int dieResult = 10, params Party[] parties)
    {
        return CreateViewModel(new WeakReferenceMessenger(), dieResult, parties);
    }

    private static InitiativeTrackerViewModel CreateViewModel(IMessenger messenger, int dieResult = 10, params Party[] parties)
    {
        return new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(dieResult))),
            new InMemoryPartyRepository(parties),
            new InMemoryStatBlockRepository(),
            messenger,
            new FakeConfirmation(),
            new InMemoryUiStateStore());
    }

    private static InitiativeTrackerViewModel CreateViewModel(FakeConfirmation confirmation, IMessenger? messenger = null)
    {
        return new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
            new InMemoryPartyRepository(),
            new InMemoryStatBlockRepository(),
            messenger ?? new WeakReferenceMessenger(),
            confirmation,
            new InMemoryUiStateStore());
    }

    private static InitiativeTrackerViewModel CreateViewModelWithGoblins(int quantity)
    {
        InitiativeTrackerViewModel viewModel = CreateViewModel();
        viewModel.NewName = "Goblin";
        viewModel.NewQuantity = quantity;
        viewModel.AddCreatureCommand.Execute(null);
        return viewModel;
    }
}