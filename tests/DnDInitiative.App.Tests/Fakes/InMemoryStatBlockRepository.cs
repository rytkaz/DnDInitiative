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

    public Task SaveManyAsync(IReadOnlyCollection<StatBlock> statBlocks, CancellationToken cancellationToken = default)
    {
        foreach (StatBlock statBlock in statBlocks)
        {
            _statBlocks.RemoveAll(existing => existing.Id == statBlock.Id);
            _statBlocks.Add(statBlock);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StatBlockSummary>> SearchAsync(
        string searchText,
        int limit,
        int skip = 0,
        CancellationToken cancellationToken = default)
    {
        string trimmed = searchText.Trim();
        List<StatBlockSummary> results = _statBlocks
            .Where(statBlock => trimmed.Length == 0 || statBlock.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .OrderBy(statBlock => statBlock.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(statBlock => statBlock.Id)
            .Skip(skip)
            .Take(limit)
            .Select(statBlock => new StatBlockSummary(statBlock.Id, statBlock.Name, statBlock.ChallengeRating, statBlock.Source))
            .ToList();

        return Task.FromResult<IReadOnlyList<StatBlockSummary>>(results);
    }

    public Task<StatBlock?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_statBlocks.Find(statBlock => statBlock.Id == id));
    }

    public Task<IReadOnlyList<StatBlock>> GetManyAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<StatBlock>>(_statBlocks.Where(statBlock => ids.Contains(statBlock.Id)).ToList());
    }
}