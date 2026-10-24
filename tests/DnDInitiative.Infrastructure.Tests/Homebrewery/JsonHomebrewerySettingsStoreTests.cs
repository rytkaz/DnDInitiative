using DnDInitiative.Core.Homebrewery;
using DnDInitiative.Infrastructure.Homebrewery;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Homebrewery;

public sealed class JsonHomebrewerySettingsStoreTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly JsonHomebrewerySettingsStore _store;

    public JsonHomebrewerySettingsStoreTests()
    {
        _store = new JsonHomebrewerySettingsStore(new StorageOptions(_directory));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task LoadAsync_NothingStored_ReturnsTheDefaults()
    {
        Assert.Equal(new HomebrewerySettings(), await _store.LoadAsync());
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_ReturnsTheSameSettings()
    {
        var settings = new HomebrewerySettings
        {
            PdfPath = @"C:\Monsters\catalog.pdf",
            Links = "https://homebrewery.naturalcrit.com/share/one\nhttps://homebrewery.naturalcrit.com/share/two",
            Source = "My Monsters",
        };

        await _store.SaveAsync(settings);

        Assert.Equal(settings, await _store.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_FileWithoutASource_UsesTheDefaultSource()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "homebrewery-settings.json"), """{ "PdfPath": "catalog.pdf" }""");

        HomebrewerySettings settings = await _store.LoadAsync();

        Assert.Equal("catalog.pdf", settings.PdfPath);
        Assert.Equal(HomebrewerySettings.DefaultSource, settings.Source);
    }

    [Fact]
    public async Task LoadAsync_DamagedFile_ReturnsTheDefaults()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "homebrewery-settings.json"), "{ not json");

        Assert.Equal(new HomebrewerySettings(), await _store.LoadAsync());
    }
}
