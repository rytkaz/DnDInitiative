using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemorySpellRepository : ISpellRepository
{
    private readonly Dictionary<string, Spell> _spells = new(StringComparer.OrdinalIgnoreCase);

    public InMemorySpellRepository(params Spell[] spells)
    {
        foreach (Spell spell in spells)
        {
            _spells[spell.Key] = spell;
        }
    }

    /// <summary>When set, lookups wait for it, so a test can decide the order they finish in.</summary>
    public Dictionary<string, TaskCompletionSource> Gates { get; } = new(StringComparer.OrdinalIgnoreCase);

    public async Task<Spell?> FindAsync(string reference, CancellationToken cancellationToken = default)
    {
        string key = reference.Trim();
        if (Gates.TryGetValue(key, out TaskCompletionSource? gate))
        {
            await gate.Task;
        }

        return _spells.GetValueOrDefault(key)
            ?? _spells.Values.FirstOrDefault(spell => string.Equals(spell.Name, key, StringComparison.OrdinalIgnoreCase));
    }

    public Task<SpellSaveResult> SaveManyAsync(IReadOnlyCollection<Spell> spells, CancellationToken cancellationToken = default)
    {
        foreach (Spell spell in spells)
        {
            _spells[spell.Key] = spell;
        }

        return Task.FromResult(new SpellSaveResult(spells.Count, 0));
    }
}
