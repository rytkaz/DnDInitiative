using DnDInitiative.Core.Encounters;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.Encounters;

internal sealed class SqliteEncounterRepository : IEncounterRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SqliteEncounterRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Encounter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<EncounterEntity> entities = await db.Encounters
            .AsNoTracking()
            .Include(encounter => encounter.Creatures)
            .OrderBy(encounter => encounter.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<Encounter?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        EncounterEntity? entity = await db.Encounters
            .AsNoTracking()
            .Include(encounter => encounter.Creatures)
            .SingleOrDefaultAsync(encounter => encounter.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(Encounter encounter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encounter);

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        EncounterEntity? existing = await db.Encounters
            .Include(entity => entity.Creatures)
            .SingleOrDefaultAsync(entity => entity.Id == encounter.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            db.Encounters.Add(new EncounterEntity
            {
                Id = encounter.Id,
                Name = encounter.Name,
                Creatures = ToCreatureEntities(encounter),
            });
        }
        else
        {
            existing.Name = encounter.Name;
            existing.Creatures.Clear();
            existing.Creatures.AddRange(ToCreatureEntities(encounter));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.Encounters
            .Where(encounter => encounter.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static List<EncounterCreatureEntity> ToCreatureEntities(Encounter encounter)
    {
        return encounter.Creatures
            .Select((creature, index) => new EncounterCreatureEntity
            {
                Position = index,
                StatBlockId = creature.StatBlockId,
                Quantity = creature.Quantity,
                CustomName = creature.CustomName,
            })
            .ToList();
    }

    private static Encounter ToDomain(EncounterEntity entity)
    {
        return new Encounter(
            entity.Id,
            entity.Name,
            entity.Creatures
                .OrderBy(creature => creature.Position)
                .Select(creature => new EncounterCreature(creature.StatBlockId, creature.Quantity, creature.CustomName))
                .ToList());
    }
}