using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.StatBlocks;

namespace DnDInitiative.Infrastructure.Tests.Persistence;

public sealed class SqliteStatBlockRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task SaveAsync_NewStatBlock_EveryFieldSurvivesTheRoundTrip()
    {
        var repository = new SqliteStatBlockRepository(_database);
        var original = new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Goblin Boss",
            Source = "MM",
            Size = "Small",
            CreatureType = "Fey (Goblinoid)",
            Alignment = "Chaotic Neutral",
            ArmorClass = 17,
            HitPoints = 21,
            HitDice = "6d6",
            InitiativeModifier = 2,
            Speed = "30 ft.",
            Abilities = new AbilityScores(10, 15, 11, 12, 13, 14),
            SavingThrows = "Dexterity +4",
            Skills = "Stealth +6",
            DamageVulnerabilities = "radiant",
            DamageResistances = "poison",
            DamageImmunities = "psychic",
            ConditionImmunities = "charmed",
            Senses = "darkvision 60 ft.",
            Languages = "Common, Goblin",
            ChallengeRating = "1",
            Traits = [new StatBlockFeature("Nimble Escape", "Disengage or Hide.")],
            Actions = [new StatBlockFeature("Multiattack", "Two attacks."), new StatBlockFeature("Scimitar", "Melee.")],
            BonusActions = [new StatBlockFeature("Dash", "Moves again.")],
            Reactions = [new StatBlockFeature("Redirect Attack", "Swaps places.")],
            LegendaryActions = [new StatBlockFeature("Command", "An ally attacks.")],
        };

        await repository.SaveAsync(original);

        StatBlock loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal<StatBlockFeature>(original.Traits, loaded.Traits);
        Assert.Equal<StatBlockFeature>(original.Actions, loaded.Actions);
        Assert.Equal<StatBlockFeature>(original.BonusActions, loaded.BonusActions);
        Assert.Equal<StatBlockFeature>(original.Reactions, loaded.Reactions);
        Assert.Equal<StatBlockFeature>(original.LegendaryActions, loaded.LegendaryActions);

        // With the lists set to the same instances, record equality compares every remaining field.
        StatBlock expected = original with
        {
            Traits = loaded.Traits,
            Actions = loaded.Actions,
            BonusActions = loaded.BonusActions,
            Reactions = loaded.Reactions,
            LegendaryActions = loaded.LegendaryActions,
        };
        Assert.Equal(expected, loaded);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_ReplacesStoredStatBlock()
    {
        var repository = new SqliteStatBlockRepository(_database);
        StatBlock goblin = Create("Goblin");
        await repository.SaveAsync(goblin);

        await repository.SaveAsync(goblin with { HitPoints = 99, Traits = [new StatBlockFeature("Tough", "More HP.")] });

        StatBlock loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(99, loaded.HitPoints);
        Assert.Equal("Tough", Assert.Single(loaded.Traits).Name);
    }

    [Fact]
    public async Task SaveManyAsync_AddsNewOnesAndUpdatesExistingOnesInOneCall()
    {
        var repository = new SqliteStatBlockRepository(_database);
        StatBlock goblin = Create("Goblin");
        await repository.SaveAsync(goblin);
        StatBlock ogre = Create("Ogre");

        await repository.SaveManyAsync([goblin with { HitPoints = 12 }, ogre]);

        IReadOnlyList<StatBlock> all = await repository.GetAllAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal(12, all.Single(statBlock => statBlock.Id == goblin.Id).HitPoints);
        Assert.Contains(all, statBlock => statBlock.Id == ogre.Id);
    }

    [Fact]
    public async Task GetAllAsync_SortsByNameIgnoringCase()
    {
        var repository = new SqliteStatBlockRepository(_database);
        await repository.SaveManyAsync([Create("Zombie"), Create("goblin"), Create("Aboleth")]);

        IReadOnlyList<StatBlock> all = await repository.GetAllAsync();

        Assert.Equal(["Aboleth", "goblin", "Zombie"], all.Select(statBlock => statBlock.Name));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheStatBlockWithThatId()
    {
        var repository = new SqliteStatBlockRepository(_database);
        StatBlock keep = Create("Keep");
        StatBlock remove = Create("Remove");
        await repository.SaveManyAsync([keep, remove]);

        await repository.DeleteAsync(remove.Id);

        Assert.Equal(keep.Id, Assert.Single(await repository.GetAllAsync()).Id);
    }

    private static StatBlock Create(string name)
    {
        return new StatBlock { Id = Guid.NewGuid(), Name = name };
    }
}