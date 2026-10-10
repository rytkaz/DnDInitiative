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
            School = spell.School,
            IsRitual = spell.IsRitual,
            CastingTime = spell.CastingTime,
            Range = spell.Range,
            Components = spell.Components,
            Duration = spell.Duration,
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
            School = entity.School,
            IsRitual = entity.IsRitual,
            CastingTime = entity.CastingTime,
            Range = entity.Range,
            Components = entity.Components,
            Duration = entity.Duration,
            Source = entity.Source,
            Description = entity.Description,
        };
    }
}
