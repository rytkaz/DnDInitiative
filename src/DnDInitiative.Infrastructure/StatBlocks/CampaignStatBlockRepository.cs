using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Campaigns;

using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.StatBlocks;

/// <summary>
/// One library made of two stores: the user's own stat blocks, kept as files in the campaign
/// folder, and the imported ones in the database. Where both hold the same Id, the user's wins.
/// </summary>
internal sealed class CampaignStatBlockRepository : IStatBlockRepository
{
    /// <summary>Typed into a search box, this word finds every stat block the user made. It is the tag shown beside them.</summary>
    private const string OwnKeyword = "custom";

    private readonly JsonFolderStore<StatBlock> _ownStore;
    private readonly SqliteStatBlockRepository _library;

    public CampaignStatBlockRepository(
        CampaignLocation location,
        SqliteStatBlockRepository library,
        ILogger<CampaignStatBlockRepository> logger)
    {
        _ownStore = new JsonFolderStore<StatBlock>(
            location,
            "statblocks",
            statBlock => statBlock.Id,
            statBlock => statBlock.Name,
            logger);
        _library = library;
    }

    public async Task<IReadOnlyList<StatBlock>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StatBlock> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<StatBlock> imported = await _library.GetAllAsync(cancellationToken).ConfigureAwait(false);

        HashSet<Guid> ownIds = own.Select(statBlock => statBlock.Id).ToHashSet();

        return own
            .Concat(imported.Where(statBlock => !ownIds.Contains(statBlock.Id)))
            .OrderBy(statBlock => statBlock.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(statBlock => statBlock.Id.ToString(), StringComparer.Ordinal)
            .ToList();
    }

    public async Task<StatBlock?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StatBlock> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);

        return own.FirstOrDefault(statBlock => statBlock.Id == id)
            ?? await _library.GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StatBlock>> GetManyAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        IReadOnlyList<StatBlock> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);
        List<StatBlock> found = own.Where(statBlock => ids.Contains(statBlock.Id)).ToList();

        // Only ask the database for the ones the campaign folder did not have.
        HashSet<Guid> foundIds = found.Select(statBlock => statBlock.Id).ToHashSet();
        List<Guid> remaining = ids.Where(id => !foundIds.Contains(id)).ToList();
        found.AddRange(await _library.GetManyAsync(remaining, cancellationToken).ConfigureAwait(false));

        return found;
    }

    public Task SaveAsync(StatBlock statBlock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statBlock);

        return statBlock.Origin == StatBlockOrigin.Custom
            ? _ownStore.SaveAsync(statBlock, cancellationToken)
            : _library.SaveAsync(statBlock, cancellationToken);
    }

    public async Task SaveManyAsync(IReadOnlyCollection<StatBlock> statBlocks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statBlocks);

        foreach (StatBlock own in statBlocks.Where(statBlock => statBlock.Origin == StatBlockOrigin.Custom))
        {
            await _ownStore.SaveAsync(own, cancellationToken).ConfigureAwait(false);
        }

        List<StatBlock> imported = statBlocks.Where(statBlock => statBlock.Origin == StatBlockOrigin.Imported).ToList();
        if (imported.Count > 0)
        {
            await _library.SaveManyAsync(imported, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes the user's version if there is one, which uncovers any imported stat block
    /// underneath. Otherwise deletes the imported one.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StatBlock> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);

        if (own.Any(statBlock => statBlock.Id == id))
        {
            await _ownStore.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _library.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<StatBlockSummary>> SearchAsync(
        string searchText,
        int limit,
        int skip = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchText);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);

        IReadOnlyList<StatBlock> own = await LoadOwnAsync(cancellationToken).ConfigureAwait(false);
        HashSet<Guid> ownIds = own.Select(statBlock => statBlock.Id).ToHashSet();

        string trimmed = searchText.Trim();

        // Only the whole word counts, so typing "cu" on the way to "Cultist" does not flash up
        // every custom stat block. Imported creatures with "custom" in their name are still
        // found the usual way, by the database search below.
        bool wantsAllOwn = trimmed.Equals(OwnKeyword, StringComparison.OrdinalIgnoreCase);

        IEnumerable<StatBlockSummary> ownMatches = own
            .Where(statBlock => wantsAllOwn
                || trimmed.Length == 0
                || statBlock.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .Select(statBlock => new StatBlockSummary(statBlock.Id, statBlock.Name, statBlock.ChallengeRating, statBlock.Source, StatBlockOrigin.Custom));

        // The page asked for is rows [skip, skip + limit) of the two lists merged. Those rows can
        // only come from the first (skip + limit) rows of the database, plus one extra for every
        // own stat block that might be covering a database row.
        int libraryRows = skip + limit + own.Count;
        IReadOnlyList<StatBlockSummary> libraryMatches = await _library
            .SearchAsync(searchText, libraryRows, skip: 0, cancellationToken)
            .ConfigureAwait(false);

        return ownMatches
            .Concat(libraryMatches.Where(summary => !ownIds.Contains(summary.Id)))
            .OrderBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.Id.ToString(), StringComparer.Ordinal)
            .Skip(skip)
            .Take(limit)
            .ToList();
    }

    private async Task<IReadOnlyList<StatBlock>> LoadOwnAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<StatBlock> own = await _ownStore.LoadAllAsync(cancellationToken).ConfigureAwait(false);

        // Whatever a file says about itself, being in the campaign folder makes it the user's.
        return own.Select(statBlock => statBlock with { Origin = StatBlockOrigin.Custom }).ToList();
    }
}