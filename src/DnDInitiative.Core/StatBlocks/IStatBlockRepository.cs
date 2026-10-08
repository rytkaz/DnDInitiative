namespace DnDInitiative.Core.StatBlocks;

public interface IStatBlockRepository
{
    Task<IReadOnlyList<StatBlock>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds the stat block, or replaces the stored one with the same Id.</summary>
    Task SaveAsync(StatBlock statBlock, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds or replaces many stat blocks in one write.</summary>
    Task SaveManyAsync(IReadOnlyCollection<StatBlock> statBlocks, CancellationToken cancellationToken = default);
}