using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemoryStatBlockRepository : IStatBlockRepository
{
    private readonly List<StatBlock> _statBlocks = [];

    public InMemoryStatBlockRepository(params StatBlock[] statBlocks)
    {
        _statBlocks.AddRange(statBlocks);
    }

    public IReadOnlyList<StatBlock> StatBlocks => _statBlocks;

    public Task<IReadOnlyList<StatBlock>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<StatBlock>>(_statBlocks.ToList());
    }

    public Task SaveAsync(StatBlock statBlock, CancellationToken cancellationToken = default)
    {
        _statBlocks.RemoveAll(existing => existing.Id == statBlock.Id);
        _statBlocks.Add(statBlock);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _statBlocks.RemoveAll(existing => existing.Id == id);
        return Task.CompletedTask;
    }
}