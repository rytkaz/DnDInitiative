using System.Globalization;
using System.IO.Compression;
using System.Text.Json;

using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Import;

internal sealed class SourceDataService : ISourceDataService
{
    // The folder every copy of the data has, used to recognise it.
    private const string BestiaryFolder = "bestiary";

    private readonly string _unpackedFolder;

    public SourceDataService(StorageOptions storage)
    {
        _unpackedFolder = Path.Combine(storage.DataDirectory, "source-data");
    }

    public string? FindDataFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return null;
        }

        // The data folder itself, the release folder that contains it, or the folder a zip
        // was unpacked into, which contains the release folder.
        IEnumerable<string> candidates = Directory
            .EnumerateDirectories(folder)
            .Select(subfolder => Path.Combine(subfolder, "data"))
            .Prepend(Path.Combine(folder, "data"))
            .Prepend(folder);

        return candidates.FirstOrDefault(candidate => Directory.Exists(Path.Combine(candidate, BestiaryFolder)));
    }

    public async Task<string> ImportZipAsync(
        string zipPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);

        // Unpacked beside the current copy and swapped in at the end, so a zip that turns
        // out to be wrong never destroys data that was working.
        string unpacking = _unpackedFolder + ".new";

        try
        {
            // Unzipping is plain, blocking file work. Task.Run moves it off the UI thread.
            await Task.Run(() => Extract(zipPath, unpacking, progress, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidDataException exception)
        {
            throw new ConversionException($"'{Path.GetFileName(zipPath)}' is not a zip file, or it is damaged.", exception);
        }

        if (Directory.Exists(_unpackedFolder))
        {
            Directory.Delete(_unpackedFolder, recursive: true);
        }

        Directory.Move(unpacking, _unpackedFolder);
        return _unpackedFolder;
    }

    private static void Extract(string zipPath, string targetFolder, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (Directory.Exists(targetFolder))
        {
            Directory.Delete(targetFolder, recursive: true);
        }

        using ZipArchive archive = ZipFile.OpenRead(zipPath);

        string dataPrefix = FindDataPrefix(archive)
            ?? throw new ConversionException(
                $"'{Path.GetFileName(zipPath)}' does not contain 5eTools data: there is no '{BestiaryFolder}' folder in it.");

        // Ends with a separator, so "…\source-data.new-evil" cannot pass as being inside "…\source-data.new".
        string targetRoot = Path.GetFullPath(targetFolder) + Path.DirectorySeparatorChar;
        int extracted = 0;

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string entryPath = entry.FullName.Replace('\\', '/');

            // An empty Name marks a folder entry. Folders are created as their files arrive.
            if (entry.Name.Length == 0 || !entryPath.StartsWith(dataPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            string destination = Path.GetFullPath(Path.Combine(targetFolder, entryPath[dataPrefix.Length..]));

            // A zip can name its entries "../../somewhere/else". Refuse anything that would land outside.
            if (!destination.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new ConversionException("The zip file contains a file path that points outside the data folder.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: true);

            extracted++;
            if (extracted % 200 == 0)
            {
                progress?.Report(string.Create(CultureInfo.InvariantCulture, $"Unpacked {extracted} files..."));
            }
        }

        progress?.Report(string.Create(CultureInfo.InvariantCulture, $"Unpacked {extracted} files."));
    }

    /// <summary>
    /// Works out where the data sits inside the zip: the part of the path in front of "bestiary/".
    /// In a release zip that is something like "5etools-src-2.1.0/data/".
    /// </summary>
    private static string? FindDataPrefix(ZipArchive archive)
    {
        List<string> paths = archive.Entries.Select(entry => entry.FullName.Replace('\\', '/')).ToList();

        // Prefer a bestiary inside a "data" folder; accept a zip made of the data folder's contents.
        return PrefixBefore(paths, "data/" + BestiaryFolder + "/", keep: "data/".Length)
            ?? PrefixBefore(paths, BestiaryFolder + "/", keep: 0);
    }

    private static string? PrefixBefore(List<string> paths, string marker, int keep)
    {
        foreach (string path in paths)
        {
            int index = path.IndexOf(marker, StringComparison.Ordinal);

            // Only a whole folder name counts: at the start, or right after a slash.
            if (index == 0 || (index > 0 && path[index - 1] == '/'))
            {
                return path[..(index + keep)];
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<SourceBook>> GetMonsterSourcesAsync(
    string dataFolder,
    CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataFolder);

        try
        {
            // bestiary/index.json maps each source that has monsters to its file: { "MM": "bestiary-mm.json", ... }
            List<string> ids = await ReadPropertyNamesAsync(
                Path.Combine(dataFolder, BestiaryFolder, "index.json"),
                cancellationToken).ConfigureAwait(false);

            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await AddNamesAsync(names, Path.Combine(dataFolder, "books.json"), "book", cancellationToken).ConfigureAwait(false);
            await AddNamesAsync(names, Path.Combine(dataFolder, "adventures.json"), "adventure", cancellationToken).ConfigureAwait(false);

            return ids
                .Select(id => new SourceBook(id, names.GetValueOrDefault(id, id)))
                .OrderBy(book => book.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (JsonException)
        {
            // A data folder this code does not understand: offer no list instead of failing the page.
            return [];
        }
    }

    private static async Task<List<string>> ReadPropertyNamesAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false));

        return document.RootElement.ValueKind == JsonValueKind.Object
            ? document.RootElement.EnumerateObject().Select(property => property.Name).ToList()
            : [];
    }

    // books.json looks like { "book": [ { "name": "Monster Manual", "source": "MM", ... }, ... ] }
    private static async Task AddNamesAsync(
        Dictionary<string, string> names,
        string path,
        string listProperty,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false));

        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty(listProperty, out JsonElement list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement entry in list.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty("source", out JsonElement source)
                && entry.TryGetProperty("name", out JsonElement name)
                && source.GetString() is { } id
                && name.GetString() is { } text)
            {
                names[id] = text;
            }
        }
    }
}