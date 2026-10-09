using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.StatBlocks;
using DnDInitiative.Infrastructure.Tests.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Import;

public sealed class MarkdownStatBlockImporterTests : IDisposable
{
    private const string Fence = "```";

    private readonly TestDatabase _database = new();
    private readonly string _notesFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_notesFolder))
        {
            Directory.Delete(_notesFolder, recursive: true);
        }
    }

    [Fact]
    public async Task ImportFolderAsync_FindsStatBlocksInSubfoldersAndIgnoresOtherNotes()
    {
        WriteNote("bestiary/goblin.md", Statblock("Goblin", hp: 10));
        WriteNote("bestiary/giants/ogre.md", Statblock("Ogre", hp: 59));
        WriteNote("spells/fireball.md", "# Fireball");
        var repository = new SqliteStatBlockRepository(_database);

        StatBlockImportResult result = await new MarkdownStatBlockImporter(repository).ImportFolderAsync(_notesFolder);

        Assert.Equal(new StatBlockImportResult(Added: 2, Updated: 0, FilesScanned: 3), result);
        Assert.Equal(2, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task ImportFolderAsync_RunTwice_UpdatesInsteadOfDuplicating()
    {
        var repository = new SqliteStatBlockRepository(_database);
        var importer = new MarkdownStatBlockImporter(repository);
        WriteNote("goblin.md", Statblock("Goblin", hp: 10));
        await importer.ImportFolderAsync(_notesFolder);
        Guid originalId = Assert.Single(await repository.GetAllAsync()).Id;

        WriteNote("goblin.md", Statblock("Goblin", hp: 12));
        StatBlockImportResult result = await importer.ImportFolderAsync(_notesFolder);

        Assert.Equal(new StatBlockImportResult(Added: 0, Updated: 1, FilesScanned: 1), result);
        StatBlock goblin = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(originalId, goblin.Id);
        Assert.Equal(12, goblin.HitPoints);
    }

    private static string Statblock(string name, int hp)
    {
        return $"""
            # {name}

            {Fence}statblock
            name: {name}
            hp: {hp}
            source: MM
            {Fence}
            """;
    }

    private void WriteNote(string relativePath, string content)
    {
        string path = Path.Combine(_notesFolder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}