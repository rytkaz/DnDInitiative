using System.Text.Json;

using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.StatBlocks;

internal sealed class SqliteStatBlockRepository : IStatBlockRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SqliteStatBlockRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<StatBlock>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<StatBlockEntity> entities = await db.StatBlocks
            .AsNoTracking()
            .OrderBy(statBlock => statBlock.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(ToDomain).ToList();
    }

    public Task SaveAsync(StatBlock statBlock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statBlock);
        return SaveManyAsync([statBlock], cancellationToken);
    }

    public async Task SaveManyAsync(IReadOnlyCollection<StatBlock> statBlocks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statBlocks);

        // If the same Id appears twice, the last one wins.
        var incoming = new Dictionary<Guid, StatBlock>();
        foreach (StatBlock statBlock in statBlocks)
        {
            incoming[statBlock.Id] = statBlock;
        }

        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<Guid> storedIds = await db.StatBlocks
            .Select(statBlock => statBlock.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingIds = new HashSet<Guid>(storedIds);

        foreach (StatBlock statBlock in incoming.Values)
        {
            StatBlockEntity entity = ToEntity(statBlock);
            if (existingIds.Contains(entity.Id))
            {
                db.StatBlocks.Update(entity);
            }
            else
            {
                db.StatBlocks.Add(entity);
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using AppDbContext db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.StatBlocks
            .Where(statBlock => statBlock.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static StatBlockEntity ToEntity(StatBlock statBlock)
    {
        return new StatBlockEntity
        {
            Id = statBlock.Id,
            Name = statBlock.Name,
            Source = statBlock.Source,
            Size = statBlock.Size,
            CreatureType = statBlock.CreatureType,
            Alignment = statBlock.Alignment,
            ArmorClass = statBlock.ArmorClass,
            HitPoints = statBlock.HitPoints,
            HitDice = statBlock.HitDice,
            InitiativeModifier = statBlock.InitiativeModifier,
            Speed = statBlock.Speed,
            Strength = statBlock.Abilities.Strength,
            Dexterity = statBlock.Abilities.Dexterity,
            Constitution = statBlock.Abilities.Constitution,
            Intelligence = statBlock.Abilities.Intelligence,
            Wisdom = statBlock.Abilities.Wisdom,
            Charisma = statBlock.Abilities.Charisma,
            SavingThrows = statBlock.SavingThrows,
            Skills = statBlock.Skills,
            DamageVulnerabilities = statBlock.DamageVulnerabilities,
            DamageResistances = statBlock.DamageResistances,
            DamageImmunities = statBlock.DamageImmunities,
            ConditionImmunities = statBlock.ConditionImmunities,
            Senses = statBlock.Senses,
            Languages = statBlock.Languages,
            ChallengeRating = statBlock.ChallengeRating,
            TraitsJson = JsonSerializer.Serialize(statBlock.Traits),
            ActionsJson = JsonSerializer.Serialize(statBlock.Actions),
            BonusActionsJson = JsonSerializer.Serialize(statBlock.BonusActions),
            ReactionsJson = JsonSerializer.Serialize(statBlock.Reactions),
            LegendaryActionsJson = JsonSerializer.Serialize(statBlock.LegendaryActions),
        };
    }

    private static StatBlock ToDomain(StatBlockEntity entity)
    {
        return new StatBlock
        {
            Id = entity.Id,
            Name = entity.Name,
            Source = entity.Source,
            Size = entity.Size,
            CreatureType = entity.CreatureType,
            Alignment = entity.Alignment,
            ArmorClass = entity.ArmorClass,
            HitPoints = entity.HitPoints,
            HitDice = entity.HitDice,
            InitiativeModifier = entity.InitiativeModifier,
            Speed = entity.Speed,
            Abilities = new AbilityScores(
                entity.Strength,
                entity.Dexterity,
                entity.Constitution,
                entity.Intelligence,
                entity.Wisdom,
                entity.Charisma),
            SavingThrows = entity.SavingThrows,
            Skills = entity.Skills,
            DamageVulnerabilities = entity.DamageVulnerabilities,
            DamageResistances = entity.DamageResistances,
            DamageImmunities = entity.DamageImmunities,
            ConditionImmunities = entity.ConditionImmunities,
            Senses = entity.Senses,
            Languages = entity.Languages,
            ChallengeRating = entity.ChallengeRating,
            Traits = ToFeatures(entity.TraitsJson),
            Actions = ToFeatures(entity.ActionsJson),
            BonusActions = ToFeatures(entity.BonusActionsJson),
            Reactions = ToFeatures(entity.ReactionsJson),
            LegendaryActions = ToFeatures(entity.LegendaryActionsJson),
        };
    }

    private static List<StatBlockFeature> ToFeatures(string json)
    {
        return string.IsNullOrEmpty(json)
            ? []
            : JsonSerializer.Deserialize<List<StatBlockFeature>>(json) ?? [];
    }
}