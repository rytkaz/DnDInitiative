namespace DnDInitiative.Core.Spells;

/// <summary>How many spells a save added, and how many it replaced.</summary>
public sealed record SpellSaveResult(int Added, int Updated);

public interface ISpellRepository
{
    /// <summary>
    /// Finds the spell a link refers to. The reference is tried as a key first ("fireball-xphb"),
    /// then as a name ("Fireball"), so links written by hand work as well as imported ones.
    /// Upper and lower case are not distinguished.
    /// </summary>
    /// <returns>The spell, or null if there is none.</returns>
    Task<Spell?> FindAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Adds the spells, replacing any stored spell that has the same key.</summary>
    Task<SpellSaveResult> SaveManyAsync(IReadOnlyCollection<Spell> spells, CancellationToken cancellationToken = default);
}
