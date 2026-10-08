using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Infrastructure.Storage;

namespace DnDInitiative.Infrastructure.Parties;

/// <summary>
/// Stores all parties in a single JSON file.
/// </summary>
public sealed class JsonPartyRepository : IPartyRepository
{
    private readonly JsonFileStore<Party> _store;

    public JsonPartyRepository(string filePath)
    {
        _store = new JsonFileStore<Party>(filePath);
    }

    public async Task<IReadOnlyList<Party>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _store.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task SaveAsync(Party party, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(party);
        return _store.UpsertAsync(party, static item => item.Id, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _store.RemoveAsync(id, static item => item.Id, cancellationToken);
    }
}