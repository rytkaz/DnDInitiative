using System.Net;
using System.Text.RegularExpressions;

using DnDInitiative.Infrastructure.Import;

namespace DnDInitiative.Infrastructure.Homebrewery;

/// <summary>
/// Strips what the Homebrewery adds to Markdown for page layout: page and column breaks, spacer
/// lines, {{...}} style blocks, comments and HTML. What is left is text this app can show.
/// </summary>
internal static partial class HomebreweryText
{
    /// <summary>The document's lines, with comments removed first because they can span lines.</summary>
    public static IReadOnlyList<string> Lines(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        string text = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (text.Contains("<!--", StringComparison.Ordinal))
        {
            text = Comment().Replace(text, "");
        }

        return text.Split('\n');
    }

    /// <summary>
    /// Whether a line is only there for the layout: "\page" or "\column", a ":" spacer, a "___"
    /// rule, a line of backticks, or the opening or closing line of a {{...}} block.
    /// </summary>
    public static bool IsLayout(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return LayoutLine().IsMatch(line.Trim());
    }

    /// <summary>
    /// Removes {{...}} spans that open and close on one line, such as "{{width:10% }}". A span
    /// that only styles its text, such as "{{font-variant:small-caps Bob}}", keeps the text.
    /// </summary>
    public static string WithoutInlineBlocks(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Spans can be nested, as in "{{bonus {{x}}}}". Removing the innermost ones until none
        // are left takes care of that. A span with a class, such as "{{bonus **cr 1/4**}}", is
        // decoration the parsers read separately, so only style-only spans keep their text.
        string previous;
        do
        {
            previous = text;
            text = InlineBlock().Replace(text, TextOfStyledSpan);
        }
        while (!string.Equals(previous, text, StringComparison.Ordinal));

        return text;
    }

    private static string TextOfStyledSpan(Match span) =>
        StyledSpan().Match(span.Value) is { Success: true } styled ? styled.Groups["text"].Value : "";

    /// <summary>
    /// Prepares text for display: HTML and images removed, web links kept, links to a conditions
    /// document turned into condition links, and entities such as "&amp;nbsp;" written out.
    /// </summary>
    public static string Display(string text, ConditionLinkRewriter links)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(links);

        string simplified = links.Rewrite(ConverterLinks.Simplify(text));

        // Decoded last, so an "&lt;" in the text is not mistaken for the start of a tag.
        return WebUtility.HtmlDecode(simplified).Replace(' ', ' ');
    }

    /// <summary>Replaces runs of spaces and tabs with one space.</summary>
    public static string CollapseSpaces(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Spaces().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex Comment();

    // \page, \column, \pagebreakNum; ":" or "::" spacers; ___ and --- rules; ``` fences;
    // "{{monster,frame" without its closing braces; a lone "}}".
    [GeneratedRegex(@"^(?:\\[A-Za-z]+|:+|_{3,}|-{3,}|`+|\{\{[^}]*|\}\}+)$")]
    private static partial Regex LayoutLine();

    // "{{font-variant:small-caps,color:red text}}": CSS declarations only, separated by commas,
    // then a space and the text. Quoted values may hold spaces, as in font-family:"Times New Roman".
    [GeneratedRegex(@"^\{\{[A-Za-z-]+:(?:""[^""]*""|[^\s,{}""]+)(?:,[A-Za-z-]+:(?:""[^""]*""|[^\s,{}""]+))*(?:\s(?<text>[^{}]*))?\}\}$")]
    private static partial Regex StyledSpan();

    [GeneratedRegex(@"\{\{[^{}]*\}\}")]
    private static partial Regex InlineBlock();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex Spaces();
}
