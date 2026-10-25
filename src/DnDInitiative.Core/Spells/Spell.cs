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

    /// <summary>The level as a number, for sorting and filtering: 0 for a cantrip, otherwise 1 to 9.</summary>
    public int LevelNumber { get; init; }

    public string School { get; init; } = "";

    public bool IsRitual { get; init; }

    public string CastingTime { get; init; } = "";

    public string Range { get; init; } = "";

    public string Components { get; init; } = "";

    public string Duration { get; init; } = "";

    /// <summary>Whether casting it takes concentration, as the duration says: "Concentration, up to 1 minute".</summary>
    public bool IsConcentration { get; init; }

    /// <summary>
    /// The classes whose spell list has it, such as "Sorcerer" and "Wizard". Empty for spells
    /// imported before classes were recorded, until they are imported again.
    /// </summary>
    public IReadOnlyList<string> Classes { get; init; } = [];

    public string Source { get; init; } = "";

    /// <summary>True for a spell the user wrote or changed, which is kept in the campaign folder and not in the library.</summary>
    public bool IsCustom { get; init; }

    /// <summary>The rules text, in Markdown. May run to several paragraphs.</summary>
    public string Description { get; init; } = "";
}
