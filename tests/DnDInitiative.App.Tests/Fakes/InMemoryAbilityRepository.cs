using DnDInitiative.Core.Abilities;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemoryAbilityRepository : IAbilityRepository
{
    private readonly Dictionary<string, Ability> _abilities = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryAbilityRepository(params Ability[] abilities)
    {
        foreach (Ability ability in abilities)
        {
            _abilities[ability.Key] = ability;
        }
    }

    public Task<Ability?> FindAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_abilities.GetValueOrDefault(key.Trim()));
    }

    public Task<IReadOnlyList<AbilitySummary>> SearchAsync(
        AbilityQuery query,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AbilitySummary> matches = _abilities.Values
            .Where(ability => ability.Name.Contains(query.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(ability => query.Category is null || ability.Category == query.Category)
            .Where(ability => query.Class is null || ability.Classes.Contains(query.Class))
            .Where(ability => query.Degree is null || ability.Degree == query.Degree)
            .Where(ability => query.Source is null || ability.Source == query.Source)
            .OrderBy(ability => ability.Degree)
            .ThenBy(ability => ability.Name, StringComparer.OrdinalIgnoreCase)
            .Skip(skip)
            .Take(take)
            .Select(ability => new AbilitySummary(
                ability.Key,
                ability.Name,
                ability.Category,
                ability.Degree,
                ability.Prerequisites,
                ability.Classes,
                ability.Source))
            .ToList();

        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> classes = _abilities.Values
            .SelectMany(ability => ability.Classes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(classes);
    }

    public Task<AbilitySaveResult> SaveManyAsync(IReadOnlyCollection<Ability> abilities, CancellationToken cancellationToken = default)
    {
        foreach (Ability ability in abilities)
        {
            _abilities[ability.Key] = ability;
        }

        return Task.FromResult(new AbilitySaveResult(abilities.Count, 0));
    }
}