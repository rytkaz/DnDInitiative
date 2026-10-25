using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemoryCustomSpellRepository : ICustomSpellRepository
{
    private readonly Dictionary<string, Spell> _spells = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(Spell spell, CancellationToken cancellationToken = default)
    {
        _spells[spell.Key] = spell with { IsCustom = true };
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_spells.Remove(key));
    }

    public Task<bool> ExistsInLibraryAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<Spell?> FindCustomAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_spells.GetValueOrDefault(key));
    }
}
