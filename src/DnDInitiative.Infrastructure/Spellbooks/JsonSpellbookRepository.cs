using DnDInitiative.Core.Spellbooks;
using DnDInitiative.Infrastructure.Campaigns;

using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Spellbooks;

internal sealed class JsonSpellbookRepository : ISpellbookRepository
{
    private readonly JsonFolderStore<Spellbook> _store;

    public JsonSpellbookRepository(CampaignLocation location, ILogger<JsonSpellbookRepository> logger)
    {
        _store = new JsonFolderStore<Spellbook>(
            location,
            "spellbooks",
            spellbook => spellbook.Id,
            spellbook => spellbook.Name,
            logger);
    }

    public async Task<IReadOnlyList<Spellbook>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Spellbook> spellbooks = await _store.LoadAllAsync(cancellationToken).ConfigureAwait(false);
        return spellbooks.OrderBy(spellbook => spellbook.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Task SaveAsync(Spellbook spellbook, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spellbook);
        return _store.SaveAsync(spellbook, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _store.DeleteAsync(id, cancellationToken);
    }
}