namespace DnDInitiative.Core.Spells;

/// <summary>
/// The spells the user wrote or changed, kept in the campaign folder. A custom spell with the
/// key of an imported one takes its place everywhere, until it is deleted.
/// </summary>
public interface ICustomSpellRepository
{
    /// <summary>Adds the spell, or replaces the custom spell with the same key.</summary>
    Task SaveAsync(Spell spell, CancellationToken cancellationToken = default);

    /// <summary>Deletes the custom spell, which uncovers the imported one under it if there is one.</summary>
    /// <returns>False if there was no custom spell with that key.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Whether the imported library has a spell with the key, whatever the campaign holds.</summary>
    Task<bool> ExistsInLibraryAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>The spell as the user last saved it, or null if the campaign has no custom spell with the key.</summary>
    Task<Spell?> FindCustomAsync(string key, CancellationToken cancellationToken = default);
}
