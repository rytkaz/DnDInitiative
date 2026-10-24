using DnDInitiative.Core.Conditions;
using DnDInitiative.Infrastructure.Conditions;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Tests.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Import;

public sealed class MarkdownConditionImporterTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly string _notesFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly SqliteConditionRepository _repository;
    private readonly MarkdownConditionImporter _importer;

    public MarkdownConditionImporterTests()
    {
        _repository = new SqliteConditionRepository(_database);
        _importer = new MarkdownConditionImporter(_repository);
        Directory.CreateDirectory(_notesFolder);
    }

    public void Dispose()
    {
        _database.Dispose();
        Directory.Delete(_notesFolder, recursive: true);
    }

    [Fact]
    public async Task ImportFolderAsync_ReadsTheConditionsNoteAndIgnoresOtherNotes()
    {
        WriteNote("rules/conditions.md", "# Conditions\n\n## Prone\n_Source: XPHB p. 367_\n\nYou can only crawl.\n\n## Blinded\n\nYou can't see.");
        WriteNote("rules/diseases.md", "# Diseases\n\n## Sewer Plague\n\nYou feel unwell.");

        int imported = await _importer.ImportFolderAsync(_notesFolder);

        Assert.Equal(2, imported);
        Assert.Equal("You can only crawl.", (await _repository.FindAsync("prone"))?.Description);
        Assert.Null(await _repository.FindAsync("Sewer Plague"));
    }

    [Fact]
    public async Task ImportFolderAsync_RunTwice_UpdatesInsteadOfDuplicating()
    {
        WriteNote("rules/conditions.md", "## Prone\n\nOld text.");
        await _importer.ImportFolderAsync(_notesFolder);

        WriteNote("rules/conditions.md", "## prone\n\nNew text.");
        await _importer.ImportFolderAsync(_notesFolder);

        Condition? prone = await _repository.FindAsync("PRONE");
        Assert.Equal("New text.", prone?.Description);
        Assert.Equal("Prone", prone?.Name);
    }

    [Fact]
    public async Task ImportFolderAsync_NoConditionsNote_ImportsNothing()
    {
        WriteNote("readme.md", "# Nothing to see");

        Assert.Equal(0, await _importer.ImportFolderAsync(_notesFolder));
    }

    private void WriteNote(string relativePath, string content)
    {
        string path = Path.Combine(_notesFolder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
