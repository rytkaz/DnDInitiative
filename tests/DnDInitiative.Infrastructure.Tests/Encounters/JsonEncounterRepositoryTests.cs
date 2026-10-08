using DnDInitiative.Core.Encounters;
using DnDInitiative.Infrastructure.Encounters;

namespace DnDInitiative.Infrastructure.Tests.Encounters;

public sealed class JsonEncounterRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private string FilePath => Path.Combine(_directory, "encounters.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_NewEncounter_CanBeReadBackFromDisk()
    {
        Encounter encounter = CreateEncounter("Ambush");
        await new JsonEncounterRepository(FilePath).SaveAsync(encounter);

        Encounter loaded = Assert.Single(await new JsonEncounterRepository(FilePath).GetAllAsync());

        Assert.Equal(encounter.Id, loaded.Id);
        Assert.Equal("Ambush", loaded.Name);
        Assert.Equal<EncounterCreature>(encounter.Creatures, loaded.Creatures);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_ReplacesStoredEncounter()
    {
        var repository = new JsonEncounterRepository(FilePath);
        Encounter encounter = CreateEncounter("Ambush");
        await repository.SaveAsync(encounter);

        await repository.SaveAsync(encounter with { Name = "Renamed" });

        Assert.Equal("Renamed", Assert.Single(await repository.GetAllAsync()).Name);
    }

    [Fact]
    public async Task GetAsync_ReturnsTheEncounterWithThatIdOrNull()
    {
        var repository = new JsonEncounterRepository(FilePath);
        Encounter first = CreateEncounter("First");
        Encounter second = CreateEncounter("Second");
        await repository.SaveAsync(first);
        await repository.SaveAsync(second);

        Assert.Equal("Second", (await repository.GetAsync(second.Id))?.Name);
        Assert.Null(await repository.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheEncounterWithThatId()
    {
        var repository = new JsonEncounterRepository(FilePath);
        Encounter keep = CreateEncounter("Keep");
        Encounter remove = CreateEncounter("Remove");
        await repository.SaveAsync(keep);
        await repository.SaveAsync(remove);

        await repository.DeleteAsync(remove.Id);

        Assert.Equal(keep.Id, Assert.Single(await repository.GetAllAsync()).Id);
    }

    private static Encounter CreateEncounter(string name)
    {
        return new Encounter(
            Guid.NewGuid(),
            name,
            [new EncounterCreature(Guid.NewGuid(), 3), new EncounterCreature(Guid.NewGuid(), 1, "Boss")]);
    }
}