using DnDInitiative.Core.Parties;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.Parties;

internal sealed class SqlitePartyRepository : IPartyRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SqlitePartyRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Party>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<PartyEntity> entities = await db.Parties
            .AsNoTracking()
            .Include(party => party.Members)
            .OrderBy(party => party.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(ToDomain).ToList();
    }

    public async Task SaveAsync(Party party, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(party);

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        PartyEntity? existing = await db.Parties
            .Include(entity => entity.Members)
            .SingleOrDefaultAsync(entity => entity.Id == party.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            db.Parties.Add(new PartyEntity { Id = party.Id, Name = party.Name, Members = ToMemberEntities(party) });
        }
        else
        {
            existing.Name = party.Name;
            existing.Members.Clear();
            existing.Members.AddRange(ToMemberEntities(party));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.Parties
            .Where(party => party.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static List<PlayerCharacterEntity> ToMemberEntities(Party party)
    {
        return party.Members
            .Select((member, index) => new PlayerCharacterEntity
            {
                Position = index,
                Name = member.Name,
                Level = member.Level,
                MaxHp = member.MaxHp,
                ArmorClass = member.ArmorClass,
            })
            .ToList();
    }

    private static Party ToDomain(PartyEntity entity)
    {
        return new Party(
            entity.Id,
            entity.Name,
            entity.Members
                .OrderBy(member => member.Position)
                .Select(member => new PlayerCharacter(member.Name, member.Level, member.MaxHp, member.ArmorClass))
                .ToList());
    }
}