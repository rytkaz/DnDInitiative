namespace DnDInitiative.Core.Abilities;

/// <summary>How many abilities a save added, and how many it replaced.</summary>
public sealed record AbilitySaveResult(int Added, int Updated);

/// <summary>
/// What to look for in the ability library. Every condition left at its default matches
/// every ability, so an empty query lists the whole library.
/// </summary>
public sealed record AbilityQuery
{
    /// <summary>Part of the name, in any case. Empty matches every name.</summary>
    public string Text { get; init; } = "";

    /// <summary>Such as <see cref="Ability.MartialExploit"/>. Null for any category.</summary>
    public string? Category { get; init; }

    /// <summary>A class that must be able to learn the ability, such as "Fighter". Null for any class.</summary>
    public string? Class { get; init; }

    /// <summary>1 to 5. Null for any degree.</summary>
    public int? Degree { get; init; }

    /// <summary>The document the ability came from, such as "Alternate Fighter". Null for any source.</summary>
    public string? Source { get; init; }
}

/// <summary>The fields an ability list needs, so it does not have to load every ability's text.</summary>
public sealed record AbilitySummary(
    string Key,
    string Name,
    string Category,
    int Degree,
    string Prerequisites,
    IReadOnlyList<string> Classes,
    string Source);

public interface IAbilityRepository
{
    /// <summary>Finds an ability by its key. Upper and lower case are not distinguished.</summary>
    /// <returns>The ability, or null if there is none.</returns>
    Task<Ability?> FindAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the abilities that match the query, ordered by degree and then by name. Skips the
    /// first <paramref name="skip"/> matches and returns at most <paramref name="take"/>, so a
    /// long list can be loaded a page at a time.
    /// </summary>
    Task<IReadOnlyList<AbilitySummary>> SearchAsync(
        AbilityQuery query,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Every class named on any ability, in alphabetical order, for a list to filter by.</summary>
    Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds the abilities, replacing any stored ability that has the same key.</summary>
    Task<AbilitySaveResult> SaveManyAsync(IReadOnlyCollection<Ability> abilities, CancellationToken cancellationToken = default);
}
