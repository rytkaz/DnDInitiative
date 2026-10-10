using System.Text.RegularExpressions;

namespace DnDInitiative.App.Formatting;

public enum MarkdownBlockKind
{
    Paragraph,
    Heading,
    Bullet,
}

/// <summary>One block of a longer text. Its text may still contain emphasis and links.</summary>
public sealed record MarkdownBlock(MarkdownBlockKind Kind, string Text);

/// <summary>
/// Splits a multi-paragraph Markdown text into blocks: paragraphs, headings and list items.
/// It knows only what the converter writes for spells; it is not a full Markdown parser.
/// </summary>
public static partial class MarkdownBlocks
{
    public static IReadOnlyList<MarkdownBlock> Parse(string? text)
    {
        var blocks = new List<MarkdownBlock>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return blocks;
        }

        // The lines of the paragraph being collected. Markdown lets one paragraph run over several lines.
        var paragraph = new List<string>();

        void EndParagraph()
        {
            if (paragraph.Count > 0)
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Paragraph, string.Join(' ', paragraph)));
                paragraph.Clear();
            }
        }

        foreach (string rawLine in text.Split('\n'))
        {
            // "> " in front marks a quotation or an Obsidian callout. Only the content matters here.
            string line = QuoteMarkers().Replace(rawLine, "").Trim();

            if (line.Length == 0)
            {
                EndParagraph();
            }
            else if (Callout().Match(line) is { Success: true } callout)
            {
                // "[!note] A title": the title becomes a heading, if there is one.
                EndParagraph();
                AddIfNotEmpty(blocks, MarkdownBlockKind.Heading, callout.Groups[1].Value);
            }
            else if (Heading().Match(line) is { Success: true } heading)
            {
                EndParagraph();
                AddIfNotEmpty(blocks, MarkdownBlockKind.Heading, heading.Groups[1].Value);
            }
            else if (Bullet().Match(line) is { Success: true } bullet)
            {
                EndParagraph();
                AddIfNotEmpty(blocks, MarkdownBlockKind.Bullet, bullet.Groups[1].Value);
            }
            else if (TableRule().IsMatch(line) || line.Trim('-').Length == 0)
            {
                // The |---|---| line under a table's heading row, or a "---" divider. Nothing to show.
                EndParagraph();
            }
            else if (line.StartsWith('|'))
            {
                // A table row. Shown as one line with its cells side by side; good enough for
                // the small tables some spells have.
                EndParagraph();
                IEnumerable<string> cells = line.Trim('|').Split('|').Select(cell => cell.Trim());
                AddIfNotEmpty(blocks, MarkdownBlockKind.Paragraph, string.Join("  |  ", cells));
            }
            else if (NumberedItem().IsMatch(line))
            {
                // Kept with its number, on a line of its own.
                EndParagraph();
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Paragraph, line));
            }
            else
            {
                paragraph.Add(line);
            }
        }

        EndParagraph();
        return blocks;
    }

    private static void AddIfNotEmpty(List<MarkdownBlock> blocks, MarkdownBlockKind kind, string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length > 0)
        {
            blocks.Add(new MarkdownBlock(kind, trimmed));
        }
    }

    [GeneratedRegex(@"^(\s*>)+")]
    private static partial Regex QuoteMarkers();

    // [!note], [!tip]- and so on, optionally followed by a title.
    [GeneratedRegex(@"^\[!\w+\][+-]?(.*)$")]
    private static partial Regex Callout();

    // One to six # signs, a space, the heading, and optionally closing # signs.
    [GeneratedRegex(@"^#{1,6}\s+(.*?)\s*#*$")]
    private static partial Regex Heading();

    // "- item", "* item" or "+ item". The space matters: "*italic*" is not a list item.
    [GeneratedRegex(@"^[-*+]\s+(.*)$")]
    private static partial Regex Bullet();

    [GeneratedRegex(@"^\|?[\s:-]*-[\s:|-]*\|[\s:|-]*$")]
    private static partial Regex TableRule();

    [GeneratedRegex(@"^\d+[.)]\s+")]
    private static partial Regex NumberedItem();
}
