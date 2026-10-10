using DnDInitiative.Core.Campaigns;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Campaigns;
using DnDInitiative.Infrastructure.Encounters;
using DnDInitiative.Infrastructure.Parties;
using DnDInitiative.Infrastructure.StatBlocks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Persistence;

/// <summary>
/// Moves what used to be stored in the database into the open campaign folder.
/// Safe to run on every start: once everything has been moved it finds nothing to do.
/// </summary>
internal sealed class LegacyDatabaseMover
{
    private const string BackupFileName = "dndinitiative.before-campaigns.db";

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly StorageOptions _storage;
    private readonly ICampaignService _campaigns;
    private readonly SqlitePartyRepository _oldParties;
    private readonly SqliteEncounterRepository _oldEncounters;
    private readonly SqliteStatBlockRepository _library;
    private readonly JsonPartyRepository _parties;
    private readonly JsonEncounterRepository _encounters;
    private readonly CampaignStatBlockRepository _statBlocks;
    private readonly ILogger<LegacyDatabaseMover> _logger;

    public LegacyDatabaseMover(
        IDbContextFactory<AppDbContext> contextFactory,
        StorageOptions storage,
        ICampaignService campaigns,
        SqlitePartyRepository oldParties,
        SqliteEncounterRepository oldEncounters,
        SqliteStatBlockRepository library,
        JsonPartyRepository parties,
        JsonEncounterRepository encounters,
        CampaignStatBlockRepository statBlocks,
        ILogger<LegacyDatabaseMover> logger)
    {
        _contextFactory = contextFactory;
        _storage = storage;
        _campaigns = campaigns;
        _oldParties = oldParties;
        _oldEncounters = oldEncounters;
        _library = library;
        _parties = parties;
        _encounters = encounters;
        _statBlocks = statBlocks;
        _logger = logger;
    }

    public async Task MoveAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Party> parties = await _oldParties.GetAllAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Encounter> encounters = await _oldEncounters.GetAllAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<StatBlock> ownStatBlocks = await FindOwnStatBlocksAsync(cancellationToken).ConfigureAwait(false);

        if (parties.Count == 0 && encounters.Count == 0 && ownStatBlocks.Count == 0)
        {
            return;
        }

        await BackUpDatabaseAsync(cancellationToken).ConfigureAwait(false);

        // Everything is written to the folder before anything is removed from the database.
        // If this stops halfway, the next start simply does it again.
        foreach (Party party in parties)
        {
            await _parties.SaveAsync(party, cancellationToken).ConfigureAwait(false);
        }

        foreach (Encounter encounter in encounters)
        {
            await _encounters.SaveAsync(encounter, cancellationToken).ConfigureAwait(false);
        }

        foreach (StatBlock statBlock in ownStatBlocks)
        {
            await _statBlocks.SaveAsync(statBlock, cancellationToken).ConfigureAwait(false);
        }

        foreach (Party party in parties)
        {
            await _oldParties.DeleteAsync(party.Id, cancellationToken).ConfigureAwait(false);
        }

        foreach (Encounter encounter in encounters)
        {
            await _oldEncounters.DeleteAsync(encounter.Id, cancellationToken).ConfigureAwait(false);
        }

        foreach (StatBlock statBlock in ownStatBlocks)
        {
            await _library.DeleteAsync(statBlock.Id, cancellationToken).ConfigureAwait(false);
        }

        CampaignLog.LegacyDataMoved(
            _logger,
            parties.Count,
            encounters.Count,
            ownStatBlocks.Count,
            _campaigns.Current?.Folder ?? "");
    }

    private async Task<IReadOnlyList<StatBlock>> FindOwnStatBlocksAsync(CancellationToken cancellationToken)
    {
        // Until an import has marked the book creatures as Imported, everything in the database
        // looks like the user's own. Moving it all would flood the campaign folder, so wait.
        bool librarySorted = await _library
            .AnyWithOriginAsync(StatBlockOrigin.Imported, cancellationToken)
            .ConfigureAwait(false);

        return librarySorted
            ? await _library.GetByOriginAsync(StatBlockOrigin.Custom, cancellationToken).ConfigureAwait(false)
            : [];
    }

    private async Task BackUpDatabaseAsync(CancellationToken cancellationToken)
    {
        string backupPath = Path.Combine(_storage.DataDirectory, BackupFileName);
        if (File.Exists(backupPath))
        {
            return;
        }

        AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (db.ConfigureAwait(false))
        {
            // Asks SQLite itself to write a complete copy. Copying the file by hand could miss
            // changes that are still in SQLite's separate write-ahead log.
            await db.Database.ExecuteSqlAsync($"VACUUM INTO {backupPath}", cancellationToken).ConfigureAwait(false);
        }
    }
}