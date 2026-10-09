using System.Text.Json;

using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Infrastructure.Persistence;

/// <summary>
/// One-time move of data from the JSON files the app used before it had a database.
/// </summary>
internal sealed class LegacyJsonImporter
{
    private readonly StorageOptions _options;
    private readonly IPartyRepository _parties;
    private readonly IStatBlockRepository _statBlocks;
    private readonly IEncounterRepository _encounters;

    public LegacyJsonImporter(
        StorageOptions options,
        IPartyRepository parties,
        IStatBlockRepository statBlocks,
        IEncounterRepository encounters)
    {
        _options = options;
        _parties = parties;
        _statBlocks = statBlocks;
        _encounters = encounters;
    }

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        await ImportFileAsync<Party>(
            "parties.json",
            async (parties, token) =>
            {
                foreach (Party party in parties)
                {
                    await _parties.SaveAsync(party, token).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);

        await ImportFileAsync<StatBlock>(
            "statblocks.json",
            (statBlocks, token) => _statBlocks.SaveManyAsync(statBlocks, token),
            cancellationToken).ConfigureAwait(false);

        await ImportFileAsync<Encounter>(
            "encounters.json",
            async (encounters, token) =>
            {
                foreach (Encounter encounter in encounters)
                {
                    await _encounters.SaveAsync(encounter, token).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task ImportFileAsync<T>(
        string fileName,
        Func<List<T>, CancellationToken, Task> save,
        CancellationToken cancellationToken)
    {
        string path = Path.Combine(_options.DataDirectory, fileName);
        if (!File.Exists(path))
        {
            return;
        }

        List<T> items;
        try
        {
            string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            items = JsonSerializer.Deserialize<List<T>>(json) ?? [];
        }
        catch (JsonException)
        {
            // Keep the file for inspection, but stop trying to read it on every start.
            File.Move(path, path + ".unreadable", overwrite: true);
            return;
        }

        await save(items, cancellationToken).ConfigureAwait(false);

        // Renamed, not deleted: it marks the import as done and leaves a backup.
        File.Move(path, path + ".imported", overwrite: true);
    }
}