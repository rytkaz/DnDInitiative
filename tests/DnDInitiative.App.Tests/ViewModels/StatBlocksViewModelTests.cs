using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.ViewModels;

public class StatBlocksViewModelTests
{
    [Fact]
    public async Task LoadCommand_ListsStatBlocksByNameAndShowsTheFirst()
    {
        var viewModel = new StatBlocksViewModel(new InMemoryStatBlockRepository(Create("Orc"), Create("Goblin")));

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.FilteredStatBlocks.Count);
        Assert.Equal("Goblin", viewModel.FilteredStatBlocks[0].Name);
        Assert.Equal("Goblin", viewModel.SelectedStatBlock?.Name);
        Assert.Equal("Goblin", viewModel.Details?.Name);
    }

    [Fact]
    public async Task SearchText_FiltersByNameIgnoringCase()
    {
        var viewModel = new StatBlocksViewModel(new InMemoryStatBlockRepository(Create("Orc"), Create("Goblin")));
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SearchText = "OR";

        Assert.Equal("Orc", Assert.Single(viewModel.FilteredStatBlocks).Name);
    }

    [Fact]
    public async Task SaveCommand_NewStatBlock_StoresItAndLeavesEditMode()
    {
        var repository = new InMemoryStatBlockRepository();
        var viewModel = new StatBlocksViewModel(repository);
        viewModel.NewCommand.Execute(null);
        Assert.IsType<StatBlockEditorViewModel>(viewModel.Editor).Name = "Owlbear";

        await viewModel.SaveCommand.ExecuteAsync(null);

        StatBlock saved = Assert.Single(repository.StatBlocks);
        Assert.Equal("Owlbear", saved.Name);
        Assert.False(viewModel.IsEditing);
        Assert.Equal(saved.Id, viewModel.SelectedStatBlock?.Id);
    }

    [Fact]
    public async Task SaveCommand_BlankName_DoesNotStoreAndStaysInEditMode()
    {
        var repository = new InMemoryStatBlockRepository();
        var viewModel = new StatBlocksViewModel(repository);
        viewModel.NewCommand.Execute(null);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Empty(repository.StatBlocks);
        Assert.True(viewModel.IsEditing);
    }

    [Fact]
    public async Task CopyCommand_StoresACopyWithItsOwnIdAndSelectsIt()
    {
        StatBlock goblin = Create("Goblin");
        var repository = new InMemoryStatBlockRepository(goblin);
        var viewModel = new StatBlocksViewModel(repository);
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.CopyCommand.ExecuteAsync(null);

        Assert.Equal(2, repository.StatBlocks.Count);
        Assert.Equal("Goblin (Copy)", viewModel.SelectedStatBlock?.Name);
        Assert.NotEqual(goblin.Id, viewModel.SelectedStatBlock?.Id);
    }

    [Fact]
    public void Details_FormatAbilitiesAndLeaveOutEmptySections()
    {
        StatBlock goblin = Create("Goblin") with
        {
            Size = "Small",
            CreatureType = "Fey (Goblinoid)",
            Alignment = "Chaotic Neutral",
            Abilities = new AbilityScores(8, 15, 10, 10, 8, 8),
            Actions = [new StatBlockFeature("Scimitar", "Melee attack.")],
        };

        var details = new StatBlockDetailsViewModel(goblin);

        Assert.Equal("Small Fey (Goblinoid), Chaotic Neutral", details.Summary);
        Assert.Contains(new LabeledValue("STR", "8 (-1)"), details.Abilities);
        Assert.Contains(new LabeledValue("DEX", "15 (+2)"), details.Abilities);
        Assert.Equal("Actions", Assert.Single(details.Sections).Title);
    }

    [Fact]
    public void Editor_ToStatBlock_KeepsIdTrimsTextAndClampsNumbers()
    {
        StatBlock original = Create("Goblin");
        var editor = new StatBlockEditorViewModel(original) { Name = "  Goblin Boss " };
        editor.NumberFields.Single(field => field.Label == "Armor Class").Value = 500;
        FeatureListEditorViewModel traits = editor.FeatureLists.Single(list => list.Title == "Traits");
        traits.AddCommand.Execute(null);
        traits.AddCommand.Execute(null);
        traits.Items[0].Name = "Nimble Escape";

        StatBlock result = editor.ToStatBlock();

        Assert.Equal(original.Id, result.Id);
        Assert.Equal("Goblin Boss", result.Name);
        Assert.Equal(99, result.ArmorClass);
        Assert.Equal("Nimble Escape", Assert.Single(result.Traits).Name);
    }

    private static StatBlock Create(string name)
    {
        return new StatBlock { Id = Guid.NewGuid(), Name = name };
    }
}