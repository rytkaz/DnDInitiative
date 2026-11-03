using DnDInitiative.Core.Campaigns;
using DnDInitiative.Infrastructure.Campaigns;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Persistence;

/// <summary>
/// The SQLite database of imported stat blocks, spells, conditions and abilities. Each campaign
/// keeps its own in its folder, so a campaign carries its library with it. Contexts are always
/// created for the campaign that is open now.
/// </summary>
internal sealed class CampaignDatabase : IDbContextFactory<AppDbContext>
{
    public const string FileName = "library.db";

    // Where the database lived before it moved into the campaign folders, and what it is
    // renamed to once a campaign has taken it over.
    private const string LegacyFileName = "dndinitiative.db";
    private const string AdoptedLegacyFileName = "dndinitiative.moved-to-campaign.db";

    private readonly CampaignLocation _location;
    private readonly StorageOptions _storage;
    private readonly ILogger<CampaignDatabase> _logger;

    public CampaignDatabase(CampaignLocation location, StorageOptions storage, ILogger<CampaignDatabase> logger)
    {
        _location = location;
        _storage = storage;
        _logger = logger;
    }

    public static string PathIn(string campaignFolder)
    {
        return Path.Combine(campaignFolder, FileName);
    }

    public AppDbContext CreateDbContext()
    {
        return CreateDbContext(PathIn(_location.Folder));
    }

    /// <summary>
    /// Gets a campaign's database ready before the campaign is opened: takes over the old
    /// shared database if this is the first campaign to need one, and applies migrations.
    /// </summary>
    /// <exception cref="CampaignException">The database could not be created or upgraded.</exception>
    public async Task PrepareAsync(string campaignFolder, CancellationToken cancellationToken)
    {
        string path = PathIn(campaignFolder);

        try
        {
            if (!File.Exists(path))
            {
                await AdoptLegacyDatabaseAsync(path, cancellationToken).ConfigureAwait(false);
            }

            AppDbContext db = CreateDbContext(path);
            await using (db.ConfigureAwait(false))
            {
                await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (SqliteException exception)
        {
            throw new CampaignException($"The library database in '{campaignFolder}' could not be opened: {exception.Message}", exception);
        }
    }

    /// <summary>Copies one campaign's database into another campaign folder, which must not have one yet.</summary>
    public static async Task CopyAsync(string fromCampaignFolder, string toCampaignFolder, CancellationToken cancellationToken)
    {
        string from = PathIn(fromCampaignFolder);
        if (File.Exists(from))
        {
            await CopyDatabaseAsync(from, PathIn(toCampaignFolder), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task AdoptLegacyDatabaseAsync(string path, CancellationToken cancellationToken)
    {
        string legacyPath = Path.Combine(_storage.DataDirectory, LegacyFileName);
        if (!File.Exists(legacyPath))
        {
            return;
        }

        // Only the first campaign gets the old library; later new campaigns start empty, as they
        // would on a new computer. The old file is renamed rather than deleted, so nothing is lost.
        await CopyDatabaseAsync(legacyPath, path, cancellationToken).ConfigureAwait(false);
        File.Move(legacyPath, Path.Combine(_storage.DataDirectory, AdoptedLegacyFileName), overwrite: true);

        CampaignLog.LegacyDatabaseAdopted(_logger, legacyPath, path);
    }

    private static async Task CopyDatabaseAsync(string from, string to, CancellationToken cancellationToken)
    {
        AppDbContext db = CreateDbContext(from);
        await using (db.ConfigureAwait(false))
        {
            // Asks SQLite itself to write a complete copy. Copying the file by hand could miss
            // changes that are still in SQLite's separate write-ahead log.
            await db.Database.ExecuteSqlAsync($"VACUUM INTO {to}", cancellationToken).ConfigureAwait(false);
        }
    }

    private static AppDbContext CreateDbContext(string path)
    {
        // Without pooling, nothing keeps a database file open once its context is disposed, so
        // a campaign that has been switched away from can be moved or deleted while the app runs.
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = false,
        }.ToString();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
