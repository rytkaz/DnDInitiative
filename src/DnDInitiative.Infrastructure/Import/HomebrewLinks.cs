using System.Text.RegularExpressions;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Homebrew files link to web pages with a tag: {@link Grappled|https://example.com/rules}.
/// The converter turns that into "Grappled (https://example.com/rules)", after which nothing
/// can tell where the linked words began. Rewriting the tag as a Markdown link before the
/// converter sees it keeps the two parts apart, and the converter passes Markdown through.
/// </summary>
internal static partial class HomebrewLinks
{
    /// <summary>Returns the file's text with its web link tags rewritten as [text](address).</summary>
    public static string ToMarkdown(string homebrewJson)
    {
        ArgumentNullException.ThrowIfNull(homebrewJson);

        return homebrewJson.Contains("{@link ", StringComparison.Ordinal)
            ? WebLinkTag().Replace(homebrewJson, "[${text}](${url})")
            : homebrewJson;
    }

    // Deliberately narrow, because this edits a JSON file as plain text:
    // - the text may not contain braces (another tag inside it), brackets or a pipe;
    // - the address must be a web address without spaces or parentheses, which would end
    //   a Markdown link early;
    // - neither may contain a quote or a backslash, so no JSON escape is ever cut in half.
    // A tag that does not fit is left alone, for the converter to handle its own way.
    [GeneratedRegex(@"\{@link (?<text>[^{}|\[\]""\\]+)\|(?<url>https?://[^{}|\s()""\\]+)\}")]
    private static partial Regex WebLinkTag();
}
