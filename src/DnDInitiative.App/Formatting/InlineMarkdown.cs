using System.Text.RegularExpressions;

namespace DnDInitiative.App.Formatting;

public sealed record TextSegment(string Text, bool IsBold = false, bool IsItalic = false);

/// <summary>
/// Splits text containing Markdown emphasis (*italic*, **bold**, ***both***) into styled segments.
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

        int position = 0;
        foreach (Match match in Emphasis().Matches(text))
        {
            if (match.Index > position)
            {
                segments.Add(new TextSegment(text[position..match.Index]));
            }

            int markers = match.Groups[1].Length;
            segments.Add(new TextSegment(match.Groups[2].Value, IsBold: markers >= 2, IsItalic: markers != 2));
            position = match.Index + match.Length;
        }

        if (position < text.Length)
        {
            segments.Add(new TextSegment(text[position..]));
        }

        return segments;
    }

    // One to three asterisks, text that neither starts nor ends with a space
    // or an asterisk, then the same number of asterisks again.
    [GeneratedRegex(@"(\*{1,3})(?=[^\s*])(.+?)(?<=[^\s*])\1")]
    private static partial Regex Emphasis();
}