using DnDInitiative.Core.Abilities;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.Abilities;

internal sealed class SqliteAbilityRepository : IAbilityRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SqliteAbilityRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Ability?> FindAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        string normalized = NormalizeKey(key);
        if (normalized.Length == 0)
        {
            return null;
        }

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        AbilityEntity? entity = await db.Abilities
            .AsNoTracking()
            .FirstOrDefaultAsync(ability => ability.Key == normalized, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<AbilitySummary>> SearchAsync(
        AbilityQuery query,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        IQueryable<AbilityEntity> abilities = db.Abilities.AsNoTracking();

        string text = query.Text.Trim();
        if (text.Length > 0)
        {
            string pattern = LikePattern.Containing(text);
            abilities = abilities.Where(ability => EF.Functions.Like(ability.Name, pattern, LikePattern.Escape));
        }

        if (query.Category is { } category)
        {
            abilities = abilities.Where(ability => ability.Category == category);
        }

        if (query.Class is { } className)
        {
            abilities = abilities.Where(ability => ability.Classes.Contains(className));
        }

        if (query.Degree is { } degree)
        {
            abilities = abilities.Where(ability => ability.Degree == degree);
        }

        if (query.Source is { } source)
        {
            abilities = abilities.Where(ability => ability.Source == source);
        }

        // The key last, so two exploits of the same name from different documents keep their order between pages.
        return await abilities
            .OrderBy(ability => ability.Degree)
            .ThenBy(ability => ability.Name)
            .ThenBy(ability => ability.Key)
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
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Only the one column is read. The lists are short, so they are merged here rather than in SQL.
        List<List<string>> lists = await db.Abilities
            .Select(ability => ability.Classes)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return lists
            .SelectMany(classes => classes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<AbilitySaveResult> SaveManyAsync(IReadOnlyCollection<Ability> abilities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(abilities);

        // If the same key appears twice, the last one wins.
        var incoming = new Dictionary<string, AbilityEntity>(StringComparer.Ordinal);
        foreach (Ability ability in abilities)
        {
            AbilityEntity entity = ToEntity(ability);
            incoming[entity.Key] = entity;
        }

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<string> storedKeys = await db.Abilities
            .Select(ability => ability.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingKeys = new HashSet<string>(storedKeys, StringComparer.Ordinal);

        int added = 0;
        int updated = 0;

        foreach (AbilityEntity entity in incoming.Values)
        {
            if (existingKeys.Contains(entity.Key))
            {
                db.Abilities.Update(entity);
                updated++;
            }
            else
            {
                db.Abilities.Add(entity);
                added++;
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new AbilitySaveResult(added, updated);
    }

    // Keys are stored in lower case, so looking one up is an exact, indexed comparison.
    private static string NormalizeKey(string key)
    {
        return key.Trim().ToLowerInvariant();
    }

    private static AbilityEntity ToEntity(Ability ability)
    {
        return new AbilityEntity
        {
            Key = NormalizeKey(ability.Key),
            Name = ability.Name,
            Category = ability.Category,
            Degree = ability.Degree,
            Prerequisites = ability.Prerequisites,
            Classes = [.. ability.Classes],
            Source = ability.Source,
            Description = ability.Description,
        };
    }

    private static Ability ToDomain(AbilityEntity entity)
    {
        return new Ability
        {
            Key = entity.Key,
            Name = entity.Name,
            Category = entity.Category,
            Degree = entity.Degree,
            Prerequisites = entity.Prerequisites,
            Classes = entity.Classes,
            Source = entity.Source,
            Description = entity.Description,
        };
    }
}
