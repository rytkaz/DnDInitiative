using DnDInitiative.Core.Campaigns;

using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.Persistence;

internal sealed record StorageOptions(string DataDirectory);

/// <summary>
/// Gets storage ready before the app uses it. Call once at startup.
/// </summary>
public interface IStorageInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

internal sealed class StorageInitializer : IStorageInitializer
{
    private readonly StorageOptions _options;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICampaignService _campaigns;
    private readonly LegacyDatabaseMover _legacyMover;

    public StorageInitializer(
        StorageOptions options,
        IDbContextFactory<AppDbContext> contextFactory,
        ICampaignService campaigns,
        LegacyDatabaseMover legacyMover)
    {
        _options = options;
        _contextFactory = contextFactory;
        _campaigns = campaigns;
        _legacyMover = legacyMover;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // SQLite creates the database file, but not the folder it lives in.
        Directory.CreateDirectory(_options.DataDirectory);

        AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (db.ConfigureAwait(false))
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        // A campaign must be open before anything asks for parties, encounters or stat blocks.
        await _campaigns.OpenLastOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        await _legacyMover.MoveAsync(cancellationToken).ConfigureAwait(false);
    }
}