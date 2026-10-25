namespace DnDInitiative.Core.Spellbooks;

/// <summary>Which library a spellbook entry's key belongs to.</summary>
public enum SpellbookEntryKind
{
    Spell,
    Ability,
}

/// <summary>
/// One spell or ability in a spellbook. It holds the library key, not a copy of the text,
/// so a corrected import shows up in every spellbook that has the entry.
/// </summary>
public sealed record SpellbookEntry(SpellbookEntryKind Kind, string Key);

/// <summary>A set of spell and ability cards to print, kept in the campaign folder.</summary>
/// <param name="Entries">In the order the cards are laid out on the pages.</param>
/// <param name="Modified">
/// When the spellbook was last saved. Kept in its own file, like an encounter's, so it survives
/// a git checkout or a copy to another computer.
/// </param>
public sealed record Spellbook(
    Guid Id,
    string Name,
    IReadOnlyList<SpellbookEntry> Entries,
    DateTimeOffset? Modified = null);