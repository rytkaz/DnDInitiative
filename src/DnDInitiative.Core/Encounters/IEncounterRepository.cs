namespace DnDInitiative.Core.Encounters;

public interface IEncounterRepository
{
    Task<IReadOnlyList<Encounter>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Encounter?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds the encounter, or replaces the stored one with the same Id.</summary>
    Task SaveAsync(Encounter encounter, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}