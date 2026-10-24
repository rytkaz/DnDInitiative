using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Infrastructure.Import;

public sealed class MarkdownStatBlockImporter : IStatBlockImporter
{
    private readonly IStatBlockLibrary _library;
    private readonly IEncounterRepository _encounters;

    public MarkdownStatBlockImporter(IStatBlockLibrary library, IEncounterRepository encounters)
    {
        _library = library;
        _encounters = encounters;
    }

    public async Task<StatBlockImportResult> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        // Keyed by name and source, so a creature that appears in two notes is imported once.
        var parsedByKey = new Dictionary<string, StatBlock>(StringComparer.Ordinal);
        int filesScanned = 0;

        foreach (string file in Directory.EnumerateFiles(folderPath, "*.md", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            filesScanned++;

            string markdown = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            foreach (StatBlock parsed in FantasyStatblockParser.ParseMarkdown(markdown))
            {
                parsedByKey[StatBlockIdentity.Key(parsed.Name, parsed.Source)] = parsed;
            }
        }

        (int added, int updated) = await ImportedStatBlocks
            .SaveAsync(_library, _encounters, parsedByKey.Values, cancellationToken)
            .ConfigureAwait(false);

        return new StatBlockImportResult(added, updated, filesScanned);
    }
}
