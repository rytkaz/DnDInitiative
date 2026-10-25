namespace DnDInitiative.Core.Abilities;

/// <summary>
/// A class feature chosen from a list, such as a martial exploit: like a spell, but with a
/// degree instead of a level and prerequisites instead of components.
/// </summary>
public sealed record Ability
{
    /// <summary>What the category of martial exploits is called. The only category imported so far.</summary>
    public const string MartialExploit = "Martial Exploit";

    /// <summary>
    /// The name and the source, in lower case and joined by hyphens, such as
    /// "arresting-strike-alternate-fighter". The same on every import, so importing a document
    /// again updates its abilities instead of adding them twice.
    /// </summary>
    public required string Key { get; init; }

    public required string Name { get; init; }

    /// <summary>What kind of ability it is, such as <see cref="MartialExploit"/>.</summary>
    public string Category { get; init; } = MartialExploit;

    /// <summary>1 to 5. An exploit of a higher degree needs a higher character level.</summary>
    public int Degree { get; init; }

    /// <summary>As the document words it, such as "5th-level, Strength of 13". Empty when there are none.</summary>
    public string Prerequisites { get; init; } = "";

    /// <summary>The classes that can learn it, such as "Fighter".</summary>
    public IReadOnlyList<string> Classes { get; init; } = [];

    /// <summary>The document it came from, such as "Alternate Fighter".</summary>
    public string Source { get; init; } = "";

    /// <summary>True for an ability the user wrote or changed, which is kept in the campaign folder and not in the library.</summary>
    public bool IsCustom { get; init; }

    /// <summary>The rules text, in Markdown. May run to several paragraphs and lists.</summary>
    public string Description { get; init; } = "";
}
