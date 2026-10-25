namespace DnDInitiative.Core.Abilities;

/// <summary>
/// The abilities the user wrote or changed, kept in the campaign folder. A custom ability with
/// the key of an imported one takes its place everywhere, until it is deleted.
/// </summary>
public interface ICustomAbilityRepository
{
    /// <summary>Adds the ability, or replaces the custom ability with the same key.</summary>
    Task SaveAsync(Ability ability, CancellationToken cancellationToken = default);

    /// <summary>Deletes the custom ability, which uncovers the imported one under it if there is one.</summary>
    /// <returns>False if there was no custom ability with that key.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Whether the imported library has an ability with the key, whatever the campaign holds.</summary>
    Task<bool> ExistsInLibraryAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>The ability as the user last saved it, or null if the campaign has no custom ability with the key.</summary>
    Task<Ability?> FindCustomAsync(string key, CancellationToken cancellationToken = default);
}
