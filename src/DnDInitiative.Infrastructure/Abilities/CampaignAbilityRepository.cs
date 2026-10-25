using DnDInitiative.Core.Abilities;
using DnDInitiative.Infrastructure.Campaigns;

using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Abilities;

/// <summary>
/// One library made of two stores: the abilities the user wrote or changed, kept as files in
/// the campaign folder, and the imported ones in the database. Where both hold the same key,
/// the user's wins, and a re-import never touches it.
/// </summary>
internal sealed class CampaignAbilityRepository : IAbilityRepository, ICustomAbilityRepository
{
    private readonly JsonFolderStore<Ability> _ownStore;
    private readonly SqliteAbilityRepository _library;

    public CampaignAbilityRepository(
        CampaignLocation location,
        SqliteAbilityRepository library,
        ILogger<CampaignAbilityRepository> logger)
    {
        _ownStore = new JsonFolderStore<Ability>(
            location,
            "abilities",
            ability => KeyIdentity.For(ability.Key),
            ability => ability.Key,
            logger);
        _library = library;
    }

    public async Task<Ability?> FindAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        return await FindCustomAsync(key, cancellationToken).ConfigureAwait(false)
            ?? await _library.FindAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AbilitySummary>> SearchAsync(
        AbilityQuery query,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        IReadOnlyList<Ability> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);
        HashSet<string> ownKeys = own.Select(ability => ability.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        IEnumerable<AbilitySummary> ownMatches = own.Where(ability => Matches(ability, query)).Select(ToSummary);

        // See CampaignSpellRepository.SearchAsync for why this many database rows are enough.
        IReadOnlyList<AbilitySummary> libraryMatches = await _library
            .SearchAsync(query, skip: 0, take: skip + take + own.Count, cancellationToken)
            .ConfigureAwait(false);

        return ownMatches
            .Concat(libraryMatches.Where(summary => !ownKeys.Contains(summary.Key)))
            .OrderBy(summary => summary.Degree)
            .ThenBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.Key, StringComparer.Ordinal)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Ability> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> imported = await _library.GetClassesAsync(cancellationToken).ConfigureAwait(false);

        return imported
            .Concat(own.SelectMany(ability => ability.Classes))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Imports go to the database. The user's own abilities are written through <see cref="SaveAsync"/>.</summary>
    public Task<AbilitySaveResult> SaveManyAsync(IReadOnlyCollection<Ability> abilities, CancellationToken cancellationToken = default)
    {
        return _library.SaveManyAsync(abilities, cancellationToken);
    }

    public Task SaveAsync(Ability ability, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ability);

        return _ownStore.SaveAsync(ability with { Key = ability.Key.Trim().ToLowerInvariant(), IsCustom = true }, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (await FindCustomAsync(key, cancellationToken).ConfigureAwait(false) is null)
        {
            return false;
        }

        await _ownStore.DeleteAsync(KeyIdentity.For(key), cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task<bool> ExistsInLibraryAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _library.ExistsAsync(key, cancellationToken);
    }

    public async Task<Ability?> FindCustomAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        IReadOnlyList<Ability> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);
        return own.FirstOrDefault(ability => string.Equals(ability.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool Matches(Ability ability, AbilityQuery query)
    {
        string text = query.Text.Trim();

        return (text.Length == 0 || ability.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            && (query.Category is null || string.Equals(ability.Category, query.Category, StringComparison.Ordinal))
            && (query.Class is null || ability.Classes.Contains(query.Class, StringComparer.OrdinalIgnoreCase))
            && (query.Degree is null || ability.Degree == query.Degree)
            && (query.Source is null || string.Equals(ability.Source, query.Source, StringComparison.Ordinal));
    }

    private static AbilitySummary ToSummary(Ability ability)
    {
        return new AbilitySummary(
            ability.Key,
            ability.Name,
            ability.Category,
            ability.Degree,
            ability.Prerequisites,
            ability.Classes,
            ability.Source,
            IsCustom: true);
    }

    private async Task<IReadOnlyList<Ability>> LoadOwnAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Ability> own = await _ownStore.LoadAllAsync(cancellationToken).ConfigureAwait(false);

        // Whatever a file says about itself, being in the campaign folder makes it the user's.
        return own.Select(ability => ability with { Key = ability.Key.ToLowerInvariant(), IsCustom = true }).ToList();
    }
}
