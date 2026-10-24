using System.Text.RegularExpressions;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Deals with the Markdown links the converter puts in its text. They point at other notes
/// in an Obsidian vault, which this app does not have. It also removes the HTML tags the
/// converter uses here and there, which this app would show as they are written.
/// </summary>
internal static partial class ConverterLinks
{
    /// <summary>What a link to a spell starts with once it has been rewritten: "spell:fireball-xphb".</summary>
    public const string SpellScheme = "spell:";

    /// <summary>What a link to a condition starts with once it has been rewritten: "condition:Prone".</summary>
    public const string ConditionScheme = "condition:";

    /// <summary>
    /// Rewrites links to spells so they carry the spell's key, as in [fireball](spell:fireball-xphb),
    /// and links to conditions so they carry its name, as in [prone](condition:Prone). Keeps links to web pages, and reduces every other link to its text, since it points at
    /// a note this app does not have. Embedded images are removed, and so are HTML
    /// tags: &lt;span title='...'&gt;SRD&lt;/span&gt; becomes SRD.
    /// </summary>
    public static string Simplify(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Nearly all text has neither in it; skip the regular expressions for those.
        if (text.Contains("](", StringComparison.Ordinal))
        {
            text = MarkdownLink().Replace(text, Rewrite);
        }

        if (text.Contains('<', StringComparison.Ordinal))
        {
            text = HtmlTag().Replace(text, "");
        }

        return text;
    }

    private static string Rewrite(Match match)
    {
        // "![alt](picture.png)" is an image. There is nothing to show for it.
        if (match.Groups["image"].Success)
        {
            return "";
        }

        string linkText = match.Groups["text"].Value;

        string target = match.Groups["target"].Value;

        // Checked first: a web page whose address happens to end in "spells/x.md" is still a web page.
        if (WebAddress(target) is { } address)
        {
            return $"[{linkText}]({address})";
        }

        if (SpellKey(target) is { } key)
        {
            return $"[{linkText}]({SpellScheme}{key})";
        }

        return ConditionName(target) is { } name ? $"[{linkText}]({ConditionScheme}{name})" : linkText;
    }

    /// <summary>
    /// Returns the name of the condition a link target points at, or null if it points at something else.
    /// The converter keeps every condition in one note, one heading each, and links to the heading:
    /// "rules/conditions.md#Blinded". With rules split into separate notes the note is
    /// "rules/conditions/conditions.md", with the same heading.
    /// </summary>
    private static string? ConditionName(string target)
    {
        // A title may follow the target after a space; the heading has its spaces written as %20.
        int space = target.IndexOf(' ', StringComparison.Ordinal);
        string link = space < 0 ? target : target[..space];

        int hash = link.IndexOf('#', StringComparison.Ordinal);
        if (hash < 0 || !ConditionNotePath().IsMatch(link[..hash]))
        {
            return null;
        }

        string name = Uri.UnescapeDataString(link[(hash + 1)..]).Trim();
        return name.Length > 0 ? name : null;
    }

    /// <summary>Returns the address if the target is a web page, without any title that follows it.</summary>
    private static string? WebAddress(string target)
    {
        int end = target.IndexOf(' ', StringComparison.Ordinal);
        string address = (end < 0 ? target : target[..end]).Trim();

        return address.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || address.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? address
                : null;
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

    // An opening or closing tag: "<", an optional "/", a name that starts with a letter, then
    // anything up to ">". The letter keeps "if it is < 5 and > 2" from being taken for a tag.
    [GeneratedRegex(@"</?[A-Za-z][^<>]*>")]
    private static partial Regex HtmlTag();

    // "conditions.md" as a whole file name, optionally inside a "conditions" folder.
    [GeneratedRegex(@"(?:^|/)(?:conditions/)?conditions\.md$", RegexOptions.IgnoreCase)]
    private static partial Regex ConditionNotePath();

    // "spells" must be a whole folder name: at the start of the path or right after a slash.
    [GeneratedRegex(@"(?:^|/)spells/(?<key>[^/]+)\.md$", RegexOptions.IgnoreCase)]
    private static partial Regex SpellNotePath();
}
