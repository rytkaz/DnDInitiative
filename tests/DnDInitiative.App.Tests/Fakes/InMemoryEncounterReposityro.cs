using DnDInitiative.Core.Encounters;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemoryEncounterRepository : IEncounterRepository
{
    private readonly List<Encounter> _encounters = [];

    public InMemoryEncounterRepository(params Encounter[] encounters)
    {
        _encounters.AddRange(encounters);
    }

    public IReadOnlyList<Encounter> Encounters => _encounters;

    public Task<IReadOnlyList<Encounter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Encounter>>(_encounters.ToList());
    }

    public Task<Encounter?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_encounters.Find(encounter => encounter.Id == id));
    }

    public Task SaveAsync(Encounter encounter, CancellationToken cancellationToken = default)
    {
        _encounters.RemoveAll(existing => existing.Id == encounter.Id);
        _encounters.Add(encounter);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _encounters.RemoveAll(existing => existing.Id == id);
        return Task.CompletedTask;
    }
}