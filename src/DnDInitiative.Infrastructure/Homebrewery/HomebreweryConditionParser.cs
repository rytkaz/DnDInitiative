using System.Text.RegularExpressions;

using DnDInitiative.Core.Conditions;

namespace DnDInitiative.Infrastructure.Homebrewery;

/// <summary>
/// Reads a Homebrewery document that defines conditions, such as an "Appendix C: Conditions".
/// Each condition is a section under a "### Name" heading; the text runs to the next such heading.
/// </summary>
internal static partial class HomebreweryConditionParser
{
    /// <summary>
    /// Whether the document is a list of conditions rather than, say, a bestiary: its title,
    /// the first "# " heading, ends with "Conditions".
    /// </summary>
    public static bool IsConditionsDocument(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        foreach (string line in HomebreweryText.Lines(markdown))
        {
            if (Title().Match(line.Trim()) is { Success: true } title)
            {
                return title.Groups["title"].Value.Trim().EndsWith("Conditions", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    /// <param name="markdown">The document's Markdown source.</param>
    /// <param name="source">What to file the conditions under.</param>
    public static IReadOnlyList<Condition> Parse(string markdown, string source)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(source);

        var conditions = new List<Condition>();
        string? heading = null;
        var body = new List<string>();

        void EndSection()
        {
            if (heading is null)
            {
                return;
            }

            string description = Clean(body);
            if (description.Length == 0)
            {
                return;
            }

            // "Flanked/Flanking" is two names for one set of rules. Each gets its own entry,
            // so a link to either finds it.
            foreach (string name in heading.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                conditions.Add(new Condition { Name = name, Source = source.Trim(), Description = description });
            }
        }

        // The table of contents sits inside the first section; it is a list of links, not rules.
        string withoutContents = TableOfContents().Replace(markdown, "");

        foreach (string line in HomebreweryText.Lines(withoutContents))
        {
            if (SectionHeading().Match(line.Trim()) is { Success: true } section)
            {
                EndSection();
                heading = HomebreweryText.CollapseSpaces(HomebreweryText.Display(section.Groups["name"].Value, ConditionLinkRewriter.None));
                body.Clear();
                continue;
            }

            // A "# " or "## " heading closes the section without starting another.
            if (heading is not null && HigherHeading().IsMatch(line.Trim()))
            {
                EndSection();
                heading = null;
                body.Clear();
                continue;
            }

            if (heading is not null)
            {
                body.Add(line);
            }
        }

        EndSection();
        return conditions;
    }

    private static string Clean(List<string> lines)
    {
        IEnumerable<string> kept = lines
            .Select(line => line.TrimEnd())
            .Select(line => HomebreweryText.IsLayout(line) ? "" : HomebreweryText.WithoutInlineBlocks(line));

        string text = HomebreweryText.Display(string.Join('\n', kept), ConditionLinkRewriter.None);

        // Removing layout lines and tags leaves gaps; one empty line is a paragraph break.
        return BlankLines().Replace(EmptyQuoteLines().Replace(text, ""), "\n\n").Trim();
    }

    [GeneratedRegex(@"^#[ \t]+(?<title>.+)$")]
    private static partial Regex Title();

    // Exactly three # signs: "##### Confused Behavior", a table's caption, belongs to the condition.
    [GeneratedRegex(@"^###[ \t]+(?<name>[^#].*?)[ \t]*$")]
    private static partial Regex SectionHeading();

    [GeneratedRegex(@"^#{1,2}[ \t]+\S")]
    private static partial Regex HigherHeading();

    [GeneratedRegex(@"<div[^>]*class\s*=\s*['""]toc['""][^>]*>.*?</div>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex TableOfContents();

    // A ">" with nothing after it, left where a quoted block had a blank line or a removed tag.
    [GeneratedRegex(@"^[ \t]*>[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex EmptyQuoteLines();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLines();
}
