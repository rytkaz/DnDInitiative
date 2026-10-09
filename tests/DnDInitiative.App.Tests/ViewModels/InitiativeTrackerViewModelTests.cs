using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
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
        Assert.True(viewModel.IsStatBlockPanelVisible);
    }

    [Fact]
    public void SelectingACombatant_WithoutAStatBlock_HidesThePanel()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelInCombatWithGoblinAndPlayer();
        viewModel.SelectedCombatant = viewModel.Combatants.Single(row => row.Name == "Goblin");

        viewModel.SelectedCombatant = viewModel.Combatants.Single(row => row.Name == "Aria");

        Assert.Null(viewModel.StatBlockDetails);
        Assert.False(viewModel.IsStatBlockPanelVisible);
    }

    [Fact]
    public void ShowStatBlockPanel_TurnedOff_HidesThePanelEvenWithAStatBlock()
    {
        InitiativeTrackerViewModel viewModel = CreateViewModelInCombatWithGoblinAndPlayer();
        viewModel.SelectedCombatant = viewModel.Combatants.Single(row => row.Name == "Goblin");

        viewModel.ShowStatBlockPanel = false;

        Assert.False(viewModel.IsStatBlockPanelVisible);
    }

    private static InitiativeTrackerViewModel CreateViewModelInCombatWithGoblinAndPlayer()
    {
        var goblin = new StatBlock { Id = Guid.NewGuid(), Name = "Goblin", HitPoints = 10 };
        var messenger = new WeakReferenceMessenger();
        var viewModel = new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(10))),
            new InMemoryPartyRepository(),
            new InMemoryStatBlockRepository(goblin),
            messenger);

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
            messenger);
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