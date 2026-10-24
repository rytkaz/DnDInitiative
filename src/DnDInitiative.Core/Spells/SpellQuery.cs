namespace DnDInitiative.Core.Spells;

/// <summary>
/// What to look for in the spell library. Every condition left at its default matches
/// every spell, so an empty query lists the whole library.
/// </summary>
public sealed record SpellQuery
{
    /// <summary>Part of the name, in any case. Empty matches every name.</summary>
    public string Text { get; init; } = "";

    /// <summary>A class whose spell list must have the spell, such as "Wizard". Null for any class.</summary>
    public string? Class { get; init; }

    /// <summary>0 for cantrips, otherwise 1 to 9. Null for any level.</summary>
    public int? Level { get; init; }

    /// <summary>True for rituals only, false for spells that are not rituals, null for both.</summary>
    public bool? IsRitual { get; init; }

    /// <summary>True for concentration spells only, false for spells without it, null for both.</summary>
    public bool? IsConcentration { get; init; }

    /// <summary>The source the spell was imported from, as stored, such as "XPHB p. 274". Null for any source.</summary>
    public string? Source { get; init; }
}

/// <summary>The fields a spell list needs, so it does not have to load every spell's text.</summary>
public sealed record SpellSummary(
    string Key,
    string Name,
    int LevelNumber,
    string School,
    bool IsRitual,
    bool IsConcentration,
    IReadOnlyList<string> Classes,
    string Source);
