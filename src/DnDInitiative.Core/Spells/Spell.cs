namespace DnDInitiative.Core.Spells;

/// <summary>A spell as it is shown to the user. Everything except the key is display text.</summary>
public sealed record Spell
{
    /// <summary>
    /// What links point at: the name of the note the converter wrote for the spell, without
    /// its extension, such as "fireball" or "fireball-xphb". Always lower case.
    /// </summary>
    public required string Key { get; init; }

    public required string Name { get; init; }

    /// <summary>Kept as the converter words it: "cantrip", "1st-level", "3rd-level".</summary>
    public string Level { get; init; } = "";

    public string School { get; init; } = "";

    public bool IsRitual { get; init; }

    public string CastingTime { get; init; } = "";

    public string Range { get; init; } = "";

    public string Components { get; init; } = "";

    public string Duration { get; init; } = "";

    public string Source { get; init; } = "";

    /// <summary>The rules text, in Markdown. May run to several paragraphs.</summary>
    public string Description { get; init; } = "";
}
