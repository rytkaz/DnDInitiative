namespace DnDInitiative.Core.Conditions;

public interface IConditionRepository
{
    /// <summary>Finds a condition by name, ignoring case and surrounding spaces.</summary>
    /// <returns>The condition, or null if there is none.</returns>
    Task<Condition?> FindAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>The names of every stored condition.</summary>
    Task<IReadOnlyList<string>> GetNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds the conditions, replacing any stored condition with the same name.</summary>
    Task SaveManyAsync(IReadOnlyCollection<Condition> conditions, CancellationToken cancellationToken = default);
}
