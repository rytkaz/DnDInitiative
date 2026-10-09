using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Import;

public sealed class JsonConversionSettingsStoreTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly JsonConversionSettingsStore _store;

    public JsonConversionSettingsStoreTests()
    {
        _store = new JsonConversionSettingsStore(new StorageOptions(_directory));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task LoadAsync_NothingStored_ReturnsEmptySettings()
    {
        Assert.Equal(new ConversionSettings(), await _store.LoadAsync());
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_ReturnsTheSameSettings()
    {
        var settings = new ConversionSettings
        {
            ToolPath = @"C:\cli\ttrpg-convert.exe",
            ConfigPath = @"C:\cli\config.json",
            DataDirectory = @"C:\cli\5etools\data",
            OutputDirectory = @"C:\cli\output",
        };

        await _store.SaveAsync(settings);

        Assert.Equal(settings, await _store.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_DamagedFile_ReturnsEmptySettings()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "conversion-settings.json"), "{ not json");

        Assert.Equal(new ConversionSettings(), await _store.LoadAsync());
    }
}