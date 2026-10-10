using System.Text.Json;

using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Import;

public sealed class ConverterConfigWriterTests : IDisposable
{
    private readonly string _dataFolder = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;

    public void Dispose()
    {
        Directory.Delete(_dataFolder, recursive: true);
    }

    [Fact]
    public async Task WriteAsync_WritesAConfigForTheBooksAndTheTemplateItPointsAt()
    {
        var writer = new ConverterConfigWriter(new StorageOptions(_dataFolder));

        string configPath = await writer.WriteAsync(["XMM", "MM"]);

        using JsonDocument config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        JsonElement root = config.RootElement;

        string?[] sources = root.GetProperty("sources").GetProperty("reference")
            .EnumerateArray()
            .Select(source => source.GetString())
            .ToArray();
        string?[] expectedSources = ["MM", "XMM"];
        Assert.Equal(expectedSources, sources);
        Assert.True(root.GetProperty("yamlStatblocks").GetBoolean());

        // The template is named relative to the config, and must exist beside it.
        string templateName = root.GetProperty("template").GetProperty("monster").GetString() ?? "";
        string template = await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(configPath)!, templateName));
        Assert.Contains("{resource.5eStatblockYaml}", template, StringComparison.Ordinal);
        Assert.Contains(new string('`', 3) + "statblock", template, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteAsync_NoBooksTicked_StillWritesAUsableConfig()
    {
        var writer = new ConverterConfigWriter(new StorageOptions(_dataFolder));

        string configPath = await writer.WriteAsync([]);

        using JsonDocument config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        Assert.Equal(0, config.RootElement.GetProperty("sources").GetProperty("reference").GetArrayLength());
        Assert.True(config.RootElement.TryGetProperty("template", out _));
    }
}