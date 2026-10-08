using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Core.Tests.Encounters;

public class EncounterTests
{
    [Theory]
    [InlineData("0", 10)]
    [InlineData("1/8", 25)]
    [InlineData("1/4", 50)]
    [InlineData("1/2", 100)]
    [InlineData("5", 1800)]
    [InlineData("30", 155000)]
    [InlineData(" 5 (XP 1,800) ", 1800)]
    public void TryGetXp_KnownRating_ReturnsItsXp(string rating, int expected)
    {
        Assert.True(ChallengeRatings.TryGetXp(rating, out int xp));
        Assert.Equal(expected, xp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("31")]
    [InlineData("varies")]
    public void TryGetXp_UnknownRating_ReturnsFalse(string rating)
    {
        Assert.False(ChallengeRatings.TryGetXp(rating, out _));
    }

    [Fact]
    public void ForParty_AddsEachCharactersBudget()
    {
        Assert.Equal(new XpBudget(200, 300, 400), XpBudget.ForParty([1, 1, 1, 1]));
        Assert.Equal(new XpBudget(550, 825, 1200), XpBudget.ForParty([1, 5]));
        Assert.Equal(new XpBudget(0, 0, 0), XpBudget.ForParty([]));
    }

    [Theory]
    [InlineData(0, EncounterDifficulty.None)]
    [InlineData(150, EncounterDifficulty.Low)]
    [InlineData(200, EncounterDifficulty.Low)]
    [InlineData(201, EncounterDifficulty.Moderate)]
    [InlineData(300, EncounterDifficulty.Moderate)]
    [InlineData(400, EncounterDifficulty.High)]
    [InlineData(401, EncounterDifficulty.AboveHigh)]
    public void Classify_ReturnsLowestDifficultyThatCoversTheXp(int totalXp, EncounterDifficulty expected)
    {
        var budget = new XpBudget(200, 300, 400);

        Assert.Equal(expected, budget.Classify(totalXp));
    }

    [Fact]
    public void Classify_WithoutAParty_ReturnsNone()
    {
        Assert.Equal(EncounterDifficulty.None, new XpBudget(0, 0, 0).Classify(500));
    }

    [Fact]
    public void TotalXp_MultipliesEachCreaturesXpByItsQuantity()
    {
        StatBlock goblin = Create("Goblin", "1/4");
        StatBlock ogre = Create("Ogre", "2");
        Encounter encounter = CreateEncounter(new EncounterCreature(goblin.Id, 3), new EncounterCreature(ogre.Id));

        int totalXp = encounter.TotalXp(Lookup(goblin, ogre));

        Assert.Equal(600, totalXp);
    }

    [Fact]
    public void ToBlueprints_ExpandsQuantitiesAndAppliesCustomNames()
    {
        StatBlock goblin = Create("Goblin", "1/4");
        StatBlock ogre = Create("Ogre", "2");
        Encounter encounter = CreateEncounter(
            new EncounterCreature(goblin.Id, 2),
            new EncounterCreature(ogre.Id, 1, "Grukk the Ogre"));

        IReadOnlyList<CombatantBlueprint> blueprints = encounter.ToBlueprints(Lookup(goblin, ogre));

        Assert.Equal(3, blueprints.Count);
        Assert.Equal("Goblin", blueprints[0].Name);
        Assert.Equal("Goblin", blueprints[1].Name);
        Assert.Equal("Grukk the Ogre", blueprints[2].Name);
    }

    [Fact]
    public void ToBlueprints_StatBlockNoLongerExists_SkipsThatLine()
    {
        StatBlock goblin = Create("Goblin", "1/4");
        Encounter encounter = CreateEncounter(
            new EncounterCreature(goblin.Id),
            new EncounterCreature(Guid.NewGuid(), 5));

        Assert.Single(encounter.ToBlueprints(Lookup(goblin)));
        Assert.Equal(50, encounter.TotalXp(Lookup(goblin)));
    }

    private static StatBlock Create(string name, string challengeRating)
    {
        return new StatBlock { Id = Guid.NewGuid(), Name = name, ChallengeRating = challengeRating };
    }

    private static Encounter CreateEncounter(params EncounterCreature[] creatures)
    {
        return new Encounter(Guid.NewGuid(), "Ambush", creatures);
    }

    private static Dictionary<Guid, StatBlock> Lookup(params StatBlock[] statBlocks)
    {
        return statBlocks.ToDictionary(statBlock => statBlock.Id);
    }
}