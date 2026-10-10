using System.Text.Json;

using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Writes the converter's configuration, and the monster template it refers to, into
/// this computer's data folder.
/// </summary>
internal sealed class ConverterConfigWriter : IConverterConfigWriter
{
    private const string ConfigFileName = "config.json";
    private const string TemplateFileName = "monster-statblock.txt";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    // Three backticks: the line that opens and closes a code block in Markdown.
    private static readonly string Fence = new('`', 3);

    private readonly string _folder;

    public ConverterConfigWriter(StorageOptions storage)
    {
        _folder = Path.Combine(storage.DataDirectory, "converter");
    }

    public async Task<string> WriteAsync(
        IReadOnlyCollection<string> sourceIds,
        IReadOnlyCollection<string> homebrewFiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceIds);
        ArgumentNullException.ThrowIfNull(homebrewFiles);
        Directory.CreateDirectory(_folder);

        // The converter's own monster template writes a decorative callout. The importer needs
        // the creature as data, which this one-block template asks the converter for.
        string template = string.Join(
            '\n',
            "# {resource.name}",
            "",
            Fence + "statblock",
            "{resource.5eStatblockYaml}",
            Fence,
            "");
        await File.WriteAllTextAsync(Path.Combine(_folder, TemplateFileName), template, cancellationToken).ConfigureAwait(false);

        // "reference" brings in a book's game data without converting its chapters of text.
        var sources = new Dictionary<string, object>
        {
            ["reference"] = sourceIds.Order(StringComparer.OrdinalIgnoreCase).ToList(),
        };

        if (homebrewFiles.Count > 0)
        {
            // A homebrew file is named by its path, and brings its own books with it; they are
            // not repeated under "reference". Full paths, because the converter resolves a
            // relative one against the folder it runs in. Forward slashes, as its documentation asks.
            sources["homebrew"] = homebrewFiles
                .Select(file => Path.GetFullPath(file).Replace('\\', '/'))
                .ToList();
        }

        var config = new Dictionary<string, object>
        {
            ["sources"] = sources,

            // Relative on purpose: the converter runs in this folder, next to both files.
            ["template"] = new Dictionary<string, string> { ["monster"] = TemplateFileName },

            // Plain text in the stat block fields, without the converter's Markdown decoration.
            ["yamlStatblocks"] = true,
            ["useDiceRoller"] = false,
        };

        string configPath = Path.Combine(_folder, ConfigFileName);
        string json = JsonSerializer.Serialize(config, SerializerOptions);
        await File.WriteAllTextAsync(configPath, json, cancellationToken).ConfigureAwait(false);

        return configPath;
    }
}