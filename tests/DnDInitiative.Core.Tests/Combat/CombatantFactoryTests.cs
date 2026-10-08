using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;
using DnDInitiative.Core.Tests.Dice;

namespace DnDInitiative.Core.Tests.CombatTests;

public class CombatantFactoryTests
{
    [Fact]
    public void Create_RollsInitiativeAsDiePlusModifier()
    {
        var factory = FactoryRolling(10, 15);

        IReadOnlyList<Combatant> combatants = factory.Create(
            [new CombatantBlueprint("Goblin", InitiativeModifier: 2), new CombatantBlueprint("Zombie", InitiativeModifier: -2)],
            new CombatantNamer());

        Assert.Equal(12, combatants[0].Initiative);
        Assert.Equal(13, combatants[1].Initiative);
    }

    [Fact]
    public void Create_CopiesHpAndArmorClassFromBlueprint()
    {
        var factory = FactoryRolling(10);

        IReadOnlyList<Combatant> combatants = factory.Create(
            [new CombatantBlueprint("Goblin", MaxHp: 7, ArmorClass: 15)],
            new CombatantNamer());

        Combatant goblin = Assert.Single(combatants);
        Assert.Equal(7, goblin.MaxHp);
        Assert.Equal(7, goblin.CurrentHp);
        Assert.Equal(15, goblin.ArmorClass);
    }

    [Fact]
    public void Create_DuplicateBlueprints_NumbersTheNames()
    {
        var factory = FactoryRolling(10, 10);

        IReadOnlyList<Combatant> combatants = factory.Create(
            [new CombatantBlueprint("Goblin"), new CombatantBlueprint("Goblin")],
            new CombatantNamer());

        Assert.Equal("Goblin 1", combatants[0].Name);
        Assert.Equal("Goblin 2", combatants[1].Name);
    }

    [Fact]
    public void Create_SecondCallWithSameNamer_ContinuesNumbering()
    {
        var factory = FactoryRolling(10, 10);
        var namer = new CombatantNamer();
        factory.Create([new CombatantBlueprint("Goblin")], namer);

        IReadOnlyList<Combatant> reinforcements = factory.Create([new CombatantBlueprint("Goblin")], namer);

        Assert.Equal("Goblin 2", Assert.Single(reinforcements).Name);
    }

    [Fact]
    public void Create_WithDisadvantage_KeepsLowerOfTwoDice()
    {
        var dice = new SequenceDiceRoller(15, 4);
        var factory = new CombatantFactory(new InitiativeRoller(dice));

        IReadOnlyList<Combatant> combatants = factory.Create(
            [new CombatantBlueprint("Goblin", InitiativeModifier: 2)],
            new CombatantNamer(),
            RollMode.Disadvantage);

        Assert.Equal(6, Assert.Single(combatants).Initiative);
        Assert.Equal(2, dice.RollCount);
    }

    private static CombatantFactory FactoryRolling(params int[] dice)
    {
        return new CombatantFactory(new InitiativeRoller(new SequenceDiceRoller(dice)));
    }
}