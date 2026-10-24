namespace DnDInitiative.Infrastructure.Persistence;

/// <summary>Builds patterns for SQL's LIKE from text a user typed.</summary>
internal static class LikePattern
{
    /// <summary>The escape character the patterns use. Pass it to EF.Functions.Like with the pattern.</summary>
    public const string Escape = "\\";

    /// <summary>A pattern that matches any text containing <paramref name="text"/>.</summary>
    public static string Containing(string text)
    {
        return "%" + EscapeWildcards(text) + "%";
    }

    // In a LIKE pattern, % and _ are wildcards. Typed by a user, they should match themselves.
    private static string EscapeWildcards(string text)
    {
        return text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
