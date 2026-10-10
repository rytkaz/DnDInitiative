using System.Text.RegularExpressions;

namespace DnDInitiative.App.Formatting;

/// <param name="Text">What is shown.</param>
/// <param name="LinkTarget">Where the text leads if it is a link, such as "spell:fireball"; otherwise null.</param>
public sealed record TextSegment(string Text, bool IsBold = false, bool IsItalic = false, string? LinkTarget = null);

/// <summary>
/// Splits text containing Markdown emphasis (*italic*, **bold**, ***both***) and links
/// ([text](target)) into styled segments.
/// </summary>
public static partial class InlineMarkdown
{
    public static IReadOnlyList<TextSegment> Parse(string? text)
    {
        var segments = new List<TextSegment>();
        if (string.IsNullOrEmpty(text))
        {
            return segments;
        }

        // Emphasis first, then links inside each piece. That order handles a bold or italic
        // run that contains links, which is how a creature's list of spells is written.
        int position = 0;
        foreach (Match match in Emphasis().Matches(text))
        {
            AddWithLinks(segments, text[position..match.Index], isBold: false, isItalic: false);

            int markers = match.Groups[1].Length;
            AddWithLinks(segments, match.Groups[2].Value, isBold: markers >= 2, isItalic: markers != 2);
            position = match.Index + match.Length;
        }

        AddWithLinks(segments, text[position..], isBold: false, isItalic: false);
        return segments;
    }

    private static void AddWithLinks(List<TextSegment> segments, string text, bool isBold, bool isItalic)
    {
        int position = 0;
        foreach (Match match in Link().Matches(text))
        {
            if (match.Index > position)
            {
                segments.Add(new TextSegment(text[position..match.Index], isBold, isItalic));
            }

            segments.Add(new TextSegment(match.Groups[1].Value, isBold, isItalic, LinkTarget: match.Groups[2].Value.Trim()));
            position = match.Index + match.Length;
        }

        if (position < text.Length)
        {
            segments.Add(new TextSegment(text[position..], isBold, isItalic));
        }
    }

    // One to three asterisks, text that neither starts nor ends with a space
    // or an asterisk, then the same number of asterisks again.
    [GeneratedRegex(@"(\*{1,3})(?=[^\s*])(.+?)(?<=[^\s*])\1")]
    private static partial Regex Emphasis();

    // [text](target)
    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex Link();
}
