using DnDInitiative.Core.Spells;

namespace DnDInitiative.Infrastructure.Import;

internal sealed class MarkdownSpellImporter : ISpellImporter
{
    private readonly ISpellRepository _spells;

    public MarkdownSpellImporter(ISpellRepository spells)
    {
        _spells = spells;
    }

    public async Task<SpellSaveResult> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        var spells = new List<Spell>();

        foreach (string file in Directory.EnumerateFiles(folderPath, "*.md", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string markdown = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);

            // Most notes are creatures. A plain text search rules them out more cheaply than the parser.
            if (!markdown.Contains(SpellNoteParser.FenceStart, StringComparison.Ordinal))
            {
                continue;
            }

            // The file name is the key: it is exactly what the converter's links to this spell end in.
            if (SpellNoteParser.Parse(markdown, Path.GetFileNameWithoutExtension(file)) is { } spell)
            {
                spells.Add(spell);
            }
        }

        return spells.Count > 0
            ? await _spells.SaveManyAsync(spells, cancellationToken).ConfigureAwait(false)
            : new SpellSaveResult(Added: 0, Updated: 0);
    }
}
