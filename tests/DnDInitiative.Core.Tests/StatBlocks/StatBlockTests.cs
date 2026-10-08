using DnDInitiative.Core.Combat;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Core.Tests.StatBlocks;

public class StatBlockTests
{
    [Theory]
    [InlineData(1, -5)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(15, 2)]
    [InlineData(20, 5)]
    [InlineData(30, 10)]
    public void AbilityModifier_IsScoreMinusTenHalvedAndRoundedDown(int score, int expected)
    {
        Assert.Equal(expected, AbilityScores.Modifier(score));
    }

    [Fact]
    public void ToBlueprint_UsesStatBlockNameAndCombatNumbers()
    {
        StatBlock goblin = CreateGoblin();

        CombatantBlueprint blueprint = goblin.ToBlueprint();

        Assert.Equal(new CombatantBlueprint("Goblin", InitiativeModifier: 2, MaxHp: 10, ArmorClass: 15), blueprint);
    }

    [Fact]
    public void ToBlueprint_WithDisplayName_UsesThatName()
    {
        StatBlock goblin = CreateGoblin();

        CombatantBlueprint blueprint = goblin.ToBlueprint("Boss Goblin");

        Assert.Equal("Boss Goblin", blueprint.Name);
    }

    private static StatBlock CreateGoblin()
    {
        return new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Goblin",
            ArmorClass = 15,
            HitPoints = 10,
            InitiativeModifier = 2,
        };
    }
}