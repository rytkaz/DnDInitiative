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

    public Task<IReadOnlyList<SpellSummary>> SearchAsync(
        SpellQuery query,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SpellSummary> matches = _spells.Values
            .Where(spell => spell.Name.Contains(query.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(spell => query.Class is null || spell.Classes.Contains(query.Class))
            .Where(spell => query.Level is null || spell.LevelNumber == query.Level)
            .Where(spell => query.IsRitual is null || spell.IsRitual == query.IsRitual)
            .Where(spell => query.IsConcentration is null || spell.IsConcentration == query.IsConcentration)
            .Where(spell => query.Source is null || spell.Source == query.Source)
            .OrderBy(spell => spell.LevelNumber)
            .ThenBy(spell => spell.Name, StringComparer.OrdinalIgnoreCase)
            .Skip(skip)
            .Take(take)
            .Select(spell => new SpellSummary(
                spell.Key,
                spell.Name,
                spell.LevelNumber,
                spell.School,
                spell.IsRitual,
                spell.IsConcentration,
                spell.Classes,
                spell.Source,
                spell.CastingTime,
                spell.Range))
            .ToList();

        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> classes = _spells.Values
            .SelectMany(spell => spell.Classes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(classes);
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
