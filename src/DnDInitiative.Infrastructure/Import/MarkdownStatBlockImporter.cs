using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Infrastructure.Import;

public sealed class MarkdownStatBlockImporter : IStatBlockImporter
{
    private readonly IStatBlockRepository _repository;

    public MarkdownStatBlockImporter(IStatBlockRepository repository)
    {
        _repository = repository;
    }

    public async Task<StatBlockImportResult> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        // A stat block already in the library with the same name and source is
        // the same creature: reuse its Id so the import replaces it.
        IReadOnlyList<StatBlock> existing = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var existingIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (StatBlock statBlock in existing)
        {
            existingIds.TryAdd(Key(statBlock), statBlock.Id);
        }

        var imported = new Dictionary<string, StatBlock>(StringComparer.OrdinalIgnoreCase);
        int filesScanned = 0;

        foreach (string file in Directory.EnumerateFiles(folderPath, "*.md", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            filesScanned++;

            string markdown = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            foreach (StatBlock parsed in FantasyStatblockParser.ParseMarkdown(markdown))
            {
                string key = Key(parsed);
                imported[key] = existingIds.TryGetValue(key, out Guid id) ? parsed with { Id = id } : parsed;
            }
        }

        if (imported.Count > 0)
        {
            await _repository.SaveManyAsync(imported.Values, cancellationToken).ConfigureAwait(false);
        }

        int updated = imported.Keys.Count(existingIds.ContainsKey);
        return new StatBlockImportResult(imported.Count - updated, updated, filesScanned);
    }

    private static string Key(StatBlock statBlock)
    {
        return statBlock.Name + "|" + statBlock.Source;
    }
}