using System.Globalization;
using System.Text;

namespace DnDInitiative.Infrastructure.Campaigns;

internal static class FileNames
{
    private const int MaxSlugLength = 60;

    // Names Windows will not accept for a file, whatever the extension.
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
    };

    /// <summary>Turns a display name into a safe, readable file name: "Goblin Ambush!" becomes "goblin-ambush".</summary>
    public static string Slug(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var builder = new StringBuilder();
        foreach (char character in name)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                // Any run of spaces and punctuation becomes a single dash.
                builder.Append('-');
            }

            if (builder.Length >= MaxSlugLength)
            {
                break;
            }
        }

        string slug = builder.ToString().Trim('-');
        if (slug.Length == 0)
        {
            return "unnamed";
        }

        return Reserved.Contains(slug) ? slug + "-item" : slug;
    }

    /// <summary>Adds ".json", and a number if another file already has that name.</summary>
    public static string UniqueFileName(string slug, IReadOnlySet<string> takenFileNames)
    {
        ArgumentNullException.ThrowIfNull(takenFileNames);

        string candidate = slug + ".json";
        for (int number = 2; takenFileNames.Contains(candidate); number++)
        {
            candidate = string.Create(CultureInfo.InvariantCulture, $"{slug}-{number}.json");
        }

        return candidate;
    }
}