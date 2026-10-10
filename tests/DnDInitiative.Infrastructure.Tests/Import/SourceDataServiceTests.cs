using System.IO.Compression;
using System.Text;

using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Import;

public sealed class SourceDataServiceTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly SourceDataService _service;

    public SourceDataServiceTests()
    {
        _service = new SourceDataService(new StorageOptions(Path.Combine(_root, "appdata")));
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Theory]
    [InlineData("")]
    [InlineData("data")]
    [InlineData("5etools-src-2.1.0/data")]
    public void FindDataFolder_PickedAtOrAboveTheData_FindsTheFolderWithTheBestiary(string dataPathInsidePicked)
    {
        string picked = Path.Combine(_root, "picked");
        string dataFolder = Path.GetFullPath(Path.Combine(picked, dataPathInsidePicked));
        Directory.CreateDirectory(Path.Combine(dataFolder, "bestiary"));

        string? found = _service.FindDataFolder(picked);

        Assert.Equal(dataFolder, Path.GetFullPath(Assert.IsType<string>(found)));
    }

    [Fact]
    public void FindDataFolder_NoBestiaryAnywhere_ReturnsNull()
    {
        string picked = Path.Combine(_root, "documents");
        Directory.CreateDirectory(Path.Combine(picked, "letters"));

        Assert.Null(_service.FindDataFolder(picked));
        Assert.Null(_service.FindDataFolder(Path.Combine(_root, "does-not-exist")));
    }

    [Fact]
    public async Task ImportZipAsync_ReleaseZip_UnpacksOnlyTheDataFolder()
    {
        string zip = CreateZip(
            "5etools-src-2.1.0/data/bestiary/bestiary-mm.json",
            "5etools-src-2.1.0/data/spells/spells-phb.json",
            "5etools-src-2.1.0/js/app.js",
            "5etools-src-2.1.0/index.html");

        string dataFolder = await _service.ImportZipAsync(zip);

        Assert.True(File.Exists(Path.Combine(dataFolder, "bestiary", "bestiary-mm.json")));
        Assert.True(File.Exists(Path.Combine(dataFolder, "spells", "spells-phb.json")));
        Assert.False(Directory.Exists(Path.Combine(dataFolder, "js")));
        Assert.Equal(dataFolder, _service.FindDataFolder(dataFolder));
    }

    [Fact]
    public async Task ImportZipAsync_ZipOfTheDataFolderItself_Works()
    {
        string zip = CreateZip("bestiary/bestiary-mm.json", "spells/spells-phb.json");

        string dataFolder = await _service.ImportZipAsync(zip);

        Assert.True(File.Exists(Path.Combine(dataFolder, "bestiary", "bestiary-mm.json")));
    }

    [Fact]
    public async Task ImportZipAsync_ZipWithoutData_RefusesAndKeepsTheEarlierData()
    {
        string dataFolder = await _service.ImportZipAsync(CreateZip("data/bestiary/bestiary-mm.json"));
        string wrongZip = CreateZip("holiday/photo.jpg");

        await Assert.ThrowsAsync<ConversionException>(() => _service.ImportZipAsync(wrongZip));

        Assert.True(File.Exists(Path.Combine(dataFolder, "bestiary", "bestiary-mm.json")));
    }

    [Fact]
    public async Task ImportZipAsync_NotAZipFile_SaysSo()
    {
        string notAZip = Path.Combine(_root, "data.zip");
        await File.WriteAllTextAsync(notAZip, "this is plain text");

        ConversionException exception = await Assert.ThrowsAsync<ConversionException>(() => _service.ImportZipAsync(notAZip));

        Assert.Contains("data.zip", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportZipAsync_EntryPointingOutsideTheFolder_IsRefused()
    {
        string zip = CreateZip("data/bestiary/bestiary-mm.json", "data/bestiary/../../../escaped.txt");

        await Assert.ThrowsAsync<ConversionException>(() => _service.ImportZipAsync(zip));

        Assert.False(File.Exists(Path.Combine(_root, "escaped.txt")));
        Assert.False(File.Exists(Path.Combine(_root, "appdata", "escaped.txt")));
    }

    [Fact]
    public async Task GetMonsterSourcesAsync_ListsSourcesWithMonstersByName()
    {
        string data = Path.Combine(_root, "data");
        Directory.CreateDirectory(Path.Combine(data, "bestiary"));
        await File.WriteAllTextAsync(
            Path.Combine(data, "bestiary", "index.json"),
            """{ "XMM": "bestiary-xmm.json", "LMoP": "bestiary-lmop.json", "ZZZ": "bestiary-zzz.json" }""");
        await File.WriteAllTextAsync(
            Path.Combine(data, "books.json"),
            """{ "book": [ { "name": "Monster Manual (2024)", "source": "XMM" }, { "name": "Player's Handbook", "source": "PHB" } ] }""");
        await File.WriteAllTextAsync(
            Path.Combine(data, "adventures.json"),
            """{ "adventure": [ { "name": "Lost Mine of Phandelver", "source": "LMoP" } ] }""");

        IReadOnlyList<SourceBook> sources = await _service.GetMonsterSourcesAsync(data);

        SourceBook[] expected =
        [
            new("LMoP", "Lost Mine of Phandelver"),
        new("XMM", "Monster Manual (2024)"),
        new("ZZZ", "ZZZ"),
    ];
        Assert.Equal(expected, sources.ToArray());
    }

    [Fact]
    public async Task GetMonsterSourcesAsync_FilesMissingOrUnreadable_ReturnsAnEmptyList()
    {
        string data = Path.Combine(_root, "data");
        Directory.CreateDirectory(Path.Combine(data, "bestiary"));
        Assert.Empty(await _service.GetMonsterSourcesAsync(data));

        await File.WriteAllTextAsync(Path.Combine(data, "bestiary", "index.json"), "not json");
        Assert.Empty(await _service.GetMonsterSourcesAsync(data));
    }

    private string CreateZip(params string[] entryNames)
    {
        string path = Path.Combine(_root, Path.GetRandomFileName() + ".zip");
        using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (string entryName in entryNames)
        {
            using Stream entry = archive.CreateEntry(entryName).Open();
            entry.Write(Encoding.UTF8.GetBytes("{}"));
        }

        return path;
    }
}