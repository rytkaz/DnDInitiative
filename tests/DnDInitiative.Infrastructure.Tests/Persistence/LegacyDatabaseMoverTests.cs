using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Campaigns;
using DnDInitiative.Infrastructure.Encounters;
using DnDInitiative.Infrastructure.Parties;
using DnDInitiative.Infrastructure.Persistence;
using DnDInitiative.Infrastructure.StatBlocks;

using Microsoft.Extensions.Logging.Abstractions;

namespace DnDInitiative.Infrastructure.Tests.Persistence;

public sealed class LegacyDatabaseMoverTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly string _root = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;

    private readonly SqlitePartyRepository _oldParties;
    private readonly SqliteEncounterRepository _oldEncounters;
    private readonly SqliteStatBlockRepository _library;
    private readonly JsonPartyRepository _parties;
    private readonly JsonEncounterRepository _encounters;
    private readonly CampaignStatBlockRepository _statBlocks;
    private readonly LegacyDatabaseMover _mover;

    public LegacyDatabaseMoverTests()
    {
        var location = new CampaignLocation();
        var storage = new StorageOptions(Path.Combine(_root, "appdata"));
        Directory.CreateDirectory(storage.DataDirectory);
        var campaigns = new CampaignService(
            location,
            new CampaignOptions(CampaignFolder),
            storage,
            NullLogger<CampaignService>.Instance);
        campaigns.OpenAsync(CampaignFolder).GetAwaiter().GetResult();

        _oldParties = new SqlitePartyRepository(_database);
        _oldEncounters = new SqliteEncounterRepository(_database);
        _library = new SqliteStatBlockRepository(_database);
        _parties = new JsonPartyRepository(location, NullLogger<JsonPartyRepository>.Instance);
        _encounters = new JsonEncounterRepository(location, NullLogger<JsonEncounterRepository>.Instance);
        _statBlocks = new CampaignStatBlockRepository(location, _library, NullLogger<CampaignStatBlockRepository>.Instance);

        _mover = new LegacyDatabaseMover(
            _database,
            storage,
            campaigns,
            _oldParties,
            _oldEncounters,
            _library,
            _parties,
            _encounters,
            _statBlocks,
            NullLogger<LegacyDatabaseMover>.Instance);
    }

    private string CampaignFolder => Path.Combine(_root, "campaign");

    private string BackupPath => Path.Combine(_root, "appdata", "dndinitiative.before-campaigns.db");

    public void Dispose()
    {
        _database.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task MoveAsync_NothingInTheDatabase_DoesNothing()
    {
        await _mover.MoveAsync(CancellationToken.None);

        Assert.False(File.Exists(BackupPath));
        Assert.False(Directory.Exists(Path.Combine(CampaignFolder, "parties")));
    }

    [Fact]
    public async Task MoveAsync_MovesPartiesAndEncountersAndKeepsABackup()
    {
        await _oldParties.SaveAsync(new Party(Guid.NewGuid(), "Heroes", [new PlayerCharacter("Aria", Level: 3)]));
        await _oldEncounters.SaveAsync(new Encounter(Guid.NewGuid(), "Ambush", [new EncounterCreature(Guid.NewGuid(), 2)]));

        await _mover.MoveAsync(CancellationToken.None);

        Assert.Equal("Aria", Assert.Single(Assert.Single(await _parties.GetAllAsync()).Members).Name);
        Assert.Equal(2, Assert.Single(Assert.Single(await _encounters.GetAllAsync()).Creatures).Quantity);
        Assert.Empty(await _oldParties.GetAllAsync());
        Assert.Empty(await _oldEncounters.GetAllAsync());
        Assert.True(File.Exists(BackupPath));
    }

    [Fact]
    public async Task MoveAsync_LibraryHasBeenImported_MovesOnlyTheUsersOwnStatBlocks()
    {
        await _library.SaveManyAsync(
        [
            new StatBlock { Id = Guid.NewGuid(), Name = "Goblin", Origin = StatBlockOrigin.Imported },
            new StatBlock { Id = Guid.NewGuid(), Name = "Owlbear King", Origin = StatBlockOrigin.Custom },
        ]);

        await _mover.MoveAsync(CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(CampaignFolder, "statblocks", "owlbear-king.json")));
        Assert.Equal("Goblin", Assert.Single(await _library.GetAllAsync()).Name);
        Assert.Equal(2, (await _statBlocks.GetAllAsync()).Count);
    }

    [Fact]
    public async Task MoveAsync_LibraryNeverImported_LeavesStatBlocksInTheDatabase()
    {
        // An old database: nothing is marked Imported, so nothing can be told apart.
        await _library.SaveManyAsync(
        [
            new StatBlock { Id = Guid.NewGuid(), Name = "Goblin" },
            new StatBlock { Id = Guid.NewGuid(), Name = "Ogre" },
        ]);

        await _mover.MoveAsync(CancellationToken.None);

        Assert.False(Directory.Exists(Path.Combine(CampaignFolder, "statblocks")));
        Assert.Equal(2, (await _library.GetAllAsync()).Count);
    }

    [Fact]
    public async Task MoveAsync_RunAgain_ChangesNothing()
    {
        await _oldParties.SaveAsync(new Party(Guid.NewGuid(), "Heroes", []));
        await _mover.MoveAsync(CancellationToken.None);
        Party moved = Assert.Single(await _parties.GetAllAsync());
        await _parties.SaveAsync(moved with { Name = "Renamed since" });

        await _mover.MoveAsync(CancellationToken.None);

        Assert.Equal("Renamed since", Assert.Single(await _parties.GetAllAsync()).Name);
    }
}