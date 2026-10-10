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

        Dictionary<string, StatBlock> existingByKey = await LoadExistingAsync(cancellationToken).ConfigureAwait(false);

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
                parsedByKey[StatBlockIdentity.Key(parsed.Name, parsed.Source)] = parsed with
                {
                    Id = StatBlockIdentity.ForImported(parsed.Name, parsed.Source),
                    Origin = StatBlockOrigin.Imported,
                };
            }
        }

        var toSave = new List<StatBlock>();
        var idChanges = new Dictionary<Guid, Guid>();
        int added = 0;
        int updated = 0;

        foreach ((string key, StatBlock incoming) in parsedByKey)
        {
            if (!existingByKey.TryGetValue(key, out StatBlock? existing))
            {
                added++;
                toSave.Add(incoming);
                continue;
            }

            // The user edited this imported creature. Their version wins over the book's.
            if (existing.Id == incoming.Id && existing.Origin == StatBlockOrigin.Custom)
            {
                continue;
            }

            // Stored before Ids were calculated: move it to its calculated Id first,
            // so encounters that use it keep working.
            if (existing.Id != incoming.Id)
            {
                idChanges[existing.Id] = incoming.Id;
            }

            updated++;
            toSave.Add(incoming);
        }

        if (idChanges.Count > 0)
        {
            await _library.ChangeIdsAsync(idChanges, cancellationToken).ConfigureAwait(false);
            await RepointEncountersAsync(idChanges, cancellationToken).ConfigureAwait(false);
        }

        if (toSave.Count > 0)
        {
            await _library.SaveManyAsync(toSave, cancellationToken).ConfigureAwait(false);
        }

        return new StatBlockImportResult(added, updated, filesScanned);
    }

    private async Task<Dictionary<string, StatBlock>> LoadExistingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<StatBlock> existing = await _library.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var byKey = new Dictionary<string, StatBlock>(StringComparer.Ordinal);
        foreach (StatBlock statBlock in existing)
        {
            string key = StatBlockIdentity.Key(statBlock.Name, statBlock.Source);

            // If several stored stat blocks share a name and source, prefer the one that
            // already has the calculated Id. Moving another onto that Id would collide.
            bool hasCalculatedId = statBlock.Id == StatBlockIdentity.ForImported(statBlock.Name, statBlock.Source);
            if (hasCalculatedId || !byKey.ContainsKey(key))
            {
                byKey[key] = statBlock;
            }
        }

        return byKey;
    }

    // Encounters refer to stat blocks by Id, so they have to follow an Id that moved.
    private async Task RepointEncountersAsync(Dictionary<Guid, Guid> idChanges, CancellationToken cancellationToken)
    {
        IReadOnlyList<Encounter> encounters = await _encounters.GetAllAsync(cancellationToken).ConfigureAwait(false);

        foreach (Encounter encounter in encounters)
        {
            if (!encounter.Creatures.Any(creature => idChanges.ContainsKey(creature.StatBlockId)))
            {
                continue;
            }

            List<EncounterCreature> creatures = encounter.Creatures
                .Select(creature => idChanges.TryGetValue(creature.StatBlockId, out Guid newId)
                    ? creature with { StatBlockId = newId }
                    : creature)
                .ToList();

            await _encounters.SaveAsync(encounter with { Creatures = creatures }, cancellationToken).ConfigureAwait(false);
        }
    }
}