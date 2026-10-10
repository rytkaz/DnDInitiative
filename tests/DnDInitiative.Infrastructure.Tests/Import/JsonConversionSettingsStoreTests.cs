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
        Assert.Equal("", (await _store.LoadAsync()).ToolPath);
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
            Sources = ["MM", "XMM"],
        };

        await _store.SaveAsync(settings);
        ConversionSettings loaded = await _store.LoadAsync();

        Assert.Equal(settings.ToolPath, loaded.ToolPath);
        Assert.Equal(settings.ConfigPath, loaded.ConfigPath);
        Assert.Equal(settings.DataDirectory, loaded.DataDirectory);
        Assert.Equal(settings.OutputDirectory, loaded.OutputDirectory);
        Assert.Equal(settings.Sources.ToArray(), loaded.Sources.ToArray());
    }

    [Fact]
    public async Task LoadAsync_DamagedFile_ReturnsEmptySettings()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "conversion-settings.json"), "{ not json");

        Assert.Equal("", (await _store.LoadAsync()).ToolPath);
    }
}