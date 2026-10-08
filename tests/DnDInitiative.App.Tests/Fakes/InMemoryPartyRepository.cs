using DnDInitiative.Core.Parties;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemoryPartyRepository : IPartyRepository
{
    private readonly List<Party> _parties = [];

    public InMemoryPartyRepository(params Party[] parties)
    {
        _parties.AddRange(parties);
    }

    public IReadOnlyList<Party> Parties => _parties;

    public Task<IReadOnlyList<Party>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Party>>(_parties.ToList());
    }

    public Task SaveAsync(Party party, CancellationToken cancellationToken = default)
    {
        _parties.RemoveAll(existing => existing.Id == party.Id);
        _parties.Add(party);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _parties.RemoveAll(existing => existing.Id == id);
        return Task.CompletedTask;
    }
}