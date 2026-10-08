using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.StatBlocks;

namespace DnDInitiative.Infrastructure.Tests.Import;

public sealed class MarkdownStatBlockImporterTests : IDisposable
{
    private const string Fence = "```";

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

    private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private string NotesFolder => Path.Combine(_directory, "notes");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportFolderAsync_FindsStatBlocksInSubfoldersAndIgnoresOtherNotes()
    {
        WriteNote("bestiary/goblin.md", Statblock("Goblin", hp: 10));
        WriteNote("bestiary/giants/ogre.md", Statblock("Ogre", hp: 59));
        WriteNote("spells/fireball.md", "# Fireball");
        var repository = new JsonStatBlockRepository(Path.Combine(_directory, "statblocks.json"));

        StatBlockImportResult result = await new MarkdownStatBlockImporter(repository).ImportFolderAsync(NotesFolder);

        Assert.Equal(new StatBlockImportResult(Added: 2, Updated: 0, FilesScanned: 3), result);
        Assert.Equal(2, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task ImportFolderAsync_RunTwice_UpdatesInsteadOfDuplicating()
    {
        var repository = new JsonStatBlockRepository(Path.Combine(_directory, "statblocks.json"));
        var importer = new MarkdownStatBlockImporter(repository);
        WriteNote("goblin.md", Statblock("Goblin", hp: 10));
        await importer.ImportFolderAsync(NotesFolder);
        Guid originalId = Assert.Single(await repository.GetAllAsync()).Id;

        WriteNote("goblin.md", Statblock("Goblin", hp: 12));
        StatBlockImportResult result = await importer.ImportFolderAsync(NotesFolder);

        Assert.Equal(new StatBlockImportResult(Added: 0, Updated: 1, FilesScanned: 1), result);
        StatBlock goblin = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(originalId, goblin.Id);
        Assert.Equal(12, goblin.HitPoints);
    }

    private void WriteNote(string relativePath, string content)
    {
        string path = Path.Combine(NotesFolder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}