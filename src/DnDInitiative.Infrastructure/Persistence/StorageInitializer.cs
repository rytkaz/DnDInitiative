using DnDInitiative.Core.Campaigns;

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
    private readonly ICampaignService _campaigns;
    private readonly LegacyDatabaseMover _legacyMover;

    public StorageInitializer(
        StorageOptions options,
        ICampaignService campaigns,
        LegacyDatabaseMover legacyMover)
    {
        _options = options;
        _campaigns = campaigns;
        _legacyMover = legacyMover;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // The settings and the converter live here, not in the campaign.
        Directory.CreateDirectory(_options.DataDirectory);

        // A campaign must be open before anything asks for parties, encounters or stat blocks.
        // Opening it also migrates its database.
        await _campaigns.OpenLastOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        await _legacyMover.MoveAsync(cancellationToken).ConfigureAwait(false);
    }
}
