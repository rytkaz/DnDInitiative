using System.Text.RegularExpressions;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Deals with the Markdown links the converter puts in its text. They point at other notes
/// in an Obsidian vault, which this app does not have.
/// </summary>
internal static partial class ConverterLinks
{
    /// <summary>What a link to a spell starts with once it has been rewritten: "spell:fireball-xphb".</summary>
    public const string SpellScheme = "spell:";

    /// <summary>
    /// Rewrites links to spells so they carry the spell's key, as in [fireball](spell:fireball-xphb),
    /// and reduces every other link to its text. Embedded images are removed.
    /// </summary>
    public static string Simplify(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Nearly all text has no link in it; skip the regular expression for those.
        return text.Contains("](", StringComparison.Ordinal) ? MarkdownLink().Replace(text, Rewrite) : text;
    }

    private static string Rewrite(Match match)
    {
        // "![alt](picture.png)" is an image. There is nothing to show for it.
        if (match.Groups["image"].Success)
        {
            return "";
        }

        string linkText = match.Groups["text"].Value;

        return SpellKey(match.Groups["target"].Value) is { } key
            ? $"[{linkText}]({SpellScheme}{key})"
            : linkText;
    }

    /// <summary>
    /// Returns the key of the spell a link target points at, or null if it points at something else.
    /// The converter keeps spells in a "spells" folder, one note per spell, so the target
    /// "compendium/spells/fireball-xphb.md" gives "fireball-xphb".
    /// </summary>
    private static string? SpellKey(string target)
    {
        // A target can be followed by a heading ("#Higher levels") or a title (' "Fireball"').
        int end = target.AsSpan().IndexOfAny('#', ' ');
        string path = end < 0 ? target : target[..end];

        Match match = SpellNotePath().Match(path);
        return match.Success ? match.Groups["key"].Value.ToLowerInvariant() : null;
    }

    // [text](target), with an optional "!" in front that makes it an image.
    [GeneratedRegex(@"(?<image>!)?\[(?<text>[^\]]+)\]\((?<target>[^)]*)\)")]
    private static partial Regex MarkdownLink();

    // "spells" must be a whole folder name: at the start of the path or right after a slash.
    [GeneratedRegex(@"(?:^|/)spells/(?<key>[^/]+)\.md$", RegexOptions.IgnoreCase)]
    private static partial Regex SpellNotePath();
}
