using DnDInitiative.Core.Encounters;
using DnDInitiative.Infrastructure.Encounters;
using DnDInitiative.Infrastructure.Tests.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Encounters;

public sealed class SqliteEncounterRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task SaveAsync_NewEncounter_CanBeReadBackWithCreaturesInOrder()
    {
        var repository = new SqliteEncounterRepository(_database);
        var encounter = new Encounter(
            Guid.NewGuid(),
            "Ambush",
            [
                new EncounterCreature(Guid.NewGuid(), 3),
                new EncounterCreature(Guid.NewGuid(), 1, "Grukk the Ogre"),
                new EncounterCreature(Guid.NewGuid(), 2),
            ]);

        await repository.SaveAsync(encounter);

        Encounter loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(encounter.Id, loaded.Id);
        Assert.Equal("Ambush", loaded.Name);
        Assert.Equal<EncounterCreature>(encounter.Creatures, loaded.Creatures);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_ReplacesNameAndCreatures()
    {
        var repository = new SqliteEncounterRepository(_database);
        var original = new Encounter(
            Guid.NewGuid(),
            "Ambush",
            [new EncounterCreature(Guid.NewGuid(), 3), new EncounterCreature(Guid.NewGuid(), 1)]);
        await repository.SaveAsync(original);
        var replacement = new EncounterCreature(Guid.NewGuid(), 5);

        await repository.SaveAsync(original with { Name = "Second Wave", Creatures = [replacement] });

        Encounter loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal("Second Wave", loaded.Name);
        Assert.Equal(replacement, Assert.Single(loaded.Creatures));
    }

    [Fact]
    public async Task GetAsync_ReturnsTheEncounterWithThatIdOrNull()
    {
        var repository = new SqliteEncounterRepository(_database);
        var first = new Encounter(Guid.NewGuid(), "First", []);
        var second = new Encounter(Guid.NewGuid(), "Second", [new EncounterCreature(Guid.NewGuid(), 2)]);
        await repository.SaveAsync(first);
        await repository.SaveAsync(second);

        Encounter? loaded = await repository.GetAsync(second.Id);

        Assert.Equal("Second", loaded?.Name);
        Assert.Equal(2, Assert.Single(loaded?.Creatures ?? []).Quantity);
        Assert.Null(await repository.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheEncounterWithThatId()
    {
        var repository = new SqliteEncounterRepository(_database);
        var keep = new Encounter(Guid.NewGuid(), "Keep", [new EncounterCreature(Guid.NewGuid(), 1)]);
        var remove = new Encounter(Guid.NewGuid(), "Remove", [new EncounterCreature(Guid.NewGuid(), 1)]);
        await repository.SaveAsync(keep);
        await repository.SaveAsync(remove);

        await repository.DeleteAsync(remove.Id);

        Assert.Equal(keep.Id, Assert.Single(await repository.GetAllAsync()).Id);
    }
}