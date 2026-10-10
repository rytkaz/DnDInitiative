using DnDInitiative.Core.Encounters;
using DnDInitiative.Infrastructure.Campaigns;

using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Encounters;

internal sealed class JsonEncounterRepository : IEncounterRepository
{
    private readonly JsonFolderStore<Encounter> _store;

    public JsonEncounterRepository(CampaignLocation location, ILogger<JsonEncounterRepository> logger)
    {
        _store = new JsonFolderStore<Encounter>(
            location,
            "encounters",
            encounter => encounter.Id,
            encounter => encounter.Name,
            logger);
    }

    public async Task<IReadOnlyList<Encounter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Encounter> encounters = await _store.LoadAllAsync(cancellationToken).ConfigureAwait(false);
        return encounters.OrderBy(encounter => encounter.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<Encounter?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Encounter> encounters = await _store.LoadAllAsync(cancellationToken).ConfigureAwait(false);
        return encounters.FirstOrDefault(encounter => encounter.Id == id);
    }

    public Task SaveAsync(Encounter encounter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        return _store.SaveAsync(encounter, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _store.DeleteAsync(id, cancellationToken);
    }
}