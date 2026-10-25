using System.Text.RegularExpressions;

namespace DnDInitiative.Core.Library;

/// <summary>Makes the keys of spells and abilities the user writes, the same way the importers make theirs.</summary>
public static partial class EntryKeys
{
    /// <summary>The name and the source in lower case, joined by hyphens: "arresting-strike-homebrew".</summary>
    public static string For(string name, string source)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(source);

        return $"{Slug(name)}-{Slug(source)}";
    }

    private static string Slug(string text)
    {
        return NotLetterOrDigit().Replace(text.ToLowerInvariant(), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NotLetterOrDigit();
}
