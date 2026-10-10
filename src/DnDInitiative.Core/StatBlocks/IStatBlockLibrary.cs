namespace DnDInitiative.Core.StatBlocks;

/// <summary>
/// The stat blocks brought in by imports, without anything the user made themselves.
/// Only importing needs this narrow view; everything else uses <see cref="IStatBlockRepository"/>.
/// </summary>
public interface IStatBlockLibrary
{
    Task<IReadOnlyList<StatBlock>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveManyAsync(IReadOnlyCollection<StatBlock> statBlocks, CancellationToken cancellationToken = default);

    /// <summary>Gives stored stat blocks new Ids. Does not touch anything that refers to them.</summary>
    /// <param name="changes">Current Id to new Id.</param>
    Task ChangeIdsAsync(IReadOnlyDictionary<Guid, Guid> changes, CancellationToken cancellationToken = default);
}