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

    public async Task SaveAsync(StatBlock statBlock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statBlock);

        List<StatBlock> statBlocks = await _store.ReadAsync(cancellationToken).ConfigureAwait(false);

        int index = statBlocks.FindIndex(existing => existing.Id == statBlock.Id);
        if (index >= 0)
        {
            statBlocks[index] = statBlock;
        }
        else
        {
            statBlocks.Add(statBlock);
        }

        await _store.WriteAsync(statBlocks, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        List<StatBlock> statBlocks = await _store.ReadAsync(cancellationToken).ConfigureAwait(false);

        if (statBlocks.RemoveAll(statBlock => statBlock.Id == id) > 0)
        {
            await _store.WriteAsync(statBlocks, cancellationToken).ConfigureAwait(false);
        }
    }
}