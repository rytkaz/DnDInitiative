using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Stores freshly imported stat blocks next to the ones already in the library, the same way
/// for every kind of import: a creature seen before is updated in place, one the user edited
/// is left alone, and encounters follow a stat block whose Id had to change.
/// </summary>
internal static class ImportedStatBlocks
{
    /// <param name="parsed">The stat blocks as read. Their Ids are replaced by the calculated ones.</param>
    /// <returns>How many stat blocks were added, and how many replaced a stored one.</returns>
    public static async Task<(int Added, int Updated)> SaveAsync(
        IStatBlockLibrary library,
        IEncounterRepository encounters,
        IEnumerable<StatBlock> parsed,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(encounters);
        ArgumentNullException.ThrowIfNull(parsed);

        Dictionary<string, StatBlock> existingByKey = await LoadExistingAsync(library, cancellationToken).ConfigureAwait(false);

        var toSave = new List<StatBlock>();
        var idChanges = new Dictionary<Guid, Guid>();
        int added = 0;
        int updated = 0;

        // A creature that was read twice is saved once; the last one read wins.
        var incomingByKey = new Dictionary<string, StatBlock>(StringComparer.Ordinal);
        foreach (StatBlock statBlock in parsed)
        {
            incomingByKey[StatBlockIdentity.Key(statBlock.Name, statBlock.Source)] = statBlock with
            {
                Id = StatBlockIdentity.ForImported(statBlock.Name, statBlock.Source),
                Origin = StatBlockOrigin.Imported,
            };
        }

        foreach ((string key, StatBlock incoming) in incomingByKey)
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
            await library.ChangeIdsAsync(idChanges, cancellationToken).ConfigureAwait(false);
            await RepointEncountersAsync(encounters, idChanges, cancellationToken).ConfigureAwait(false);
        }

        if (toSave.Count > 0)
        {
            await library.SaveManyAsync(toSave, cancellationToken).ConfigureAwait(false);
        }

        return (added, updated);
    }

    private static async Task<Dictionary<string, StatBlock>> LoadExistingAsync(IStatBlockLibrary library, CancellationToken cancellationToken)
    {
        IReadOnlyList<StatBlock> existing = await library.GetAllAsync(cancellationToken).ConfigureAwait(false);

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
    private static async Task RepointEncountersAsync(
        IEncounterRepository encounters,
        Dictionary<Guid, Guid> idChanges,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Encounter> stored = await encounters.GetAllAsync(cancellationToken).ConfigureAwait(false);

        foreach (Encounter encounter in stored)
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

            await encounters.SaveAsync(encounter with { Creatures = creatures }, cancellationToken).ConfigureAwait(false);
        }
    }
}