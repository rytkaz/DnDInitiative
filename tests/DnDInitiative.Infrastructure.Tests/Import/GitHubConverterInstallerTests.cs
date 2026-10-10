using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Import;

public sealed class GitHubConverterInstallerTests : IDisposable
{
    private const string ReleaseUrl = "https://api.github.com/repos/ebullient/ttrpg-convert-cli/releases/latest";
    private const string ZipUrl = "https://example.test/ttrpg-convert-cli-9.9.9-windows-x86_64.zip";
    private const string ChecksumUrl = ZipUrl + ".sha256";

    private readonly string _dataFolder = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly FakeHttpHandler _handler = new();
    private readonly HttpClient _http;
    private readonly GitHubConverterInstaller _installer;

    public GitHubConverterInstallerTests()
    {
        _http = new HttpClient(_handler);
        _installer = new GitHubConverterInstaller(_http, new StorageOptions(_dataFolder));
    }

    public void Dispose()
    {
        _http.Dispose();
        _handler.Dispose();
        Directory.Delete(_dataFolder, recursive: true);
    }

    [Fact]
    public void FindInstalled_NothingDownloaded_ReturnsNull()
    {
        Assert.Null(_installer.FindInstalled());
    }

    [Fact]
    public async Task InstallLatestAsync_DownloadsVerifiesAndUnpacksTheWindowsRelease()
    {
        byte[] zip = CreateZip("ttrpg-convert-cli-9.9.9/bin/ttrpg-convert.exe", "version 9.9.9");
        ServeRelease(zip, Convert.ToHexString(SHA256.HashData(zip)));

        string installed = await _installer.InstallLatestAsync();

        Assert.Equal("version 9.9.9", await File.ReadAllTextAsync(installed));
        Assert.Equal(installed, _installer.FindInstalled());
        Assert.Equal("ttrpg-convert.exe", Path.GetFileName(installed));
    }

    [Fact]
    public async Task InstallLatestAsync_RunAgain_ReplacesTheInstalledCopy()
    {
        byte[] first = CreateZip("bin/ttrpg-convert.exe", "old");
        ServeRelease(first, Convert.ToHexString(SHA256.HashData(first)));
        await _installer.InstallLatestAsync();

        byte[] second = CreateZip("bin/ttrpg-convert.exe", "new");
        ServeRelease(second, Convert.ToHexString(SHA256.HashData(second)));
        string installed = await _installer.InstallLatestAsync();

        Assert.Equal("new", await File.ReadAllTextAsync(installed));
    }

    [Fact]
    public async Task InstallLatestAsync_ChecksumDoesNotMatch_InstallsNothing()
    {
        ServeRelease(CreateZip("bin/ttrpg-convert.exe", "tampered"), new string('0', 64));

        await Assert.ThrowsAsync<ConversionException>(() => _installer.InstallLatestAsync());

        Assert.Null(_installer.FindInstalled());
    }

    [Fact]
    public async Task InstallLatestAsync_ReleaseHasNoWindowsDownload_SaysSo()
    {
        _handler.Respond(ReleaseUrl, """{ "tag_name": "9.9.9", "assets": [ { "name": "linux.zip", "browser_download_url": "https://example.test/linux.zip" } ] }""");

        ConversionException exception = await Assert.ThrowsAsync<ConversionException>(() => _installer.InstallLatestAsync());

        Assert.Contains("9.9.9", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallLatestAsync_NoConnection_ExplainsInPlainWords()
    {
        // Nothing is served, so every request fails.
        ConversionException exception = await Assert.ThrowsAsync<ConversionException>(() => _installer.InstallLatestAsync());

        Assert.Contains("internet connection", exception.Message, StringComparison.Ordinal);
    }

    private void ServeRelease(byte[] zip, string checksum)
    {
        _handler.Respond(
            ReleaseUrl,
            $$"""
            {
              "tag_name": "9.9.9",
              "assets": [
                { "name": "ttrpg-convert-cli-9.9.9-windows-x86_64.zip", "browser_download_url": "{{ZipUrl}}" },
                { "name": "ttrpg-convert-cli-9.9.9-windows-x86_64.zip.sha256", "browser_download_url": "{{ChecksumUrl}}" }
              ]
            }
            """);
        _handler.Respond(ZipUrl, zip);
        _handler.Respond(ChecksumUrl, checksum);
    }

    private static byte[] CreateZip(string entryName, string content)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            using Stream entry = archive.CreateEntry(entryName).Open();
            entry.Write(Encoding.UTF8.GetBytes(content));
        }

        return memory.ToArray();
    }

    /// <summary>Answers requests from a table instead of the network.</summary>
    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, byte[]> _responses = new(StringComparer.Ordinal);

        public void Respond(string url, string body)
        {
            _responses[url] = Encoding.UTF8.GetBytes(body);
        }

        public void Respond(string url, byte[] body)
        {
            _responses[url] = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string url = request.RequestUri?.ToString() ?? "";
            if (!_responses.TryGetValue(url, out byte[]? body))
            {
                throw new HttpRequestException($"No connection to {url}");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
        }
    }
}