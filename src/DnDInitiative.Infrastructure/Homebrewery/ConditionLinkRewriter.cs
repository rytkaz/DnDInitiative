using System.Text;
using System.Text.RegularExpressions;

using DnDInitiative.Core.Conditions;
using DnDInitiative.Infrastructure.Import;

namespace DnDInitiative.Infrastructure.Homebrewery;

/// <summary>
/// Makes condition names in homebrew text open the condition in the app. Stat blocks often
/// link condition words to the document that defines them, [Grapple](https://homebrewery...),
/// and those links are rewritten. Where the author wrote a condition's name without a link,
/// one is added.
/// </summary>
internal sealed partial class ConditionLinkRewriter
{
    // A link text and a condition name are taken to mean the same thing when they share at
    // least this many letters at the start...
    private const int MinimumSharedLetters = 4;

    // ...and differ by no more than this many letters at the end of the shorter one, so that
    // "Grapple" finds "Grappled" and "Frightens" finds "Frightened".
    private const int AllowedDifferentEnding = 2;

    private readonly HashSet<string> _documentIds;
    private readonly IReadOnlyList<string> _names;
    private readonly List<string> _namesLongestFirst;

    /// <param name="conditionDocumentIds">Share ids of the documents the conditions came from.</param>
    /// <param name="conditionNames">The names of the conditions links may point at: those the documents define and those already in the library.</param>
    public ConditionLinkRewriter(IEnumerable<string> conditionDocumentIds, IEnumerable<string> conditionNames)
    {
        ArgumentNullException.ThrowIfNull(conditionDocumentIds);
        ArgumentNullException.ThrowIfNull(conditionNames);

        // Share ids are case-sensitive.
        _documentIds = new HashSet<string>(conditionDocumentIds, StringComparer.Ordinal);
        _names = conditionNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Longest first, so "Temporary Hit Points" is found before a shorter name inside it.
        _namesLongestFirst = _names.OrderByDescending(name => name.Length).ToList();
    }

    /// <summary>Leaves every link as it is.</summary>
    public static ConditionLinkRewriter None { get; } = new([], []);

    /// <summary>
    /// Rewrites links into a conditions document as [text](condition:Name). A link whose text
    /// matches no condition is left pointing at the web page, which is still somewhere useful.
    /// </summary>
    public string Rewrite(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (_documentIds.Count == 0 || !text.Contains("](", StringComparison.Ordinal))
        {
            return text;
        }

        return WebLink().Replace(text, match =>
        {
            string? shareId = HomebreweryLinks.ShareId(match.Groups["address"].Value);
            if (shareId is null || !_documentIds.Contains(shareId))
            {
                return match.Value;
            }

            string linkText = match.Groups["text"].Value;
            return FindName(linkText, _names) is { } name
                ? $"[{linkText}]({ConverterLinks.ConditionScheme}{name})"
                : match.Value;
        });
    }

    /// <summary>
    /// Adds a condition link to every condition name written in the text without one.
    /// The standard conditions are found whatever their case, since older stat blocks write
    /// "knocked prone". Any other condition is found only when it is capitalised the way its
    /// name is: homebrew names such as Hidden or Turned are ordinary words too.
    /// Text that is already a link is left alone.
    /// </summary>
    /// <param name="text">Display text, after <see cref="Rewrite"/>.</param>
    /// <param name="except">A condition not to link, such as the one whose own text this is.</param>
    public string LinkNames(string text, string? except = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (_namesLongestFirst.Count == 0 || text.Length == 0)
        {
            return text;
        }

        var linked = new StringBuilder(text.Length);
        int position = 0;
        foreach (Match link in AnyLink().Matches(text))
        {
            AppendLinkedNames(linked, text.AsSpan(position, link.Index - position), except);
            linked.Append(link.Value);
            position = link.Index + link.Length;
        }

        AppendLinkedNames(linked, text.AsSpan(position), except);
        return linked.ToString();
    }

    private void AppendLinkedNames(StringBuilder linked, ReadOnlySpan<char> text, string? except)
    {
        int index = 0;
        while (index < text.Length)
        {
            bool startsWord = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            if (startsWord && NameAt(text, index, except) is { } name)
            {
                linked.Append('[').Append(text.Slice(index, name.Length)).Append("](")
                    .Append(ConverterLinks.ConditionScheme).Append(name).Append(')');
                index += name.Length;
                continue;
            }

            linked.Append(text[index]);
            index++;
        }
    }

    private string? NameAt(ReadOnlySpan<char> text, int index, string? except)
    {
        foreach (string name in _namesLongestFirst)
        {
            int end = index + name.Length;
            if (end > text.Length || (end < text.Length && char.IsLetterOrDigit(text[end])))
            {
                continue;
            }

            bool isStandard = StandardConditions.IsStandard(name);
            StringComparison comparison = isStandard ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!text.Slice(index, name.Length).Equals(name, comparison)
                || string.Equals(name, except, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // "Burning Lash" is the name of an attack, not the Burning condition. A homebrew
            // name followed by another capitalised word is taken to be part of a longer name.
            if (!isStandard && end + 1 < text.Length && text[end] == ' ' && char.IsUpper(text[end + 1]))
            {
                continue;
            }

            return name;
        }

        return null;
    }

    /// <summary>
    /// Finds the condition a link's text refers to. The text is often another form of the
    /// name ("Grapple" for Grappled, "Bleed" for Bleeding), so a close match is accepted when
    /// there is no exact one. Failing that, each word is tried on its own, so "lose a limb"
    /// finds Limb Damage. Returns null when nothing is close enough.
    /// </summary>
    internal static string? FindName(string linkText, IReadOnlyList<string> names)
    {
        ArgumentNullException.ThrowIfNull(linkText);
        ArgumentNullException.ThrowIfNull(names);

        if (ClosestName(Letters(linkText), names) is { } name)
        {
            return name;
        }

        return linkText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(Letters)
            .Where(word => word.Length >= MinimumSharedLetters)
            .Select(word => ClosestName(word, names))
            .FirstOrDefault(match => match is not null);
    }

    private static string? ClosestName(string wanted, IReadOnlyList<string> names)
    {
        if (wanted.Length == 0)
        {
            return null;
        }

        string? best = null;
        int bestShared = 0;

        foreach (string name in names)
        {
            string candidate = Letters(name);
            if (string.Equals(candidate, wanted, StringComparison.Ordinal))
            {
                return name;
            }

            int shared = SharedStart(candidate, wanted);
            int shorter = Math.Min(candidate.Length, wanted.Length);
            if (shared >= MinimumSharedLetters && shared >= shorter - AllowedDifferentEnding && shared > bestShared)
            {
                best = name;
                bestShared = shared;
            }
        }

        return best;
    }

    // Lower-case letters only, so "Prone," and "prone" compare equal.
    private static string Letters(string text)
    {
        return new string(text.Where(char.IsLetter).Select(char.ToLowerInvariant).ToArray());
    }

    private static int SharedStart(string first, string second)
    {
        int length = 0;
        while (length < first.Length && length < second.Length && first[length] == second[length])
        {
            length++;
        }

        return length;
    }

    // Any Markdown link or image, whatever it points at.
    [GeneratedRegex(@"!?\[[^\]]*\]\([^)]*\)")]
    private static partial Regex AnyLink();

    // [text](https://...), with an optional title after the address.
    [GeneratedRegex(@"\[(?<text>[^\]]+)\]\((?<address>https?://[^)\s]+)[^)]*\)")]
    private static partial Regex WebLink();
}
