using System.Text.Json;

using DnDInitiative.Core.Campaigns;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Campaigns;

internal sealed class CampaignService : ICampaignService
{
    /// <summary>Raised whenever the layout of a campaign folder changes in a way older versions cannot read.</summary>
    public const int CurrentFormatVersion = 1;

    public event EventHandler? FilesChangedExternally;

    private const string ManifestFileName = "campaign.json";
    private const string StateFileName = "app-state.json";

    // The subfolders the repositories keep their items in. A new kind of item needs its folder
    // added here, or Save As leaves it behind.
    private static readonly string[] ItemFolders = ["parties", "encounters", "statblocks"];

    private readonly CampaignLocation _location;
    private readonly CampaignOptions _options;
    private readonly string _stateFilePath;
    private readonly ILogger<CampaignService> _logger;
    private readonly CampaignFileWatcher _watcher;

    public CampaignService(
        CampaignLocation location,
        CampaignOptions options,
        StorageOptions storage,
        CampaignFileWatcher watcher,
        ILogger<CampaignService> logger)
    {
        _location = location;
        _options = options;
        _stateFilePath = Path.Combine(storage.DataDirectory, StateFileName);
        _watcher = watcher;
        _logger = logger;

        _watcher.Changed += (_, _) => FilesChangedExternally?.Invoke(this, EventArgs.Empty);
    }

    public Campaign? Current { get; private set; }

    public CampaignFolderState Inspect(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        if (!Directory.Exists(folder))
        {
            return CampaignFolderState.Empty;
        }

        if (File.Exists(Path.Combine(folder, ManifestFileName)))
        {
            return CampaignFolderState.Campaign;
        }

        // Names starting with a dot (.git, .gitignore) do not count: a new, empty
        // git repository is a perfectly good place to start a campaign.
        bool hasOtherFiles = Directory
            .EnumerateFileSystemEntries(folder)
            .Any(entry => !Path.GetFileName(entry).StartsWith('.'));

        return hasOtherFiles ? CampaignFolderState.HasOtherFiles : CampaignFolderState.Empty;
    }

    public async Task<Campaign> OpenAsync(string folder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        string fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        Directory.CreateDirectory(fullPath);

        // Read (and check) the manifest before changing anything, so a failed open
        // leaves the previous campaign in place.
        CampaignManifest manifest = await ReadOrCreateManifestAsync(fullPath, cancellationToken).ConfigureAwait(false);

        _location.Folder = fullPath;
        _watcher.Watch(fullPath);
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

    public async Task<Campaign> SaveAsAsync(string folder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        Campaign source = Current ?? throw new InvalidOperationException("No campaign is open.");
        string target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));

        // A copy inside the campaign would be copied again by the next Save As, and end up in its git repository.
        if (IsSameOrInside(target, source.Folder))
        {
            throw new CampaignException($"'{target}' is inside the open campaign. Pick a folder outside it.");
        }

        if (Inspect(target) != CampaignFolderState.Empty)
        {
            throw new CampaignException($"'{target}' is not empty. Pick an empty folder for the copy.");
        }

        try
        {
            CopyItemFiles(source.Folder, target);
            await WriteManifestAsync(target, NewManifest(target), cancellationToken).ConfigureAwait(false);
        }
        catch (IOException exception)
        {
            throw CopyFailed(target, exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw CopyFailed(target, exception);
        }

        return await OpenAsync(target, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsSameOrInside(string folder, string campaignFolder)
    {
        return folder.Equals(campaignFolder, StringComparison.OrdinalIgnoreCase)
            || folder.StartsWith(campaignFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyItemFiles(string sourceFolder, string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);

        // Only the item folders: anything else in the campaign folder, such as .git, belongs to the original.
        foreach (string subfolder in ItemFolders)
        {
            string from = Path.Combine(sourceFolder, subfolder);
            if (!Directory.Exists(from))
            {
                continue;
            }

            string to = Path.Combine(targetFolder, subfolder);
            Directory.CreateDirectory(to);

            // Only the item files, not a .tmp left behind by a save that was interrupted.
            foreach (string file in Directory.EnumerateFiles(from, "*.json"))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
            }
        }
    }

    private static CampaignException CopyFailed(string folder, Exception exception)
    {
        return new CampaignException($"The campaign could not be copied to '{folder}': {exception.Message}", exception);
    }

    // Any folder can become a campaign. It is named after the folder to begin with.
    private static CampaignManifest NewManifest(string folder)
    {
        return new CampaignManifest(CurrentFormatVersion, Path.GetFileName(folder));
    }

    private static async Task WriteManifestAsync(string folder, CampaignManifest manifest, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(manifest, CampaignJson.Options) + "\n";
        await File.WriteAllTextAsync(Path.Combine(folder, ManifestFileName), json, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<CampaignManifest> ReadOrCreateManifestAsync(string folder, CancellationToken cancellationToken)
    {
        string path = Path.Combine(folder, ManifestFileName);

        if (!File.Exists(path))
        {
            CampaignManifest created = NewManifest(folder);
            await WriteManifestAsync(folder, created, cancellationToken).ConfigureAwait(false);
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