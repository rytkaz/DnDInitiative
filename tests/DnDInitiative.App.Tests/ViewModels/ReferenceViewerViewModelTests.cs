using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Conditions;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.Tests.ViewModels;

public class ReferenceViewerViewModelTests
{
    private readonly InMemorySpellRepository _spells = new(
        new Spell { Key = "fireball-xphb", Name = "Fireball", Level = "3rd-level", School = "Evocation" },
        new Spell { Key = "shield", Name = "Shield", Level = "1st-level", School = "Abjuration" });

    private readonly InMemoryConditionRepository _conditions = new(
        new Condition { Name = "Prone", Source = "XPHB p. 367", Description = "You can only crawl." });

    private readonly ReferenceViewerViewModel _viewModel;

    public ReferenceViewerViewModelTests()
    {
        _viewModel = new ReferenceViewerViewModel(_spells, _conditions);
    }

    [Fact]
    public async Task ShowSpellCommand_KnownSpell_OpensThePopupOnIt()
    {
        await _viewModel.ShowSpellCommand.ExecuteAsync("fireball-xphb");

        Assert.True(_viewModel.IsOpen);
        Assert.Equal("Fireball", _viewModel.Details?.Name);
        Assert.Equal("3rd-level Evocation", _viewModel.Details?.Summary);
    }

    [Fact]
    public async Task ShowSpellCommand_UnknownSpell_OpensThePopupToSaySo()
    {
        await _viewModel.ShowSpellCommand.ExecuteAsync("wish");

        Assert.True(_viewModel.IsOpen);
        Assert.Equal("wish", _viewModel.Details?.Name);
        Assert.Equal("Spell not found", _viewModel.Details?.Summary);
    }

    [Fact]
    public async Task ShowConditionCommand_KnownCondition_OpensThePopupOnIt()
    {
        await _viewModel.ShowConditionCommand.ExecuteAsync("prone");

        Assert.True(_viewModel.IsOpen);
        ConditionDetailsViewModel card = Assert.IsType<ConditionDetailsViewModel>(_viewModel.Details);
        Assert.Equal("Prone", card.Name);
        Assert.Equal("Condition", card.Summary);
    }

    [Fact]
    public async Task ShowConditionCommand_UnknownCondition_OpensThePopupToSaySo()
    {
        await _viewModel.ShowConditionCommand.ExecuteAsync("Petrified");

        Assert.True(_viewModel.IsOpen);
        Assert.Equal("Petrified", _viewModel.Details?.Name);
        Assert.Equal("Condition not found", _viewModel.Details?.Summary);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task ShowCommands_NoReference_DoNothing(string? reference)
    {
        await _viewModel.ShowSpellCommand.ExecuteAsync(reference);
        await _viewModel.ShowConditionCommand.ExecuteAsync(reference);

        Assert.False(_viewModel.IsOpen);
        Assert.Null(_viewModel.Details);
    }

    [Fact]
    public async Task ShowConditionCommand_WhileASpellIsOpen_ReplacesIt()
    {
        await _viewModel.ShowSpellCommand.ExecuteAsync("shield");

        await _viewModel.ShowConditionCommand.ExecuteAsync("Prone");

        Assert.True(_viewModel.IsOpen);
        Assert.Equal("Prone", _viewModel.Details?.Name);
    }

    [Fact]
    public async Task ShowCommands_EarlierLookupFinishesLast_DoesNotReplaceTheNewerCard()
    {
        var slow = new TaskCompletionSource();
        _spells.Gates["fireball-xphb"] = slow;

        Task first = _viewModel.ShowSpellCommand.ExecuteAsync("fireball-xphb");
        await _viewModel.ShowConditionCommand.ExecuteAsync("Prone");
        slow.SetResult();
        await first;

        Assert.Equal("Prone", _viewModel.Details?.Name);
    }

    [Fact]
    public async Task CloseCommand_ClosesThePopup()
    {
        await _viewModel.ShowConditionCommand.ExecuteAsync("Prone");

        _viewModel.CloseCommand.Execute(null);

        Assert.False(_viewModel.IsOpen);
    }
}
