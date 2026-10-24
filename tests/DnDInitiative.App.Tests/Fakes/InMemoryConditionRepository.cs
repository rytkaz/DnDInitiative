using DnDInitiative.Core.Conditions;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemoryConditionRepository : IConditionRepository
{
    private readonly Dictionary<string, Condition> _conditions = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryConditionRepository(params Condition[] conditions)
    {
        foreach (Condition condition in conditions)
        {
            _conditions[condition.Name] = condition;
        }
    }

    public Task<Condition?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_conditions.GetValueOrDefault(name.Trim()));
    }

    public Task SaveManyAsync(IReadOnlyCollection<Condition> conditions, CancellationToken cancellationToken = default)
    {
        foreach (Condition condition in conditions)
        {
            _conditions[condition.Name] = condition;
        }

        return Task.CompletedTask;
    }
}
