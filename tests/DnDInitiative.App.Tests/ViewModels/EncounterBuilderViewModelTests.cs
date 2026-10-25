using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.ViewModels;

public class EncounterBuilderViewModelTests
{
    private static readonly DateTimeOffset Monday = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tuesday = Monday.AddDays(1);
    private static readonly DateTimeOffset Wednesday = Monday.AddDays(2);

    private readonly FixedTimeProvider _clock = new() { Now = Wednesday };
    private readonly StatBlock _goblin = new() { Id = Guid.NewGuid(), Name = "Goblin", ChallengeRating = "1/4" };

    // Four level 1 characters: budgets of 200 / 300 / 400 XP.
    private readonly Party _party = new(
        Guid.NewGuid(),
        "Heroes",
        [
            new PlayerCharacter("Aria", Level: 1),
            new PlayerCharacter("Bren", Level: 1),
            new PlayerCharacter("Cato", Level: 1),
            new PlayerCharacter("Dara", Level: 1),
        ]);

    [Fact]
    public async Task LoadCommand_SelectsFirstPartyAndShowsItsBudget()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync();

        Assert.Same(_party, viewModel.SelectedParty);
        Assert.Equal(new XpBudget(200, 300, 400), viewModel.Budget);
    }

    [Fact]
    public async Task AddCreatureCommand_AddsARowAndUpdatesTheDifficulty()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync();
        viewModel.NewEncounterCommand.Execute(null);
        viewModel.SelectedStatBlock = Assert.Single(viewModel.FilteredStatBlocks);

        viewModel.AddCreatureCommand.Execute(null);

        EncounterCreatureViewModel row = Assert.Single(SelectedEditor(viewModel).Creatures);
        Assert.Equal("Goblin", row.StatBlockName);
        Assert.Equal(50, viewModel.TotalXp);
        Assert.Equal("Low", viewModel.DifficultyText);
    }

    [Fact]
    public async Task AddCreatureCommand_SameStatBlockAgain_RaisesTheCount()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync();
        viewModel.NewEncounterCommand.Execute(null);
        viewModel.SelectedStatBlock = Assert.Single(viewModel.FilteredStatBlocks);

        viewModel.AddCreatureCommand.Execute(null);
        viewModel.AddCreatureCommand.Execute(null);

        Assert.Equal(2, Assert.Single(SelectedEditor(viewModel).Creatures).Quantity);
        Assert.Equal(100, viewModel.TotalXp);
    }

    [Theory]
    [InlineData(4, 200, "Low")]
    [InlineData(5, 250, "Moderate")]
    [InlineData(8, 400, "High")]
    [InlineData(9, 450, "Above High")]
    public async Task ChangingACount_RecalculatesTheDifficulty(int quantity, int expectedXp, string expectedDifficulty)
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync();
        viewModel.NewEncounterCommand.Execute(null);
        viewModel.SelectedStatBlock = Assert.Single(viewModel.FilteredStatBlocks);
        viewModel.AddCreatureCommand.Execute(null);

        Assert.Single(SelectedEditor(viewModel).Creatures).Quantity = quantity;

        Assert.Equal(expectedXp, viewModel.TotalXp);
        Assert.Equal(expectedDifficulty, viewModel.DifficultyText);
    }

    [Fact]
    public async Task RemoveCreatureCommand_RemovesTheRowAndItsXp()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync();
        viewModel.NewEncounterCommand.Execute(null);
        viewModel.SelectedStatBlock = Assert.Single(viewModel.FilteredStatBlocks);
        viewModel.AddCreatureCommand.Execute(null);

        viewModel.RemoveCreatureCommand.Execute(Assert.Single(SelectedEditor(viewModel).Creatures));

        Assert.Empty(SelectedEditor(viewModel).Creatures);
        Assert.Equal(0, viewModel.TotalXp);
    }

    [Fact]
    public async Task SaveCommand_StoresTheEncounterWithItsCreatures()
    {
        var encounters = new InMemoryEncounterRepository();
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(encounters);
        viewModel.NewEncounterCommand.Execute(null);
        SelectedEditor(viewModel).Name = "Ambush";
        viewModel.SelectedStatBlock = Assert.Single(viewModel.FilteredStatBlocks);
        viewModel.AddCreatureCommand.Execute(null);
        Assert.Single(SelectedEditor(viewModel).Creatures).Quantity = 3;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Encounter saved = Assert.Single(encounters.Encounters);
        Assert.Equal("Ambush", saved.Name);
        Assert.Equal(new EncounterCreature(_goblin.Id, 3), Assert.Single(saved.Creatures));
    }

    [Fact]
    public async Task LoadCommand_StoredEncounter_ShowsItsCreaturesByName()
    {
        var stored = new Encounter(Guid.NewGuid(), "Ambush", [new EncounterCreature(_goblin.Id, 2, "Sneak")]);
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(new InMemoryEncounterRepository(stored));

        EncounterCreatureViewModel row = Assert.Single(SelectedEditor(viewModel).Creatures);

        Assert.Equal("Goblin", row.StatBlockName);
        Assert.Equal("Sneak", row.CustomName);
        Assert.Equal(100, viewModel.TotalXp);
    }

    [Fact]
    public async Task StartEncounterCommand_SendsTheCreaturesAndSelectedParty()
    {
        var messenger = new WeakReferenceMessenger();
        var recorder = new MessageRecorder();
        messenger.Register<StartCombatMessage>(recorder);
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(messenger: messenger);
        viewModel.NewEncounterCommand.Execute(null);
        SelectedEditor(viewModel).Name = "Ambush";
        viewModel.SelectedStatBlock = Assert.Single(viewModel.FilteredStatBlocks);
        viewModel.AddCreatureCommand.Execute(null);
        Assert.Single(SelectedEditor(viewModel).Creatures).Quantity = 3;

        viewModel.StartEncounterCommand.Execute(null);

        StartCombatMessage message = Assert.IsType<StartCombatMessage>(recorder.Received);
        Assert.Equal("Ambush", message.Name);
        Assert.Same(_party, message.Party);
        Assert.Equal(3, message.Creatures.Count);
        Assert.All(message.Creatures, creature => Assert.Equal("Goblin", creature.Name));
    }

    [Fact]
    public async Task CopyLinkCommand_SavesTheEncounterAndCopiesAMarkdownLink()
    {
        var encounters = new InMemoryEncounterRepository();
        var clipboard = new FakeClipboard();
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(encounters, clipboard: clipboard);
        viewModel.NewEncounterCommand.Execute(null);
        SelectedEditor(viewModel).Name = "Ambush";

        await viewModel.CopyLinkCommand.ExecuteAsync(null);

        Encounter saved = Assert.Single(encounters.Encounters);
        var expectedLink = new EncounterLink(saved.Id, _party.Id);
        Assert.Equal($"[Ambush]({expectedLink})", clipboard.Text);
    }

    [Fact]
    public async Task LoadMoreStatBlocksCommand_AppendsTheNextPage()
    {
        StatBlock[] many = Enumerable.Range(0, 150)
            .Select(index => new StatBlock { Id = Guid.NewGuid(), Name = $"Creature {index:D3}" })
            .ToArray();
        var viewModel = new EncounterBuilderViewModel(
            new InMemoryEncounterRepository(),
            new InMemoryStatBlockRepository(many),
            new InMemoryPartyRepository(_party),
            new WeakReferenceMessenger(),
            new FakeClipboard(),
            new FakeConfirmation(),
            new InMemoryUiStateStore(),
            _clock);
        await viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(100, viewModel.FilteredStatBlocks.Count);

        await viewModel.LoadMoreStatBlocksCommand.ExecuteAsync(null);

        Assert.Equal(150, viewModel.FilteredStatBlocks.Count);
    }

    [Fact]
    public async Task DeleteCommand_Confirmed_RemovesTheEncounter()
    {
        var encounters = new InMemoryEncounterRepository(new Encounter(Guid.NewGuid(), "Ambush", []));
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(encounters);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Empty(encounters.Encounters);
        Assert.Empty(viewModel.Encounters);
        Assert.Null(viewModel.SelectedEncounter);
    }

    [Fact]
    public async Task DeleteCommand_Declined_KeepsTheEncounter()
    {
        var encounters = new InMemoryEncounterRepository(new Encounter(Guid.NewGuid(), "Ambush", []));
        var confirmation = new FakeConfirmation { Answer = false };
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(encounters, confirmation: confirmation);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Single(confirmation.Questions);
        Assert.Single(encounters.Encounters);
        Assert.Equal("Ambush", SelectedEditor(viewModel).Name);
    }

    [Fact]
    public async Task UnsavedChanges_TracksEditsUntilTheyAreSaved()
    {
        var encounters = new InMemoryEncounterRepository(
            new Encounter(Guid.NewGuid(), "Ambush", [new EncounterCreature(_goblin.Id, 2)]));
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(encounters);
        Assert.Empty(viewModel.UnsavedChanges);

        Assert.Single(SelectedEditor(viewModel).Creatures).Quantity = 3;
        Assert.Equal("Encounter: Ambush", Assert.Single(viewModel.UnsavedChanges));

        await viewModel.SaveCommand.ExecuteAsync(null);
        Assert.Empty(viewModel.UnsavedChanges);
    }

    [Fact]
    public async Task LoadCommand_ListsEncountersByNameUnlessToldOtherwise()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(ThreeEncounters());

        Assert.Equal(["Ambush", "bridge", "Crypt"], Names(viewModel));
        Assert.Equal(EncounterSortOrder.Name, viewModel.SelectedSort.Order);
    }

    [Fact]
    public async Task SelectedSort_LastModified_PutsTheNewestFirstAndUndatedOnesLast()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(ThreeEncounters());

        viewModel.SelectedSort = viewModel.SortOptions.Single(option => option.Order == EncounterSortOrder.Modified);

        // Crypt was saved on Tuesday, Ambush on Monday, and bridge has no date.
        Assert.Equal(["Crypt", "Ambush", "bridge"], Names(viewModel));
    }

    [Fact]
    public async Task SelectedSort_Changed_KeepsTheSelectedEncounterSelected()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(ThreeEncounters());
        EncounterEditorViewModel ambush = SelectedEditor(viewModel);

        viewModel.SelectedSort = viewModel.SortOptions[1];

        Assert.Same(ambush, viewModel.SelectedEncounter);
    }

    [Fact]
    public async Task SelectedSort_IsRememberedForNextTime()
    {
        var uiState = new InMemoryUiStateStore();
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(ThreeEncounters(), uiState: uiState);

        viewModel.SelectedSort = viewModel.SortOptions[1];
        Assert.True(uiState.State.SortEncountersByDate);

        EncounterBuilderViewModel reopened = await CreateLoadedViewModelAsync(ThreeEncounters(), uiState: uiState);
        Assert.Equal(EncounterSortOrder.Modified, reopened.SelectedSort.Order);
        Assert.Equal(["Crypt", "Ambush", "bridge"], Names(reopened));
    }

    [Fact]
    public async Task SaveCommand_DatesTheEncounterAndMovesItToItsNewPlace()
    {
        InMemoryEncounterRepository encounters = ThreeEncounters();
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(encounters);
        viewModel.SelectedSort = viewModel.SortOptions[1];
        viewModel.SelectedEncounter = viewModel.Encounters.Single(encounter => encounter.Name == "bridge");

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(Wednesday, encounters.Encounters.Single(encounter => encounter.Name == "bridge").Modified);
        Assert.Equal(["bridge", "Crypt", "Ambush"], Names(viewModel));
        Assert.Empty(viewModel.UnsavedChanges);
    }

    [Fact]
    public async Task SaveCommand_RenamedEncounter_MovesToItsAlphabeticalPlace()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(ThreeEncounters());
        SelectedEditor(viewModel).Name = "Zombie ambush";

        // Not while typing, only once it is saved.
        Assert.Equal(["Zombie ambush", "bridge", "Crypt"], Names(viewModel));

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(["bridge", "Crypt", "Zombie ambush"], Names(viewModel));
    }

    [Fact]
    public async Task NewEncounterCommand_SortedByDate_StartsAtTheTop()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(ThreeEncounters());
        viewModel.SelectedSort = viewModel.SortOptions[1];

        viewModel.NewEncounterCommand.Execute(null);

        Assert.Equal("New encounter", viewModel.Encounters[0].Name);
        Assert.Same(viewModel.Encounters[0], viewModel.SelectedEncounter);
    }

    [Fact]
    public async Task CopyCommand_StoresACopyWithItsOwnIdAndSelectsIt()
    {
        var original = new Encounter(Guid.NewGuid(), "Ambush", [new EncounterCreature(_goblin.Id, 3, "Sneak")], Monday);
        var encounters = new InMemoryEncounterRepository(original);
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(encounters);

        await viewModel.CopyCommand.ExecuteAsync(null);

        Encounter copy = encounters.Encounters.Single(encounter => encounter.Id != original.Id);
        Assert.Equal("Ambush (copy)", copy.Name);
        Assert.Equal(new EncounterCreature(_goblin.Id, 3, "Sneak"), Assert.Single(copy.Creatures));
        Assert.Equal(Wednesday, copy.Modified);

        Assert.Equal(["Ambush", "Ambush (copy)"], Names(viewModel));
        Assert.Equal(copy.Id, SelectedEditor(viewModel).Id);
        Assert.Empty(viewModel.UnsavedChanges);
    }

    [Fact]
    public async Task CopyCommand_CopiesUnsavedEditsAndLeavesTheOriginalAsItWas()
    {
        var original = new Encounter(Guid.NewGuid(), "Ambush", [new EncounterCreature(_goblin.Id, 1)], Monday);
        var encounters = new InMemoryEncounterRepository(original);
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(encounters);
        Assert.Single(SelectedEditor(viewModel).Creatures).Quantity = 5;

        await viewModel.CopyCommand.ExecuteAsync(null);

        Encounter copy = encounters.Encounters.Single(encounter => encounter.Id != original.Id);
        Assert.Equal(5, Assert.Single(copy.Creatures).Quantity);
        Assert.Equal(1, Assert.Single(encounters.Encounters.Single(encounter => encounter.Id == original.Id).Creatures).Quantity);
    }

    [Fact]
    public async Task CopyCommand_NothingSelected_IsNotAvailable()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync();

        Assert.False(viewModel.CopyCommand.CanExecute(null));
    }

    [Fact]
    public async Task SelectedStatBlock_InTheLibrary_ShowsItsStatBlock()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync();

        viewModel.SelectedStatBlock = viewModel.FilteredStatBlocks.Single(summary => summary.Id == _goblin.Id);

        Assert.Equal("Goblin", viewModel.StatBlockDetails?.Name);
    }

    [Fact]
    public async Task SelectedCreature_InTheEncounter_ShowsItsStatBlock()
    {
        var encounters = new InMemoryEncounterRepository(
            new Encounter(Guid.NewGuid(), "Ambush", [new EncounterCreature(_goblin.Id, 2)]));
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(encounters);
        Assert.Null(viewModel.StatBlockDetails);

        viewModel.SelectedCreature = Assert.Single(SelectedEditor(viewModel).Creatures);

        Assert.Equal("Goblin", viewModel.StatBlockDetails?.Name);
    }

    [Fact]
    public async Task SelectedStatBlock_Cleared_KeepsShowingTheLastStatBlock()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync();
        viewModel.SelectedStatBlock = viewModel.FilteredStatBlocks[0];

        // What the list does when a search refills it.
        viewModel.SelectedStatBlock = null;
        viewModel.SelectedCreature = null;

        Assert.Equal("Goblin", viewModel.StatBlockDetails?.Name);
    }

    [Fact]
    public async Task ReloadCampaignDataAsync_ClearsThePreview()
    {
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync();
        viewModel.SelectedStatBlock = viewModel.FilteredStatBlocks[0];

        await viewModel.ReloadCampaignDataAsync();

        Assert.Null(viewModel.StatBlockDetails);
    }

    [Fact]
    public async Task Constructor_UsesTheSavedPanelLayout()
    {
        var uiState = new InMemoryUiStateStore
        {
            State = new UiState { EncounterStatBlockPanelWidth = 99999, ShowEncounterStatBlockPanel = false },
        };

        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(uiState: uiState);

        Assert.Equal(1200, viewModel.StatBlockPanelWidth);
        Assert.False(viewModel.ShowStatBlockPanel);
        Assert.Equal(0, uiState.SaveCount);
    }

    [Fact]
    public async Task PanelLayout_IsSavedWithoutDisturbingTheRestOfTheState()
    {
        var uiState = new InMemoryUiStateStore { State = new UiState { IsWindowMaximized = true } };
        EncounterBuilderViewModel viewModel = await CreateLoadedViewModelAsync(uiState: uiState);

        viewModel.ShowStatBlockPanel = false;
        viewModel.StatBlockPanelWidth = 500;
        viewModel.SaveLayout();

        Assert.Equal(
            new UiState { IsWindowMaximized = true, ShowEncounterStatBlockPanel = false, EncounterStatBlockPanelWidth = 500 },
            uiState.State);
    }

    private async Task<EncounterBuilderViewModel> CreateLoadedViewModelAsync(
        InMemoryEncounterRepository? encounters = null,
        IMessenger? messenger = null,
        FakeClipboard? clipboard = null,
        FakeConfirmation? confirmation = null,
        InMemoryUiStateStore? uiState = null)
    {
        var viewModel = new EncounterBuilderViewModel(
            encounters ?? new InMemoryEncounterRepository(),
            new InMemoryStatBlockRepository(_goblin),
            new InMemoryPartyRepository(_party),
            messenger ?? new WeakReferenceMessenger(),
            clipboard ?? new FakeClipboard(),
            confirmation ?? new FakeConfirmation(),
            uiState ?? new InMemoryUiStateStore(),
            _clock);
        await viewModel.LoadCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static InMemoryEncounterRepository ThreeEncounters()
    {
        return new InMemoryEncounterRepository(
            new Encounter(Guid.NewGuid(), "Crypt", [], Tuesday),
            new Encounter(Guid.NewGuid(), "Ambush", [], Monday),
            new Encounter(Guid.NewGuid(), "bridge", []));
    }

    private static string[] Names(EncounterBuilderViewModel viewModel)
    {
        return viewModel.Encounters.Select(encounter => encounter.Name).ToArray();
    }

    private static EncounterEditorViewModel SelectedEditor(EncounterBuilderViewModel viewModel)
    {
        return Assert.IsType<EncounterEditorViewModel>(viewModel.SelectedEncounter);
    }
}