using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Storage;

namespace DnDInitiative.Infrastructure.StatBlocks;

/// <summary>
/// Stores all stat blocks in a single JSON file.
/// </summary>
public sealed class JsonStatBlockRepository : IStatBlockRepository
{
    private readonly JsonFileStore<StatBlock> _store;

    public JsonStatBlockRepository(string filePath)
    {
        _store = new JsonFileStore<StatBlock>(filePath);
    }

    public async Task<IReadOnlyList<StatBlock>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _store.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task SaveAsync(StatBlock statBlock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statBlock);
        return _store.UpsertAsync(statBlock, static item => item.Id, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _store.RemoveAsync(id, static item => item.Id, cancellationToken);
    }

    public async Task SaveManyAsync(IReadOnlyCollection<StatBlock> statBlocks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statBlocks);

        List<StatBlock> stored = await _store.ReadAsync(cancellationToken).ConfigureAwait(false);

        var indexById = new Dictionary<Guid, int>();
        for (int i = 0; i < stored.Count; i++)
        {
            indexById[stored[i].Id] = i;
        }

        foreach (StatBlock statBlock in statBlocks)
        {
            if (indexById.TryGetValue(statBlock.Id, out int index))
            {
                stored[index] = statBlock;
            }
            else
            {
                indexById[statBlock.Id] = stored.Count;
                stored.Add(statBlock);
            }
        }

        await _store.WriteAsync(stored, cancellationToken).ConfigureAwait(false);
    }
}