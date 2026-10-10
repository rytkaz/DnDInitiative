using DnDInitiative.Core.Spells;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Spells;
using DnDInitiative.Infrastructure.Tests.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Import;

public sealed class MarkdownSpellImporterTests : IDisposable
{
    private const string Fence = "```";

    private readonly TestDatabase _database = new();
    private readonly string _notesFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly SqliteSpellRepository _repository;
    private readonly MarkdownSpellImporter _importer;

    public MarkdownSpellImporterTests()
    {
        _repository = new SqliteSpellRepository(_database);
        _importer = new MarkdownSpellImporter(_repository);
        Directory.CreateDirectory(_notesFolder);
    }

    public void Dispose()
    {
        _database.Dispose();
        Directory.Delete(_notesFolder, recursive: true);
    }

    [Fact]
    public async Task ImportFolderAsync_FindsSpellNotesInSubfoldersAndIgnoresOtherNotes()
    {
        WriteNote("compendium/spells/fireball-xphb.md", SpellNote("Fireball", range: "150 feet"));
        WriteNote("compendium/spells/shield.md", SpellNote("Shield", range: "Self"));
        WriteNote("compendium/bestiary/goblin.md", $"# Goblin\n\n{Fence}statblock\nname: Goblin\n{Fence}");

        SpellSaveResult result = await _importer.ImportFolderAsync(_notesFolder);

        Assert.Equal(new SpellSaveResult(Added: 2, Updated: 0), result);
        Assert.Null(await _repository.FindAsync("goblin"));
    }

    [Fact]
    public async Task ImportFolderAsync_UsesTheFileNameAsTheKeyLinksPointAt()
    {
        WriteNote("compendium/spells/fireball-xphb.md", SpellNote("Fireball", range: "150 feet"));

        await _importer.ImportFolderAsync(_notesFolder);

        Assert.Equal("Fireball", (await _repository.FindAsync("fireball-xphb"))?.Name);
    }

    [Fact]
    public async Task ImportFolderAsync_RunTwice_UpdatesInsteadOfDuplicating()
    {
        WriteNote("spells/fireball.md", SpellNote("Fireball", range: "150 feet"));
        await _importer.ImportFolderAsync(_notesFolder);

        WriteNote("spells/fireball.md", SpellNote("Fireball", range: "200 feet"));
        SpellSaveResult result = await _importer.ImportFolderAsync(_notesFolder);

        Assert.Equal(new SpellSaveResult(Added: 0, Updated: 1), result);
        Assert.Equal("200 feet", (await _repository.FindAsync("fireball"))?.Range);
    }

    [Fact]
    public async Task ImportFolderAsync_NoSpellNotes_ReportsNothingImported()
    {
        WriteNote("readme.md", "# Nothing to see");

        Assert.Equal(new SpellSaveResult(Added: 0, Updated: 0), await _importer.ImportFolderAsync(_notesFolder));
    }

    private static string SpellNote(string name, string range)
    {
        return $"# {name}\n\n{Fence}spell\nname: {name}\nlevel: 3rd-level\nrange: {range}\n{Fence}\n\nThe text of {name}.";
    }

    private void WriteNote(string relativePath, string content)
    {
        string path = Path.Combine(_notesFolder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
