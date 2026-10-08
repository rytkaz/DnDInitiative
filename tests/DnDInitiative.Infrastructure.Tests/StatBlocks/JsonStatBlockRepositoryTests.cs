using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.StatBlocks;

namespace DnDInitiative.Infrastructure.Tests.StatBlocks;

public sealed class JsonStatBlockRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private string FilePath => Path.Combine(_directory, "statblocks.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task GetAllAsync_FileDoesNotExist_ReturnsEmptyList()
    {
        var repository = new JsonStatBlockRepository(FilePath);

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_NewStatBlock_CanBeReadBackFromDisk()
    {
        StatBlock goblin = CreateGoblin();
        await new JsonStatBlockRepository(FilePath).SaveAsync(goblin);

        StatBlock loaded = Assert.Single(await new JsonStatBlockRepository(FilePath).GetAllAsync());

        Assert.Equal(goblin.Id, loaded.Id);
        Assert.Equal("Goblin", loaded.Name);
        Assert.Equal("1/4", loaded.ChallengeRating);
        Assert.Equal(15, loaded.ArmorClass);
        Assert.Equal(goblin.Abilities, loaded.Abilities);
        Assert.Equal<StatBlockFeature>(goblin.Traits, loaded.Traits);
        Assert.Equal<StatBlockFeature>(goblin.Actions, loaded.Actions);
        Assert.Empty(loaded.Reactions);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_ReplacesStoredStatBlock()
    {
        var repository = new JsonStatBlockRepository(FilePath);
        StatBlock goblin = CreateGoblin();
        await repository.SaveAsync(goblin);

        await repository.SaveAsync(goblin with { HitPoints = 99 });

        StatBlock loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(99, loaded.HitPoints);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheStatBlockWithThatId()
    {
        var repository = new JsonStatBlockRepository(FilePath);
        StatBlock keep = CreateGoblin();
        StatBlock remove = CreateGoblin();
        await repository.SaveAsync(keep);
        await repository.SaveAsync(remove);

        await repository.DeleteAsync(remove.Id);

        StatBlock loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(keep.Id, loaded.Id);
    }

    private static StatBlock CreateGoblin()
    {
        return new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Goblin",
            Size = "Small",
            CreatureType = "Fey (Goblinoid)",
            ArmorClass = 15,
            HitPoints = 10,
            InitiativeModifier = 2,
            Abilities = new AbilityScores(8, 15, 10, 10, 8, 8),
            ChallengeRating = "1/4",
            Traits = [new StatBlockFeature("Nimble Escape", "Disengage or Hide as a bonus action.")],
            Actions = [new StatBlockFeature("Scimitar", "Melee attack, 1d6 + 2 slashing damage.")],
        };
    }
}