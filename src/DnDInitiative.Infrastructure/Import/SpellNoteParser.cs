using System.Globalization;
using System.Text.RegularExpressions;

using DnDInitiative.Core.Spells;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Reads a spell from a note written with the app's own spell template
/// (see <see cref="ConverterConfigWriter"/>): a fenced "spell" block of "name: value"
/// lines, followed by the spell's text.
/// </summary>
internal static partial class SpellNoteParser
{
    /// <summary>The line that opens the block. Three backticks, then the word "spell".</summary>
    public static readonly string FenceStart = new string('`', 3) + "spell";

    /// <summary>The fields the template writes, in order. The parser reads them back by these names.</summary>
    public static readonly IReadOnlyList<string> FieldNames =
        ["name", "level", "school", "ritual", "time", "range", "components", "duration", "classes", "source"];

    /// <param name="markdown">The whole note.</param>
    /// <param name="key">The note's file name without its extension, which is what links to the spell contain.</param>
    /// <returns>The spell, or null if the note is not a spell note.</returns>
    public static Spell? Parse(string markdown, string key)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Match match = SpellNote().Match(markdown);
        if (!match.Success)
        {
            return null;
        }

        Dictionary<string, string> fields = ReadFields(match.Groups["fields"].Value);

        string name = fields.GetValueOrDefault("name", "");
        if (name.Length == 0)
        {
            return null;
        }

        string level = fields.GetValueOrDefault("level", "");
        string duration = fields.GetValueOrDefault("duration", "");

        return new Spell
        {
            Key = key,
            Name = name,
            Level = level,
            LevelNumber = ReadLevelNumber(level),
            School = fields.GetValueOrDefault("school", ""),
            IsRitual = string.Equals(fields.GetValueOrDefault("ritual"), "true", StringComparison.OrdinalIgnoreCase),
            CastingTime = fields.GetValueOrDefault("time", ""),
            Range = fields.GetValueOrDefault("range", ""),
            Components = fields.GetValueOrDefault("components", ""),
            Duration = duration,
            IsConcentration = duration.StartsWith("Concentration", StringComparison.OrdinalIgnoreCase),
            Classes = ReadClasses(fields.GetValueOrDefault("classes", "")),
            Source = fields.GetValueOrDefault("source", ""),
            Description = CleanText(match.Groups["text"].Value),
        };
    }

    /// <summary>
    /// The block looks like YAML but is not parsed as YAML: the converter writes the values
    /// as they are, so "components: V, S, M (a pinch of salt: any kind)" would not be valid.
    /// Each line is simply split at its first colon.
    /// </summary>
    private static Dictionary<string, string> ReadFields(string block)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string line in block.Split('\n'))
        {
            int colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon > 0)
            {
                fields[line[..colon].Trim()] = ConverterLinks.Simplify(line[(colon + 1)..]).Trim();
            }
        }

        return fields;
    }

    /// <summary>"3rd-level" gives 3. A cantrip, or a level written some other way, gives 0.</summary>
    private static int ReadLevelNumber(string level)
    {
        Match match = LeadingNumber().Match(level);
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : 0;
    }

    /// <summary>
    /// The converter writes a link to each spell list that has the spell, separated by
    /// semicolons; with the links reduced to their text that is "Sorcerer; Wizard; Cleric (Light Domain)".
    /// Only the classes' own lists are kept. A subclass's list ("Cleric (Light Domain)") is left out,
    /// because filtering by Cleric should not show every spell one domain adds.
    /// </summary>
    private static List<string> ReadClasses(string classes)
    {
        return classes
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(name => !name.Contains('(', StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Prepares a note's text for display: links simplified (see <see cref="ConverterLinks"/>),
    /// Obsidian anchors removed, and runs of empty lines reduced to one.
    /// </summary>
    internal static string CleanText(string text)
    {
        string cleaned = ConverterLinks.Simplify(text.Replace("\r\n", "\n", StringComparison.Ordinal));

        // "^some-id" on a line of its own is an Obsidian anchor, not text.
        cleaned = BlockId().Replace(cleaned, "");

        // Removing things can leave runs of empty lines; one empty line is a paragraph break.
        return BlankLines().Replace(cleaned, "\n\n").Trim();
    }

    // The opening line, the field lines, the closing fence, then everything after it.
    [GeneratedRegex(@"```spell[^\n]*\r?\n(?<fields>.*?)\r?\n```[^\n]*(?<text>.*)", RegexOptions.Singleline)]
    private static partial Regex SpellNote();

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingNumber();

    [GeneratedRegex(@"^\^[\w-]+[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex BlockId();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLines();
}
