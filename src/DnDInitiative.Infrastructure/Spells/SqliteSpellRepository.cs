using DnDInitiative.Core.Spells;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.Spells;

internal sealed class SqliteSpellRepository : ISpellRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SqliteSpellRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Spell?> FindAsync(string reference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);

        string text = reference.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        string key = NormalizeKey(text);

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        SpellEntity? entity = await db.Spells
            .AsNoTracking()
            .FirstOrDefaultAsync(spell => spell.Key == key, cancellationToken)
            .ConfigureAwait(false);

        // The Name column ignores case (see AppDbContext), so "fireball" finds "Fireball".
        // Two books can print a spell of the same name; ordering by key makes the choice
        // between them the same every time.
        entity ??= await db.Spells
            .AsNoTracking()
            .Where(spell => spell.Name == text)
            .OrderBy(spell => spell.Key)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<SpellSummary>> SearchAsync(
        SpellQuery query,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        IQueryable<SpellEntity> spells = db.Spells.AsNoTracking();

        string text = query.Text.Trim();
        if (text.Length > 0)
        {
            string pattern = LikePattern.Containing(text);
            spells = spells.Where(spell => EF.Functions.Like(spell.Name, pattern, LikePattern.Escape));
        }

        if (query.Class is { } className)
        {
            spells = spells.Where(spell => spell.Classes.Contains(className));
        }

        if (query.Level is { } level)
        {
            spells = spells.Where(spell => spell.LevelNumber == level);
        }

        if (query.IsRitual is { } isRitual)
        {
            spells = spells.Where(spell => spell.IsRitual == isRitual);
        }

        if (query.IsConcentration is { } isConcentration)
        {
            spells = spells.Where(spell => spell.IsConcentration == isConcentration);
        }

        if (query.Source is { } source)
        {
            spells = spells.Where(spell => spell.Source == source);
        }

        // The key last, so two spells of the same name from different books keep their order between pages.
        return await spells
            .OrderBy(spell => spell.LevelNumber)
            .ThenBy(spell => spell.Name)
            .ThenBy(spell => spell.Key)
            .Skip(skip)
            .Take(take)
            .Select(spell => new SpellSummary(
                spell.Key,
                spell.Name,
                spell.LevelNumber,
                spell.School,
                spell.IsRitual,
                spell.IsConcentration,
                spell.Classes,
                spell.Source,
                spell.CastingTime,
                spell.Range))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Whether there is a stored spell with exactly this key. Upper and lower case are not distinguished.</summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        string normalized = NormalizeKey(key);

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Spells
            .AnyAsync(spell => spell.Key == normalized, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Only the one column is read. The lists are short, so they are merged here rather than in SQL.
        List<List<string>> lists = await db.Spells
            .Select(spell => spell.Classes)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return lists
            .SelectMany(classes => classes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<SpellSaveResult> SaveManyAsync(IReadOnlyCollection<Spell> spells, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spells);

        // If the same key appears twice, the last one wins.
        var incoming = new Dictionary<string, SpellEntity>(StringComparer.Ordinal);
        foreach (Spell spell in spells)
        {
            SpellEntity entity = ToEntity(spell);
            incoming[entity.Key] = entity;
        }

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<string> storedKeys = await db.Spells
            .Select(spell => spell.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingKeys = new HashSet<string>(storedKeys, StringComparer.Ordinal);

        int added = 0;
        int updated = 0;

        foreach (SpellEntity entity in incoming.Values)
        {
            if (existingKeys.Contains(entity.Key))
            {
                db.Spells.Update(entity);
                updated++;
            }
            else
            {
                db.Spells.Add(entity);
                added++;
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SpellSaveResult(added, updated);
    }

    // Keys are stored in lower case, so looking one up is an exact, indexed comparison.
    private static string NormalizeKey(string key)
    {
        return key.Trim().ToLowerInvariant();
    }

    private static SpellEntity ToEntity(Spell spell)
    {
        return new SpellEntity
        {
            Key = NormalizeKey(spell.Key),
            Name = spell.Name,
            Level = spell.Level,
            LevelNumber = spell.LevelNumber,
            School = spell.School,
            IsRitual = spell.IsRitual,
            CastingTime = spell.CastingTime,
            Range = spell.Range,
            Components = spell.Components,
            Duration = spell.Duration,
            IsConcentration = spell.IsConcentration,
            Classes = [.. spell.Classes],
            Source = spell.Source,
            Description = spell.Description,
        };
    }

    private static Spell ToDomain(SpellEntity entity)
    {
        return new Spell
        {
            Key = entity.Key,
            Name = entity.Name,
            Level = entity.Level,
            LevelNumber = entity.LevelNumber,
            School = entity.School,
            IsRitual = entity.IsRitual,
            CastingTime = entity.CastingTime,
            Range = entity.Range,
            Components = entity.Components,
            Duration = entity.Duration,
            IsConcentration = entity.IsConcentration,
            Classes = entity.Classes,
            Source = entity.Source,
            Description = entity.Description,
        };
    }
}
