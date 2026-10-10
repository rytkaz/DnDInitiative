using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Campaigns;

/// <summary>
/// Keeps items of one kind as one JSON file each, in a subfolder of the open campaign.
/// </summary>
internal sealed class JsonFolderStore<T>
    where T : class
{
    private readonly CampaignLocation _location;
    private readonly string _subfolder;
    private readonly Func<T, Guid> _getId;
    private readonly Func<T, string> _getName;
    private readonly ILogger _logger;

    public JsonFolderStore(
        CampaignLocation location,
        string subfolder,
        Func<T, Guid> getId,
        Func<T, string> getName,
        ILogger logger)
    {
        _location = location;
        _subfolder = subfolder;
        _getId = getId;
        _getName = getName;
        _logger = logger;
    }

    private string FolderPath => Path.Combine(_location.Folder, _subfolder);

    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken)
    {
        List<StoredFile> files = await ReadFilesAsync(cancellationToken).ConfigureAwait(false);

        // Two files with one Id can appear after a careless merge or a copied file. Show the first only.
        var items = new List<T>();
        var seen = new HashSet<Guid>();
        foreach (StoredFile file in files)
        {
            if (seen.Add(_getId(file.Item)))
            {
                items.Add(file.Item);
            }
            else
            {
                CampaignLog.DuplicateId(_logger, file.Path);
            }
        }

        return items;
    }

    public async Task SaveAsync(T item, CancellationToken cancellationToken)
    {
        Guid id = _getId(item);
        Directory.CreateDirectory(FolderPath);

        List<StoredFile> files = await ReadFilesAsync(cancellationToken).ConfigureAwait(false);
        List<string> ownFiles = files
            .Where(file => _getId(file.Item) == id)
            .Select(file => file.Path)
            .ToList();

        // Every name already in the folder is off limits, including files that could not be
        // read. Only this item's own files may be replaced.
        var taken = new HashSet<string>(
            Directory.EnumerateFiles(FolderPath, "*.json").Select(file => Path.GetFileName(file)),
            StringComparer.OrdinalIgnoreCase);
        taken.ExceptWith(ownFiles.Select(file => Path.GetFileName(file)));

        string path = Path.Combine(FolderPath, FileNames.UniqueFileName(FileNames.Slug(_getName(item)), taken));
        string json = JsonSerializer.Serialize(item, CampaignJson.Options) + "\n";

        // Written under a temporary name and then renamed, so a crash halfway through
        // never leaves a half-written file where a good one used to be.
        string temporaryPath = path + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
        File.Move(temporaryPath, path, overwrite: true);

        // The item was renamed, or existed twice: remove what is now out of date.
        foreach (string oldPath in ownFiles)
        {
            if (!string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(oldPath);
            }
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        List<StoredFile> files = await ReadFilesAsync(cancellationToken).ConfigureAwait(false);
        foreach (StoredFile file in files.Where(file => _getId(file.Item) == id))
        {
            File.Delete(file.Path);
        }
    }

    private async Task<List<StoredFile>> ReadFilesAsync(CancellationToken cancellationToken)
    {
        var files = new List<StoredFile>();
        if (!Directory.Exists(FolderPath))
        {
            return files;
        }

        // Sorted, so "the first file with this Id" means the same thing on every machine.
        foreach (string path in Directory.EnumerateFiles(FolderPath, "*.json").Order(StringComparer.OrdinalIgnoreCase))
        {
            string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            try
            {
                if (JsonSerializer.Deserialize<T>(json, CampaignJson.Options) is { } item)
                {
                    files.Add(new StoredFile(path, item));
                }
            }
            catch (JsonException exception)
            {
                // Typically a file left with merge-conflict markers. One bad file must not hide the rest.
                CampaignLog.UnreadableFile(_logger, exception, path);
            }
        }

        return files;
    }

    private sealed record StoredFile(string Path, T Item);
}