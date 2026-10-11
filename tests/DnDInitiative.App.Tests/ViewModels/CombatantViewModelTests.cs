using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Combat;

namespace DnDInitiative.App.Tests.ViewModels;

public class CombatantViewModelTests
{
    private readonly Combatant _combatant = new("Goblin 2", initiative: 12);
    private readonly CombatantViewModel _viewModel;
    private readonly List<string?> _changed = [];

    public CombatantViewModelTests()
    {
        _viewModel = new CombatantViewModel(_combatant, (_, _) => { });
        _viewModel.PropertyChanged += (_, e) => _changed.Add(e.PropertyName);
    }

    [Fact]
    public void Name_Set_RenamesTheCombatant()
    {
        _viewModel.Name = "  Goblin (burning) ";

        Assert.Equal("Goblin (burning)", _combatant.Name);
        Assert.Equal("Goblin (burning)", _viewModel.Name);
        Assert.Contains(nameof(CombatantViewModel.Name), _changed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_SetToNothing_KeepsTheOldNameAndTellsTheTextBoxToShowItAgain(string name)
    {
        _viewModel.Name = name;

        Assert.Equal("Goblin 2", _combatant.Name);
        Assert.Contains(nameof(CombatantViewModel.Name), _changed);
    }
}
