using DnDInitiative.Core.Parties;
using DnDInitiative.Infrastructure.Campaigns;

using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Parties;

internal sealed class JsonPartyRepository : IPartyRepository
{
    private readonly JsonFolderStore<Party> _store;

    public JsonPartyRepository(CampaignLocation location, ILogger<JsonPartyRepository> logger)
    {
        _store = new JsonFolderStore<Party>(location, "parties", party => party.Id, party => party.Name, logger);
    }

    public async Task<IReadOnlyList<Party>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Party> parties = await _store.LoadAllAsync(cancellationToken).ConfigureAwait(false);
        return parties.OrderBy(party => party.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Task SaveAsync(Party party, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(party);
        return _store.SaveAsync(party, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _store.DeleteAsync(id, cancellationToken);
    }
}