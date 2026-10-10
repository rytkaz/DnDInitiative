using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.Tests.ViewModels;

public class SpellViewerViewModelTests
{
    private readonly InMemorySpellRepository _spells = new(
        new Spell { Key = "fireball-xphb", Name = "Fireball", Level = "3rd-level", School = "Evocation" },
        new Spell { Key = "shield", Name = "Shield", Level = "1st-level", School = "Abjuration" });

    private readonly SpellViewerViewModel _viewModel;

    public SpellViewerViewModelTests()
    {
        _viewModel = new SpellViewerViewModel(_spells);
    }

    [Fact]
    public async Task ShowCommand_KnownSpell_OpensThePopupOnIt()
    {
        await _viewModel.ShowCommand.ExecuteAsync("fireball-xphb");

        Assert.True(_viewModel.IsOpen);
        Assert.Equal("Fireball", _viewModel.Details?.Name);
        Assert.Equal("3rd-level Evocation", _viewModel.Details?.Summary);
    }

    [Fact]
    public async Task ShowCommand_UnknownSpell_OpensThePopupToSaySo()
    {
        await _viewModel.ShowCommand.ExecuteAsync("wish");

        Assert.True(_viewModel.IsOpen);
        Assert.Equal("wish", _viewModel.Details?.Name);
        Assert.Equal("Spell not found", _viewModel.Details?.Summary);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task ShowCommand_NoReference_DoesNothing(string? reference)
    {
        await _viewModel.ShowCommand.ExecuteAsync(reference);

        Assert.False(_viewModel.IsOpen);
        Assert.Null(_viewModel.Details);
    }

    [Fact]
    public async Task ShowCommand_WhileAlreadyOpen_ReplacesTheSpell()
    {
        await _viewModel.ShowCommand.ExecuteAsync("fireball-xphb");

        await _viewModel.ShowCommand.ExecuteAsync("shield");

        Assert.True(_viewModel.IsOpen);
        Assert.Equal("Shield", _viewModel.Details?.Name);
    }

    [Fact]
    public async Task ShowCommand_EarlierLookupFinishesLast_DoesNotReplaceTheNewerSpell()
    {
        var slow = new TaskCompletionSource();
        _spells.Gates["fireball-xphb"] = slow;

        Task first = _viewModel.ShowCommand.ExecuteAsync("fireball-xphb");
        await _viewModel.ShowCommand.ExecuteAsync("shield");
        slow.SetResult();
        await first;

        Assert.Equal("Shield", _viewModel.Details?.Name);
    }

    [Fact]
    public async Task CloseCommand_ClosesThePopup()
    {
        await _viewModel.ShowCommand.ExecuteAsync("shield");

        _viewModel.CloseCommand.Execute(null);

        Assert.False(_viewModel.IsOpen);
    }
}
