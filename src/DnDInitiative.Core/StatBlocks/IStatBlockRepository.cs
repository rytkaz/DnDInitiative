namespace DnDInitiative.Core.StatBlocks;

public interface IStatBlockRepository
{
    Task<IReadOnlyList<StatBlock>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds the stat block, or replaces the stored one with the same Id.</summary>
    Task SaveAsync(StatBlock statBlock, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds or replaces many stat blocks in one write.</summary>
    Task SaveManyAsync(IReadOnlyCollection<StatBlock> statBlocks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds stat blocks whose name contains the text, ignoring case, sorted by name.
    /// An empty text matches everything, and the word "custom" also matches every stat block
    /// the user made, whatever its name. Skips the first <paramref name="skip"/> matches
    /// and returns at most <paramref name="limit"/>.
    /// </summary>
    Task<IReadOnlyList<StatBlockSummary>> SearchAsync(
        string searchText,
        int limit,
        int skip = 0,
        CancellationToken cancellationToken = default);

    Task<StatBlock?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the stat blocks with the given Ids. Ids that do not exist are skipped.</summary>
    Task<IReadOnlyList<StatBlock>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}