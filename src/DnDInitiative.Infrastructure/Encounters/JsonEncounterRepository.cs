using DnDInitiative.Core.Encounters;
using DnDInitiative.Infrastructure.Storage;

namespace DnDInitiative.Infrastructure.Encounters;

/// <summary>
/// Stores all encounters in a single JSON file.
/// </summary>
public sealed class JsonEncounterRepository : IEncounterRepository
{
    private readonly JsonFileStore<Encounter> _store;

    public JsonEncounterRepository(string filePath)
    {
        _store = new JsonFileStore<Encounter>(filePath);
    }

    public async Task<IReadOnlyList<Encounter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _store.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Encounter?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        List<Encounter> encounters = await _store.ReadAsync(cancellationToken).ConfigureAwait(false);
        return encounters.Find(encounter => encounter.Id == id);
    }

    public Task SaveAsync(Encounter encounter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        return _store.UpsertAsync(encounter, static item => item.Id, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _store.RemoveAsync(id, static item => item.Id, cancellationToken);
    }
}