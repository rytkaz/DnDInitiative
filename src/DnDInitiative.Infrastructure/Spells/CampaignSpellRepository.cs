using DnDInitiative.Core.Spells;
using DnDInitiative.Infrastructure.Campaigns;

using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Spells;

/// <summary>
/// One library made of two stores: the spells the user wrote or changed, kept as files in the
/// campaign folder, and the imported ones in the database. Where both hold the same key, the
/// user's wins, and a re-import never touches it.
/// </summary>
internal sealed class CampaignSpellRepository : ISpellRepository, ICustomSpellRepository
{
    private readonly JsonFolderStore<Spell> _ownStore;
    private readonly SqliteSpellRepository _library;

    public CampaignSpellRepository(
        CampaignLocation location,
        SqliteSpellRepository library,
        ILogger<CampaignSpellRepository> logger)
    {
        _ownStore = new JsonFolderStore<Spell>(
            location,
            "spells",
            spell => KeyIdentity.For(spell.Key),
            spell => spell.Key,
            logger);
        _library = library;
    }

    public async Task<Spell?> FindAsync(string reference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);

        string text = reference.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        IReadOnlyList<Spell> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);

        Spell? found = own.FirstOrDefault(spell => string.Equals(spell.Key, text, StringComparison.OrdinalIgnoreCase))
            ?? own.OrderBy(spell => spell.Key, StringComparer.Ordinal)
                .FirstOrDefault(spell => string.Equals(spell.Name, text, StringComparison.OrdinalIgnoreCase));

        if (found is not null)
        {
            return found;
        }

        Spell? imported = await _library.FindAsync(reference, cancellationToken).ConfigureAwait(false);
        if (imported is null)
        {
            return null;
        }

        // Found by name, the imported spell may be one the user has changed under another name.
        return own.FirstOrDefault(spell => string.Equals(spell.Key, imported.Key, StringComparison.OrdinalIgnoreCase)) ?? imported;
    }

    public async Task<IReadOnlyList<SpellSummary>> SearchAsync(
        SpellQuery query,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        IReadOnlyList<Spell> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);
        HashSet<string> ownKeys = own.Select(spell => spell.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        IEnumerable<SpellSummary> ownMatches = own.Where(spell => Matches(spell, query)).Select(ToSummary);

        // The page asked for is rows [skip, skip + take) of the two lists merged. Those rows can
        // only come from the first (skip + take) rows of the database, plus one extra for every
        // custom spell that might be covering a database row.
        IReadOnlyList<SpellSummary> libraryMatches = await _library
            .SearchAsync(query, skip: 0, take: skip + take + own.Count, cancellationToken)
            .ConfigureAwait(false);

        return ownMatches
            .Concat(libraryMatches.Where(summary => !ownKeys.Contains(summary.Key)))
            .OrderBy(summary => summary.LevelNumber)
            .ThenBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.Key, StringComparer.Ordinal)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Spell> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> imported = await _library.GetClassesAsync(cancellationToken).ConfigureAwait(false);

        return imported
            .Concat(own.SelectMany(spell => spell.Classes))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Imports go to the database. The user's own spells are written through <see cref="SaveAsync"/>.</summary>
    public Task<SpellSaveResult> SaveManyAsync(IReadOnlyCollection<Spell> spells, CancellationToken cancellationToken = default)
    {
        return _library.SaveManyAsync(spells, cancellationToken);
    }

    public Task SaveAsync(Spell spell, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spell);

        return _ownStore.SaveAsync(spell with { Key = spell.Key.Trim().ToLowerInvariant(), IsCustom = true }, cancellationToken);
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

    public async Task<Spell?> FindCustomAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        IReadOnlyList<Spell> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);
        return own.FirstOrDefault(spell => string.Equals(spell.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool Matches(Spell spell, SpellQuery query)
    {
        string text = query.Text.Trim();

        return (text.Length == 0 || spell.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            && (query.Class is null || spell.Classes.Contains(query.Class, StringComparer.OrdinalIgnoreCase))
            && (query.Level is null || spell.LevelNumber == query.Level)
            && (query.IsRitual is null || spell.IsRitual == query.IsRitual)
            && (query.IsConcentration is null || spell.IsConcentration == query.IsConcentration)
            && (query.Source is null || string.Equals(spell.Source, query.Source, StringComparison.Ordinal));
    }

    private static SpellSummary ToSummary(Spell spell)
    {
        return new SpellSummary(
            spell.Key,
            spell.Name,
            spell.LevelNumber,
            spell.School,
            spell.IsRitual,
            spell.IsConcentration,
            spell.Classes,
            spell.Source,
            spell.CastingTime,
            spell.Range,
            IsCustom: true);
    }

    private async Task<IReadOnlyList<Spell>> LoadOwnAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Spell> own = await _ownStore.LoadAllAsync(cancellationToken).ConfigureAwait(false);

        // Whatever a file says about itself, being in the campaign folder makes it the user's.
        return own.Select(spell => spell with { Key = spell.Key.ToLowerInvariant(), IsCustom = true }).ToList();
    }
}
