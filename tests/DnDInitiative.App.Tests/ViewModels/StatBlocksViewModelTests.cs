using System.Globalization;

using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.ViewModels;

public class StatBlocksViewModelTests
{
    [Fact]
    public async Task LoadCommand_ListsStatBlocksByNameAndShowsTheFirst()
    {
        var viewModel = CreateViewModel(new InMemoryStatBlockRepository(Create("Orc"), Create("Goblin")));

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.FilteredStatBlocks.Count);
        Assert.Equal("Goblin", viewModel.FilteredStatBlocks[0].Name);
        Assert.Equal("Goblin", viewModel.SelectedStatBlock?.Name);
        Assert.Equal("Goblin", viewModel.Details?.Name);
    }

    [Fact]
    public async Task SearchText_FiltersByNameIgnoringCase()
    {
        var viewModel = CreateViewModel(new InMemoryStatBlockRepository(Create("Orc"), Create("Goblin")));
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SearchText = "OR";

        Assert.Equal("Orc", Assert.Single(viewModel.FilteredStatBlocks).Name);
    }

    [Fact]
    public async Task SaveCommand_NewStatBlock_StoresItAndLeavesEditMode()
    {
        var repository = new InMemoryStatBlockRepository();
        var viewModel = CreateViewModel(repository);
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
        var viewModel = CreateViewModel(repository);
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
        var viewModel = CreateViewModel(repository);
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

    [Fact]
    public void Editor_ToStatBlock_KeepsLegendaryMythicAndLairActionsWithTheirDescriptions()
    {
        StatBlock original = Create("Bog Tyrant") with
        {
            LegendaryActionsDescription = "Legendary Action Uses: 3.",
            LegendaryActions = [new StatBlockFeature("Stomp", "The ground shakes.")],
            MythicActionsDescription = "When its heart beats again.",
            MythicActions = [new StatBlockFeature("Mire", "Difficult terrain.")],
            LairActionsDescription = "On initiative count 20.",
            LairActions = [new StatBlockFeature("Swamp Gas", "A cloud rises.")],
        };
        var editor = new StatBlockEditorViewModel(original);
        FeatureListEditorViewModel lair = editor.FeatureLists.Single(list => list.Title == "Lair Actions");
        lair.Description = "  On initiative count 10.  ";

        StatBlock result = editor.ToStatBlock();

        Assert.Equal("Legendary Action Uses: 3.", result.LegendaryActionsDescription);
        Assert.Equal("Stomp", Assert.Single(result.LegendaryActions).Name);
        Assert.Equal("When its heart beats again.", result.MythicActionsDescription);
        Assert.Equal("Mire", Assert.Single(result.MythicActions).Name);
        Assert.Equal("On initiative count 10.", result.LairActionsDescription);
        Assert.Equal("Swamp Gas", Assert.Single(result.LairActions).Name);
        Assert.False(editor.FeatureLists.Single(list => list.Title == "Traits").HasDescription);
        Assert.True(lair.HasDescription);
    }

    [Fact]
    public async Task SaveCommand_EditThatLeavesTheListRowUnchanged_StillRefreshesTheDetails()
    {
        StatBlock goblin = Create("Goblin") with { ArmorClass = 15 };
        StatBlocksViewModel viewModel = CreateViewModel(new InMemoryStatBlockRepository(goblin));
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.EditCommand.Execute(null);
        Assert.IsType<StatBlockEditorViewModel>(viewModel.Editor)
            .NumberFields.Single(field => field.Label == "Armor Class").Value = 18;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(new LabeledValue("Armor Class", "18"), viewModel.Details?.CoreStats ?? []);
    }

    [Fact]
    public async Task LoadMoreCommand_AppendsTheNextPageUntilTheEnd()
    {
        StatBlocksViewModel viewModel = CreateViewModel(new InMemoryStatBlockRepository(CreateMany(250)));
        await viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(100, viewModel.FilteredStatBlocks.Count);

        await viewModel.LoadMoreCommand.ExecuteAsync(null);
        Assert.Equal(200, viewModel.FilteredStatBlocks.Count);

        await viewModel.LoadMoreCommand.ExecuteAsync(null);
        await viewModel.LoadMoreCommand.ExecuteAsync(null);
        Assert.Equal(250, viewModel.FilteredStatBlocks.Count);
        Assert.Equal(250, viewModel.FilteredStatBlocks.Select(summary => summary.Id).Distinct().Count());
    }

    [Fact]
    public async Task SearchText_Changed_StartsAgainFromTheFirstPage()
    {
        StatBlocksViewModel viewModel = CreateViewModel(new InMemoryStatBlockRepository(CreateMany(250)));
        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.LoadMoreCommand.ExecuteAsync(null);

        viewModel.SearchText = "Creature 00";

        Assert.Equal(10, viewModel.FilteredStatBlocks.Count);
    }

    [Fact]
    public async Task SaveCommand_StatBlockBeyondTheFirstPage_IsStillSelectedAfterwards()
    {
        var repository = new InMemoryStatBlockRepository(CreateMany(250));
        StatBlocksViewModel viewModel = CreateViewModel(repository);
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewCommand.Execute(null);
        Assert.IsType<StatBlockEditorViewModel>(viewModel.Editor).Name = "Zombie";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Zombie", viewModel.SelectedStatBlock?.Name);
        Assert.Equal("Zombie", viewModel.Details?.Name);
    }

    [Fact]
    public async Task DeleteCommand_Confirmed_RemovesTheStatBlock()
    {
        var repository = new InMemoryStatBlockRepository(Create("Goblin"));
        StatBlocksViewModel viewModel = CreateViewModel(repository);
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Empty(repository.StatBlocks);
        Assert.Empty(viewModel.FilteredStatBlocks);
        Assert.Null(viewModel.SelectedStatBlock);
    }

    [Fact]
    public async Task DeleteCommand_Declined_KeepsTheStatBlock()
    {
        var repository = new InMemoryStatBlockRepository(Create("Goblin"));
        var confirmation = new FakeConfirmation { Answer = false };
        StatBlocksViewModel viewModel = CreateViewModel(repository, confirmation: confirmation);
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Single(confirmation.Questions);
        Assert.Single(repository.StatBlocks);
        Assert.Equal("Goblin", viewModel.SelectedStatBlock?.Name);
    }

    [Fact]
    public void CancelCommand_UntouchedEditor_ClosesWithoutAsking()
    {
        var confirmation = new FakeConfirmation();
        StatBlocksViewModel viewModel = CreateViewModel(new InMemoryStatBlockRepository(), confirmation: confirmation);
        viewModel.NewCommand.Execute(null);

        viewModel.CancelCommand.Execute(null);

        Assert.False(viewModel.IsEditing);
        Assert.Empty(confirmation.Questions);
    }

    [Fact]
    public void CancelCommand_WithChangesAndDeclined_StaysInEditMode()
    {
        var confirmation = new FakeConfirmation { Answer = false };
        StatBlocksViewModel viewModel = CreateViewModel(new InMemoryStatBlockRepository(), confirmation: confirmation);
        viewModel.NewCommand.Execute(null);
        Assert.IsType<StatBlockEditorViewModel>(viewModel.Editor).Name = "Owlbear";

        viewModel.CancelCommand.Execute(null);

        Assert.True(viewModel.IsEditing);
        Assert.Single(confirmation.Questions);
        Assert.Equal("Stat block: Owlbear", Assert.Single(viewModel.UnsavedChanges));
    }

    [Fact]
    public void CancelCommand_WithChangesAndConfirmed_DiscardsThem()
    {
        StatBlocksViewModel viewModel = CreateViewModel(new InMemoryStatBlockRepository());
        viewModel.NewCommand.Execute(null);
        Assert.IsType<StatBlockEditorViewModel>(viewModel.Editor).Name = "Owlbear";

        viewModel.CancelCommand.Execute(null);

        Assert.False(viewModel.IsEditing);
        Assert.Empty(viewModel.UnsavedChanges);
    }

    [Fact]
    public async Task StatBlocksImportedMessage_RefreshesTheList()
    {
        var repository = new InMemoryStatBlockRepository();
        var messenger = new WeakReferenceMessenger();
        StatBlocksViewModel viewModel = CreateViewModel(repository, messenger: messenger);
        await viewModel.LoadCommand.ExecuteAsync(null);
        await repository.SaveAsync(Create("Goblin"));

        messenger.Send(new StatBlocksImportedMessage(new StatBlockImportResult(Added: 1, Updated: 0, FilesScanned: 1)));

        Assert.Equal("Goblin", Assert.Single(viewModel.FilteredStatBlocks).Name);
    }

    [Fact]
    public async Task CopyCommand_CopyOfAnImportedStatBlock_BelongsToTheUser()
    {
        var imported = new StatBlock { Id = Guid.NewGuid(), Name = "Goblin", Origin = StatBlockOrigin.Imported };
        var repository = new InMemoryStatBlockRepository(imported);
        StatBlocksViewModel viewModel = CreateViewModel(repository);
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.CopyCommand.ExecuteAsync(null);

        StatBlock copy = Assert.Single(repository.StatBlocks, statBlock => statBlock.Id != imported.Id);
        Assert.Equal(StatBlockOrigin.Custom, copy.Origin);
    }

    private static StatBlock[] CreateMany(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => Create("Creature " + index.ToString("D3", CultureInfo.InvariantCulture)))
            .ToArray();
    }

    private static StatBlock Create(string name)
    {
        return new StatBlock { Id = Guid.NewGuid(), Name = name };
    }

    private static StatBlocksViewModel CreateViewModel(
        InMemoryStatBlockRepository repository,
        FakeConfirmation? confirmation = null,
        IMessenger? messenger = null)
    {
        return new StatBlocksViewModel(
            repository,
            confirmation ?? new FakeConfirmation(),
            messenger ?? new WeakReferenceMessenger(),
            new InMemoryUiStateStore());
    }
}