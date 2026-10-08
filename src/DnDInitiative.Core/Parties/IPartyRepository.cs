namespace DnDInitiative.Core.Parties;

public interface IPartyRepository
{
    Task<IReadOnlyList<Party>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds the party, or replaces the stored party with the same Id.</summary>
    Task SaveAsync(Party party, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}