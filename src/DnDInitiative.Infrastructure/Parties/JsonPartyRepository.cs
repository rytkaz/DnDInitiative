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

    public async Task SaveAsync(Party party, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(party);

        List<Party> parties = await _store.ReadAsync(cancellationToken).ConfigureAwait(false);

        int index = parties.FindIndex(existing => existing.Id == party.Id);
        if (index >= 0)
        {
            parties[index] = party;
        }
        else
        {
            parties.Add(party);
        }

        await _store.WriteAsync(parties, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        List<Party> parties = await _store.ReadAsync(cancellationToken).ConfigureAwait(false);

        if (parties.RemoveAll(party => party.Id == id) > 0)
        {
            await _store.WriteAsync(parties, cancellationToken).ConfigureAwait(false);
        }
    }
}