namespace DnDInitiative.Core.StatBlocks;

public interface IStatBlockRepository
{
    Task<IReadOnlyList<StatBlock>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds the stat block, or replaces the stored one with the same Id.</summary>
    Task SaveAsync(StatBlock statBlock, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}