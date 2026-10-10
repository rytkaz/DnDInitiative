using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Writes the converter's configuration, and the monster and spell templates it refers to,
/// into this computer's data folder.
/// </summary>
internal sealed class ConverterConfigWriter : IConverterConfigWriter
{
    private const string ConfigFileName = "config.json";
    private const string TemplateFileName = "monster-statblock.txt";
    private const string SpellTemplateFileName = "spell-note.txt";
    private const string HomebrewFolderName = "homebrew";

    // What the converter can write a stat block for. Used to keep these out of the
    // library when a book is only being converted for its spells.
    private static readonly string[] StatBlockTypes = ["monster", "object", "vehicle"];

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    // Three backticks: the line that opens and closes a code block in Markdown.
    private static readonly string Fence = new('`', 3);

    private readonly string _folder;

    public ConverterConfigWriter(StorageOptions storage)
    {
        _folder = Path.Combine(storage.DataDirectory, "converter");
    }

    public async Task<string> WriteAsync(ConverterConfigOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
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

        // The same idea for spells: a block of named values that SpellNoteParser reads back,
        // then the spell's text. "{resource.time}" and the rest are filled in by the converter.
        string[] spellTemplateLines =
        [
            "# {resource.name}",
            "",
            SpellNoteParser.FenceStart,

            // One line per field, such as "time: {resource.time}". The doubled braces are
            // how an interpolated string writes a literal brace.
            .. SpellNoteParser.FieldNames.Select(field => $"{field}: {{resource.{field}}}"),
            Fence,
            "",
            "{resource.text}",
            "",
        ];
        string spellTemplate = string.Join('\n', spellTemplateLines);
        await File.WriteAllTextAsync(Path.Combine(_folder, SpellTemplateFileName), spellTemplate, cancellationToken).ConfigureAwait(false);

        // The converter only links a creature to a spell whose book is being converted, and a
        // monster book rarely contains its own spells. So every book with spells is converted
        // too; the ones that were not ticked have their creatures left out further down.
        var wanted = new HashSet<string>(options.Sources, StringComparer.OrdinalIgnoreCase);
        List<string> spellOnly = options.SpellSources
            .Where(source => !wanted.Contains(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // "reference" brings in a book's game data without converting its chapters of text.
        var sources = new Dictionary<string, object>
        {
            ["reference"] = wanted.Concat(spellOnly).Order(StringComparer.OrdinalIgnoreCase).ToList(),
        };

        IReadOnlyList<string> homebrewFiles = await PrepareHomebrewAsync(options.HomebrewFiles, cancellationToken).ConfigureAwait(false);
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
            ["template"] = new Dictionary<string, string>
            {
                ["monster"] = TemplateFileName,
                ["spell"] = SpellTemplateFileName,
            },

            // Plain text in the stat block fields, without the converter's Markdown decoration.
            ["yamlStatblocks"] = true,
            ["useDiceRoller"] = false,
        };

        if (spellOnly.Count > 0)
        {
            config["excludePattern"] = spellOnly.SelectMany(ExcludeStatBlocksOf).ToList();
        }

        string configPath = Path.Combine(_folder, ConfigFileName);
        string json = JsonSerializer.Serialize(config, SerializerOptions);
        await File.WriteAllTextAsync(configPath, json, cancellationToken).ConfigureAwait(false);

        return configPath;
    }

    /// <summary>
    /// Copies the homebrew files next to the configuration, with their web links rewritten
    /// (see <see cref="HomebrewLinks"/>). The converter is given the copies; the user's own
    /// files are only ever read.
    /// </summary>
    /// <returns>The paths of the copies, in the order the files were given.</returns>
    private async Task<IReadOnlyList<string>> PrepareHomebrewAsync(
        IReadOnlyCollection<string> files,
        CancellationToken cancellationToken)
    {
        string folder = Path.Combine(_folder, HomebrewFolderName);

        // Copies from an earlier run, for files that may since have been removed from the list.
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        var copies = new List<string>();
        if (files.Count == 0)
        {
            return copies;
        }

        Directory.CreateDirectory(folder);

        foreach (string file in files)
        {
            string json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);

            // Numbered, so two files with the same name from different folders do not collide.
            string copy = Path.Combine(
                folder,
                string.Create(CultureInfo.InvariantCulture, $"{copies.Count + 1}-{Path.GetFileName(file)}"));

            await File.WriteAllTextAsync(copy, HomebrewLinks.ToMarkdown(json), cancellationToken).ConfigureAwait(false);
            copies.Add(copy);
        }

        return copies;
    }

    /// <summary>
    /// The converter names everything "type|name|source" in lower case, such as "monster|goblin|mm",
    /// and leaves out whatever matches one of these patterns. The pipe is escaped because in a
    /// regular expression a bare one means "or".
    /// </summary>
    private static IEnumerable<string> ExcludeStatBlocksOf(string source)
    {
        string escapedSource = Regex.Escape(source.ToLowerInvariant());
        return StatBlockTypes.Select(type => $@"{type}\|.*\|{escapedSource}");
    }
}
