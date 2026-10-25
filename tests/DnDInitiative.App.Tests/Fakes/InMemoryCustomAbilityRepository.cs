using DnDInitiative.Core.Abilities;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemoryCustomAbilityRepository : ICustomAbilityRepository
{
    private readonly Dictionary<string, Ability> _abilities = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(Ability ability, CancellationToken cancellationToken = default)
    {
        _abilities[ability.Key] = ability with { IsCustom = true };
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_abilities.Remove(key));
    }

    public Task<bool> ExistsInLibraryAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<Ability?> FindCustomAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_abilities.GetValueOrDefault(key));
    }
}
