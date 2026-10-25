using DnDInitiative.Core.Spellbooks;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemorySpellbookRepository : ISpellbookRepository
{
    private readonly List<Spellbook> _spellbooks = [];

    public InMemorySpellbookRepository(params Spellbook[] spellbooks)
    {
        _spellbooks.AddRange(spellbooks);
    }

    public IReadOnlyList<Spellbook> Spellbooks => _spellbooks;

    public Task<IReadOnlyList<Spellbook>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Spellbook>>(_spellbooks.ToList());
    }

    public Task SaveAsync(Spellbook spellbook, CancellationToken cancellationToken = default)
    {
        _spellbooks.RemoveAll(existing => existing.Id == spellbook.Id);
        _spellbooks.Add(spellbook);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _spellbooks.RemoveAll(existing => existing.Id == id);
        return Task.CompletedTask;
    }
}