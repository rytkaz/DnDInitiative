using System.Text.Json;

using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Encounters;
using DnDInitiative.Infrastructure.Parties;
using DnDInitiative.Infrastructure.Persistence;
using DnDInitiative.Infrastructure.StatBlocks;

namespace DnDInitiative.Infrastructure.Tests.Persistence;

public sealed class LegacyJsonImporterTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly SqlitePartyRepository _parties;
    private readonly SqliteStatBlockRepository _statBlocks;
    private readonly SqliteEncounterRepository _encounters;
    private readonly LegacyJsonImporter _importer;

    public LegacyJsonImporterTests()
    {
        Directory.CreateDirectory(_dataDirectory);
        _parties = new SqlitePartyRepository(_database);
        _statBlocks = new SqliteStatBlockRepository(_database);
        _encounters = new SqliteEncounterRepository(_database);
        _importer = new LegacyJsonImporter(new StorageOptions(_dataDirectory), _parties, _statBlocks, _encounters);
    }

    public void Dispose()
    {
        _database.Dispose();
        Directory.Delete(_dataDirectory, recursive: true);
    }

    [Fact]
    public async Task ImportAsync_CopiesEachJsonFileIntoTheDatabaseAndRenamesIt()
    {
        var goblin = new StatBlock { Id = Guid.NewGuid(), Name = "Goblin", HitPoints = 10 };
        var party = new Party(Guid.NewGuid(), "Heroes", [new PlayerCharacter("Aria", Level: 3)]);
        var encounter = new Encounter(Guid.NewGuid(), "Ambush", [new EncounterCreature(goblin.Id, 3)]);
        WriteJson("statblocks.json", goblin);
        WriteJson("parties.json", party);
        WriteJson("encounters.json", encounter);

        await _importer.ImportAsync(CancellationToken.None);

        Assert.Equal(10, Assert.Single(await _statBlocks.GetAllAsync()).HitPoints);
        Assert.Equal("Aria", Assert.Single(Assert.Single(await _parties.GetAllAsync()).Members).Name);
        Assert.Equal(new EncounterCreature(goblin.Id, 3), Assert.Single(Assert.Single(await _encounters.GetAllAsync()).Creatures));

        Assert.False(File.Exists(PathTo("parties.json")));
        Assert.True(File.Exists(PathTo("parties.json.imported")));
        Assert.True(File.Exists(PathTo("statblocks.json.imported")));
        Assert.True(File.Exists(PathTo("encounters.json.imported")));
    }

    [Fact]
    public async Task ImportAsync_NoJsonFiles_DoesNothing()
    {
        await _importer.ImportAsync(CancellationToken.None);

        Assert.Empty(await _parties.GetAllAsync());
        Assert.Empty(Directory.GetFiles(_dataDirectory));
    }

    [Fact]
    public async Task ImportAsync_UnreadableFile_IsSetAsideAndTheRestStillImports()
    {
        File.WriteAllText(PathTo("parties.json"), "{ this is not valid");
        WriteJson("statblocks.json", new StatBlock { Id = Guid.NewGuid(), Name = "Goblin" });

        await _importer.ImportAsync(CancellationToken.None);

        Assert.Empty(await _parties.GetAllAsync());
        Assert.True(File.Exists(PathTo("parties.json.unreadable")));
        Assert.Single(await _statBlocks.GetAllAsync());
    }

    private string PathTo(string fileName)
    {
        return Path.Combine(_dataDirectory, fileName);
    }

    private void WriteJson<T>(string fileName, T item)
    {
        File.WriteAllText(PathTo(fileName), JsonSerializer.Serialize(new List<T> { item }));
    }
}