using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Campaigns;
using DnDInitiative.Infrastructure.StatBlocks;

using Microsoft.Extensions.Logging.Abstractions;

namespace DnDInitiative.Infrastructure.Tests.Persistence;

public sealed class CampaignStatBlockRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly string _folder = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly SqliteStatBlockRepository _library;
    private readonly CampaignStatBlockRepository _repository;

    public CampaignStatBlockRepositoryTests()
    {
        _library = new SqliteStatBlockRepository(_database);
        _repository = new CampaignStatBlockRepository(
            new CampaignLocation { Folder = _folder },
            _library,
            NullLogger<CampaignStatBlockRepository>.Instance);
    }

    private string StatBlocksFolder => Path.Combine(_folder, "statblocks");

    public void Dispose()
    {
        _database.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task SaveAsync_OwnStatBlock_BecomesAFileAndStaysOutOfTheDatabase()
    {
        await _repository.SaveAsync(Own("Owlbear"));

        Assert.True(File.Exists(Path.Combine(StatBlocksFolder, "owlbear.json")));
        Assert.Empty(await _library.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_ImportedStatBlock_GoesToTheDatabaseAndNotTheFolder()
    {
        await _repository.SaveAsync(Imported("Goblin"));

        Assert.False(Directory.Exists(StatBlocksFolder));
        Assert.Equal("Goblin", Assert.Single(await _library.GetAllAsync()).Name);
    }

    [Fact]
    public async Task GetAllAsync_ShowsBothKindsOnceEachInNameOrder()
    {
        await _repository.SaveAsync(Imported("Goblin"));
        await _repository.SaveAsync(Own("Owlbear"));
        await _repository.SaveAsync(Imported("Zombie"));

        IReadOnlyList<StatBlock> all = await _repository.GetAllAsync();

        Assert.Equal(["Goblin", "Owlbear", "Zombie"], all.Select(statBlock => statBlock.Name).ToArray());
    }

    [Fact]
    public async Task SaveAsync_EditedImportedStatBlock_CoversTheOriginalWithoutChangingIt()
    {
        StatBlock goblin = Imported("Goblin") with { HitPoints = 7 };
        await _repository.SaveAsync(goblin);

        // What the editor does: same Id, changed values, origin back to Custom.
        await _repository.SaveAsync(goblin with { HitPoints = 99, Origin = StatBlockOrigin.Custom });

        Assert.Equal(99, (await _repository.GetAsync(goblin.Id))?.HitPoints);
        Assert.Equal(99, Assert.Single(await _repository.GetAllAsync()).HitPoints);
        Assert.Equal(99, Assert.Single(await _repository.GetManyAsync([goblin.Id])).HitPoints);
        Assert.Equal(7, Assert.Single(await _library.GetAllAsync()).HitPoints);
    }

    [Fact]
    public async Task DeleteAsync_EditedImportedStatBlock_BringsTheOriginalBack()
    {
        StatBlock goblin = Imported("Goblin") with { HitPoints = 7 };
        await _repository.SaveAsync(goblin);
        await _repository.SaveAsync(goblin with { HitPoints = 99, Origin = StatBlockOrigin.Custom });

        await _repository.DeleteAsync(goblin.Id);

        Assert.Equal(7, (await _repository.GetAsync(goblin.Id))?.HitPoints);
    }

    [Fact]
    public async Task DeleteAsync_ImportedStatBlock_RemovesItFromTheDatabase()
    {
        StatBlock goblin = Imported("Goblin");
        await _repository.SaveAsync(goblin);

        await _repository.DeleteAsync(goblin.Id);

        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task SearchAsync_PagesThroughBothKindsInOneNameOrder()
    {
        await _repository.SaveManyAsync([Imported("Ape"), Imported("Cat"), Imported("Eagle")]);
        await _repository.SaveAsync(Own("Bear"));
        await _repository.SaveAsync(Own("Dog"));

        string[] firstPage = Names(await _repository.SearchAsync("", limit: 2));
        string[] secondPage = Names(await _repository.SearchAsync("", limit: 2, skip: 2));
        string[] thirdPage = Names(await _repository.SearchAsync("", limit: 2, skip: 4));

        Assert.Equal(["Ape", "Bear"], firstPage);
        Assert.Equal(["Cat", "Dog"], secondPage);
        Assert.Equal(["Eagle"], thirdPage);
    }

    [Fact]
    public async Task SearchAsync_EditedImportedStatBlock_AppearsOnceUnderItsNewName()
    {
        StatBlock cat = Imported("Cat");
        await _repository.SaveManyAsync([Imported("Ape"), cat, Imported("Eagle")]);
        await _repository.SaveAsync(cat with { Name = "Cat, Giant", Origin = StatBlockOrigin.Custom });

        string[] names = Names(await _repository.SearchAsync("", limit: 10));

        Assert.Equal(["Ape", "Cat, Giant", "Eagle"], names);
    }

    [Fact]
    public async Task SearchAsync_FiltersBothKindsByName()
    {
        await _repository.SaveAsync(Imported("Goblin"));
        await _repository.SaveAsync(Own("Goblin King"));
        await _repository.SaveAsync(Own("Owlbear"));

        string[] names = Names(await _repository.SearchAsync("gob", limit: 10));

        Assert.Equal(["Goblin", "Goblin King"], names);
    }

    [Fact]
    public async Task SearchAsync_SaysWhichStatBlocksAreTheUsersOwn()
    {
        await _repository.SaveAsync(Imported("Goblin"));
        await _repository.SaveAsync(Own("Owlbear King"));

        IReadOnlyList<StatBlockSummary> results = await _repository.SearchAsync("", limit: 10);

        Assert.False(results.Single(summary => summary.Name == "Goblin").IsOwn);
        Assert.True(results.Single(summary => summary.Name == "Owlbear King").IsOwn);
    }

    private static StatBlock Own(string name)
    {
        return new StatBlock { Id = Guid.NewGuid(), Name = name, Origin = StatBlockOrigin.Custom };
    }

    private static StatBlock Imported(string name)
    {
        return new StatBlock { Id = Guid.NewGuid(), Name = name, Origin = StatBlockOrigin.Imported };
    }

    private static string[] Names(IReadOnlyList<StatBlockSummary> summaries)
    {
        return summaries.Select(summary => summary.Name).ToArray();
    }
}