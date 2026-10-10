using System.Text.Json;

using DnDInitiative.Core.Spells;
using DnDInitiative.Core.StatBlocks;
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

        string configPath = await writer.WriteAsync(new ConverterConfigOptions { Sources = ["XMM", "MM"] });

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

        string configPath = await writer.WriteAsync(new ConverterConfigOptions());

        using JsonDocument config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        Assert.Equal(0, config.RootElement.GetProperty("sources").GetProperty("reference").GetArrayLength());
        Assert.True(config.RootElement.TryGetProperty("template", out _));

        // No empty "homebrew" list either: the key is only written when there is something in it.
        Assert.False(config.RootElement.GetProperty("sources").TryGetProperty("homebrew", out _));
        Assert.False(config.RootElement.TryGetProperty("excludePattern", out _));
    }

    [Fact]
    public async Task WriteAsync_SpellSources_AreConvertedButTheirCreaturesAreLeftOutUnlessTicked()
    {
        var writer = new ConverterConfigWriter(new StorageOptions(_dataFolder));

        string configPath = await writer.WriteAsync(new ConverterConfigOptions
        {
            Sources = ["XMM", "FTD"],
            SpellSources = ["XPHB", "ftd", "XGE"],
        });

        using JsonDocument config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        JsonElement root = config.RootElement;

        // FTD was ticked and has spells: listed once, not twice.
        string?[] expectedSources = ["FTD", "XGE", "XMM", "XPHB"];
        Assert.Equal(expectedSources, Strings(root.GetProperty("sources").GetProperty("reference")));

        // Only the two books that are there for their spells alone lose their stat blocks.
        string?[] expectedPatterns =
        [
            @"monster\|.*\|xphb", @"object\|.*\|xphb", @"vehicle\|.*\|xphb",
            @"monster\|.*\|xge", @"object\|.*\|xge", @"vehicle\|.*\|xge",
        ];
        Assert.Equal(expectedPatterns, Strings(root.GetProperty("excludePattern")));
    }

    [Fact]
    public async Task WriteAsync_WritesASpellTemplateTheSpellParserCanReadBack()
    {
        var writer = new ConverterConfigWriter(new StorageOptions(_dataFolder));

        string configPath = await writer.WriteAsync(new ConverterConfigOptions());

        using JsonDocument config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        string templateName = config.RootElement.GetProperty("template").GetProperty("spell").GetString() ?? "";
        string template = await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(configPath)!, templateName));

        // Stand in for the converter: fill every placeholder in with a value, then parse the result.
        string note = template
            .Replace("{resource.name}", "Fireball", StringComparison.Ordinal)
            .Replace("{resource.level}", "3rd-level", StringComparison.Ordinal)
            .Replace("{resource.school}", "Evocation", StringComparison.Ordinal)
            .Replace("{resource.ritual}", "false", StringComparison.Ordinal)
            .Replace("{resource.time}", "1 action", StringComparison.Ordinal)
            .Replace("{resource.range}", "150 feet", StringComparison.Ordinal)
            .Replace("{resource.components}", "V, S, M (a ball of bat guano and sulfur)", StringComparison.Ordinal)
            .Replace("{resource.duration}", "Instantaneous", StringComparison.Ordinal)
            .Replace("{resource.source}", "XPHB p. 274", StringComparison.Ordinal)
            .Replace("{resource.text}", "A bright streak flashes.", StringComparison.Ordinal);

        Assert.DoesNotContain("{resource.", note, StringComparison.Ordinal);
        Assert.Equal(
            new Spell
            {
                Key = "fireball-xphb",
                Name = "Fireball",
                Level = "3rd-level",
                School = "Evocation",
                CastingTime = "1 action",
                Range = "150 feet",
                Components = "V, S, M (a ball of bat guano and sulfur)",
                Duration = "Instantaneous",
                Source = "XPHB p. 274",
                Description = "A bright streak flashes.",
            },
            SpellNoteParser.Parse(note, "fireball-xphb"));
    }

    private static string?[] Strings(JsonElement array)
    {
        return array.EnumerateArray().Select(item => item.GetString()).ToArray();
    }

    [Fact]
    public async Task WriteAsync_HomebrewFiles_AreListedByFullPathWithForwardSlashes()
    {
        var writer = new ConverterConfigWriter(new StorageOptions(_dataFolder));
        string file = Path.Combine(_dataFolder, "brew", "Flee, Mortals!.json");

        string configPath = await writer.WriteAsync(new ConverterConfigOptions { Sources = ["MM"], HomebrewFiles = [file] });

        using JsonDocument config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        JsonElement sources = config.RootElement.GetProperty("sources");

        string? written = Assert.Single(sources.GetProperty("homebrew").EnumerateArray()).GetString();
        Assert.Equal(file.Replace('\\', '/'), written);

        // The books are still there; homebrew is added to them, not instead of them.
        Assert.Equal("MM", Assert.Single(sources.GetProperty("reference").EnumerateArray()).GetString());
    }
}