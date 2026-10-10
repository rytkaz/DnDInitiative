using System.Text.Json;

using DnDInitiative.Core.Campaigns;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Campaigns;

internal sealed class CampaignService : ICampaignService
{
    /// <summary>Raised whenever the layout of a campaign folder changes in a way older versions cannot read.</summary>
    public const int CurrentFormatVersion = 1;

    private const string ManifestFileName = "campaign.json";
    private const string StateFileName = "app-state.json";

    private readonly CampaignLocation _location;
    private readonly CampaignOptions _options;
    private readonly string _stateFilePath;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(
        CampaignLocation location,
        CampaignOptions options,
        StorageOptions storage,
        ILogger<CampaignService> logger)
    {
        _location = location;
        _options = options;
        _stateFilePath = Path.Combine(storage.DataDirectory, StateFileName);
        _logger = logger;
    }

    public Campaign? Current { get; private set; }

    public async Task<Campaign> OpenAsync(string folder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        string fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        Directory.CreateDirectory(fullPath);

        // Read (and check) the manifest before changing anything, so a failed open
        // leaves the previous campaign in place.
        CampaignManifest manifest = await ReadOrCreateManifestAsync(fullPath, cancellationToken).ConfigureAwait(false);

        _location.Folder = fullPath;
        Current = new Campaign(fullPath, manifest.Name);
        await RememberAsync(fullPath, cancellationToken).ConfigureAwait(false);

        return Current;
    }

    public async Task<Campaign> OpenLastOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        string? lastFolder = await ReadLastFolderAsync(cancellationToken).ConfigureAwait(false);

        if (lastFolder is not null)
        {
            if (!Directory.Exists(lastFolder))
            {
                // Moved, deleted, or on a drive that is not plugged in.
                CampaignLog.LastCampaignMissing(_logger, lastFolder);
            }
            else
            {
                try
                {
                    return await OpenAsync(lastFolder, cancellationToken).ConfigureAwait(false);
                }
                catch (CampaignException exception)
                {
                    CampaignLog.LastCampaignUnusable(_logger, exception, lastFolder);
                }
            }
        }

        return await OpenAsync(_options.DefaultFolder, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<CampaignManifest> ReadOrCreateManifestAsync(string folder, CancellationToken cancellationToken)
    {
        string path = Path.Combine(folder, ManifestFileName);

        if (!File.Exists(path))
        {
            // Any folder can become a campaign. It is named after the folder to begin with.
            var created = new CampaignManifest(CurrentFormatVersion, Path.GetFileName(folder));
            string json = JsonSerializer.Serialize(created, CampaignJson.Options) + "\n";
            await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
            return created;
        }

        CampaignManifest? manifest;
        try
        {
            string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            manifest = JsonSerializer.Deserialize<CampaignManifest>(json, CampaignJson.Options);
        }
        catch (JsonException exception)
        {
            throw new CampaignException($"The file {ManifestFileName} in '{folder}' could not be read.", exception);
        }

        if (manifest is null)
        {
            throw new CampaignException($"The file {ManifestFileName} in '{folder}' is empty.");
        }

        if (manifest.FormatVersion > CurrentFormatVersion)
        {
            throw new CampaignException(
                $"The campaign in '{folder}' was saved by a newer version of this app. Update the app to open it.");
        }

        return manifest;
    }

    private async Task<string?> ReadLastFolderAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_stateFilePath))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(_stateFilePath, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppState>(json)?.LastCampaignFolder;
        }
        catch (JsonException)
        {
            // Only a convenience: without it the default campaign opens.
            return null;
        }
    }

    private async Task RememberAsync(string folder, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
        string json = JsonSerializer.Serialize(new AppState(folder));
        await File.WriteAllTextAsync(_stateFilePath, json, cancellationToken).ConfigureAwait(false);
    }

    private sealed record AppState(string? LastCampaignFolder);
}