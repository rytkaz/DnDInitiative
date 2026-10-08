using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.ViewModels;

public class EncounterBuilderViewModelTests
{
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
        viewModel.SelectedStatBlock = _goblin;

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
        viewModel.SelectedStatBlock = _goblin;

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
        viewModel.SelectedStatBlock = _goblin;
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
        viewModel.SelectedStatBlock = _goblin;
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
        viewModel.SelectedStatBlock = _goblin;
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
        viewModel.SelectedStatBlock = _goblin;
        viewModel.AddCreatureCommand.Execute(null);
        Assert.Single(SelectedEditor(viewModel).Creatures).Quantity = 3;

        viewModel.StartEncounterCommand.Execute(null);

        StartCombatMessage message = Assert.IsType<StartCombatMessage>(recorder.Received);
        Assert.Equal("Ambush", message.Name);
        Assert.Same(_party, message.Party);
        Assert.Equal(3, message.Creatures.Count);
        Assert.All(message.Creatures, creature => Assert.Equal("Goblin", creature.Name));
    }

    private async Task<EncounterBuilderViewModel> CreateLoadedViewModelAsync(
        InMemoryEncounterRepository? encounters = null,
        IMessenger? messenger = null)
    {
        var viewModel = new EncounterBuilderViewModel(
            encounters ?? new InMemoryEncounterRepository(),
            new InMemoryStatBlockRepository(_goblin),
            new InMemoryPartyRepository(_party),
            messenger ?? new WeakReferenceMessenger());
        await viewModel.LoadCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static EncounterEditorViewModel SelectedEditor(EncounterBuilderViewModel viewModel)
    {
        return Assert.IsType<EncounterEditorViewModel>(viewModel.SelectedEncounter);
    }
}