using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Downloads the converter from its GitHub releases into this computer's data folder.
/// </summary>
internal sealed class GitHubConverterInstaller : IConverterInstaller
{
    private const string WindowsAssetSuffix = "-windows-x86_64.zip";
    private const string ExecutableName = "ttrpg-convert.exe";
    private const string InstalledFolderName = "current";
    private const int BytesPerMegabyte = 1024 * 1024;

    private static readonly Uri LatestReleaseUri =
        new("https://api.github.com/repos/ebullient/ttrpg-convert-cli/releases/latest");

    private readonly HttpClient _http;
    private readonly string _root;

    public GitHubConverterInstaller(HttpClient http, StorageOptions storage)
    {
        _http = http;
        _root = Path.Combine(storage.DataDirectory, "converter");
    }

    private string InstalledFolder => Path.Combine(_root, InstalledFolderName);

    public string? FindInstalled()
    {
        if (!Directory.Exists(InstalledFolder))
        {
            return null;
        }

        // The zip has its own folder layout, which has changed between releases. Search for the program.
        return Directory
            .EnumerateFiles(InstalledFolder, ExecutableName, SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    public async Task<string> InstallLatestAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report("Looking up the latest converter release...");
            Release release = await FindLatestReleaseAsync(cancellationToken).ConfigureAwait(false);

            Directory.CreateDirectory(_root);
            string zipPath = Path.Combine(_root, "download.zip");

            await DownloadAsync(release, zipPath, progress, cancellationToken).ConfigureAwait(false);
            await VerifyAsync(release, zipPath, cancellationToken).ConfigureAwait(false);

            progress?.Report("Unpacking...");
            Unpack(zipPath);
            File.Delete(zipPath);

            string installed = FindInstalled()
                ?? throw new ConversionException($"The downloaded converter did not contain {ExecutableName}.");

            progress?.Report($"Converter {release.Version} is installed.");
            return installed;
        }
        catch (HttpRequestException exception)
        {
            throw new ConversionException(
                "The converter could not be downloaded. Check your internet connection and try again. "
                    + $"({exception.Message})",
                exception);
        }
    }

    private async Task<Release> FindLatestReleaseAsync(CancellationToken cancellationToken)
    {
        string json = await _http.GetStringAsync(LatestReleaseUri, cancellationToken).ConfigureAwait(false);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        string version = root.GetProperty("tag_name").GetString() ?? "";

        // A release lists its downloadable files as "assets", each with a name and a URL.
        var urlsByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement asset in root.GetProperty("assets").EnumerateArray())
        {
            string? name = asset.GetProperty("name").GetString();
            string? url = asset.GetProperty("browser_download_url").GetString();
            if (name is not null && url is not null)
            {
                urlsByName[name] = url;
            }
        }

        string? zipName = urlsByName.Keys.FirstOrDefault(
            name => name.EndsWith(WindowsAssetSuffix, StringComparison.OrdinalIgnoreCase));

        if (zipName is null || !urlsByName.TryGetValue(zipName + ".sha256", out string? checksumUrl))
        {
            throw new ConversionException(
                $"Converter release {version} has no Windows download this app recognises. "
                    + "Download it by hand and set its path on this page.");
        }

        return new Release(version, new Uri(urlsByName[zipName]), new Uri(checksumUrl));
    }

    private async Task DownloadAsync(
        Release release,
        string zipPath,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        // ResponseHeadersRead: start handing over the body as it arrives, instead of
        // holding the whole file in memory first.
        using HttpResponseMessage response = await _http
            .GetAsync(release.ZipUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;

        Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            FileStream file = File.Create(zipPath);
            await using (file.ConfigureAwait(false))
            {
                byte[] buffer = new byte[81920];
                long downloaded = 0;
                long reportedMegabytes = -1;

                while (true)
                {
                    int read = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }

                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    downloaded += read;

                    // One line per megabyte, not one per chunk.
                    long megabytes = downloaded / BytesPerMegabyte;
                    if (megabytes != reportedMegabytes)
                    {
                        reportedMegabytes = megabytes;
                        progress?.Report(DescribeProgress(release.Version, megabytes, totalBytes));
                    }
                }
            }
        }
    }

    private static string DescribeProgress(string version, long megabytes, long? totalBytes)
    {
        return totalBytes is long total
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"Downloading converter {version}: {megabytes} of {total / BytesPerMegabyte} MB")
            : string.Create(CultureInfo.InvariantCulture, $"Downloading converter {version}: {megabytes} MB");
    }

    private async Task VerifyAsync(Release release, string zipPath, CancellationToken cancellationToken)
    {
        // The file holds the hash, sometimes followed by the file name.
        string published = await _http.GetStringAsync(release.ChecksumUri, cancellationToken).ConfigureAwait(false);
        string expected = published.Trim().Split(' ', 2)[0];

        byte[] hash;
        FileStream file = File.OpenRead(zipPath);
        await using (file.ConfigureAwait(false))
        {
            hash = await SHA256.HashDataAsync(file, cancellationToken).ConfigureAwait(false);
        }

        if (!string.Equals(expected, Convert.ToHexString(hash), StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(zipPath);
            throw new ConversionException(
                "The downloaded converter does not match its published checksum, so it was not installed. Try again.");
        }
    }

    private void Unpack(string zipPath)
    {
        // Unpacked beside the installed copy and swapped in at the end, so a failure
        // halfway never leaves a half-installed converter.
        string unpacking = InstalledFolder + ".new";
        if (Directory.Exists(unpacking))
        {
            Directory.Delete(unpacking, recursive: true);
        }

        ZipFile.ExtractToDirectory(zipPath, unpacking);

        if (Directory.Exists(InstalledFolder))
        {
            Directory.Delete(InstalledFolder, recursive: true);
        }

        Directory.Move(unpacking, InstalledFolder);
    }

    private sealed record Release(string Version, Uri ZipUri, Uri ChecksumUri);
}