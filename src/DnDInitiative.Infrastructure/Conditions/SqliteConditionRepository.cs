using DnDInitiative.Core.Conditions;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.Conditions;

internal sealed class SqliteConditionRepository : IConditionRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SqliteConditionRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Condition?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        string text = name.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // The column ignores case (see AppDbContext), so "prone" finds "Prone".
        ConditionEntity? entity = await db.Conditions
            .AsNoTracking()
            .FirstOrDefaultAsync(condition => condition.Name == text, cancellationToken)
            .ConfigureAwait(false);

        return entity is null
            ? null
            : new Condition { Name = entity.Name, Source = entity.Source, Description = entity.Description };
    }

    public async Task<IReadOnlyList<string>> GetNamesAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Conditions
            .AsNoTracking()
            .Select(condition => condition.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveManyAsync(IReadOnlyCollection<Condition> conditions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // The stored spelling of each name, looked up ignoring case. An update has to use the
        // stored spelling: EF Core matches the entity it tracks by the exact key.
        List<string> storedNames = await db.Conditions
            .Select(condition => condition.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var stored = storedNames.ToDictionary(name => name, StringComparer.OrdinalIgnoreCase);

        // If the same name appears twice, the last one wins.
        var incoming = new Dictionary<string, Condition>(StringComparer.OrdinalIgnoreCase);
        foreach (Condition condition in conditions)
        {
            incoming[condition.Name.Trim()] = condition;
        }

        foreach ((string name, Condition condition) in incoming)
        {
            var entity = new ConditionEntity { Name = name, Source = condition.Source, Description = condition.Description };

            if (stored.TryGetValue(name, out string? storedName))
            {
                entity.Name = storedName;
                db.Conditions.Update(entity);
            }
            else
            {
                db.Conditions.Add(entity);
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
