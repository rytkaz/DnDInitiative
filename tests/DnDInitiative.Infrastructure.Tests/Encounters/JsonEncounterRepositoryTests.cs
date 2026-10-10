using DnDInitiative.Core.Encounters;
using DnDInitiative.Infrastructure.Campaigns;
using DnDInitiative.Infrastructure.Encounters;

using Microsoft.Extensions.Logging.Abstractions;

namespace DnDInitiative.Infrastructure.Tests.Campaigns;

public sealed class JsonEncounterRepositoryTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly JsonEncounterRepository _repository;

    public JsonEncounterRepositoryTests()
    {
        var location = new CampaignLocation { Folder = _folder };
        _repository = new JsonEncounterRepository(location, NullLogger<JsonEncounterRepository>.Instance);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsTheSameEncounter()
    {
        var encounter = new Encounter(
            Guid.NewGuid(),
            "Goblin Ambush",
            [new EncounterCreature(Guid.NewGuid(), 3), new EncounterCreature(Guid.NewGuid(), 1, "Boss")]);

        await _repository.SaveAsync(encounter);

        Encounter loaded = Assert.IsType<Encounter>(await _repository.GetAsync(encounter.Id));
        Assert.Equal("Goblin Ambush", loaded.Name);
        Assert.Equal(encounter.Creatures.ToArray(), loaded.Creatures.ToArray());
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        Assert.Null(await _repository.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAllAsync_SortsByNameIgnoringCase()
    {
        await _repository.SaveAsync(new Encounter(Guid.NewGuid(), "zombies", []));
        await _repository.SaveAsync(new Encounter(Guid.NewGuid(), "Ambush", []));

        IReadOnlyList<Encounter> all = await _repository.GetAllAsync();

        Assert.Equal(["Ambush", "zombies"], all.Select(encounter => encounter.Name).ToArray());
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyThatEncounter()
    {
        var ambush = new Encounter(Guid.NewGuid(), "Ambush", []);
        var siege = new Encounter(Guid.NewGuid(), "Siege", []);
        await _repository.SaveAsync(ambush);
        await _repository.SaveAsync(siege);

        await _repository.DeleteAsync(ambush.Id);

        Assert.Equal("Siege", Assert.Single(await _repository.GetAllAsync()).Name);
    }
}