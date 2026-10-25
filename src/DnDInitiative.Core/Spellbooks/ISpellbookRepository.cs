namespace DnDInitiative.Core.Spellbooks;

public interface ISpellbookRepository
{
    Task<IReadOnlyList<Spellbook>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds the spellbook, or replaces the stored one with the same Id.</summary>
    Task SaveAsync(Spellbook spellbook, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}