using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;

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

    private static InitiativeTrackerViewModel CreateViewModel(int dieResult = 10)
    {
        return new InitiativeTrackerViewModel(
            new CombatantFactory(new InitiativeRoller(new FixedDiceRoller(dieResult))));
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