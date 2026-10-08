using DnDInitiative.Core.Combat;

namespace DnDInitiative.Core.Tests.CombatTests;

public class CombatantTests
{
    [Fact]
    public void NewCombatant_WithMaxHp_StartsAtFullHp()
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);

        Assert.Equal(7, goblin.MaxHp);
        Assert.Equal(7, goblin.CurrentHp);
        Assert.True(goblin.TracksHp);
    }

    [Fact]
    public void NewCombatant_WithoutMaxHp_DoesNotTrackHp()
    {
        var player = new Combatant("Aria", initiative: 12);

        Assert.Null(player.MaxHp);
        Assert.Null(player.CurrentHp);
        Assert.False(player.TracksHp);
    }

    [Fact]
    public void NewCombatants_GetDifferentIds()
    {
        var first = new Combatant("Goblin", initiative: 12);
        var second = new Combatant("Goblin", initiative: 12);

        Assert.NotEqual(first.Id, second.Id);
    }

    // Behaviour 7: damage and healing

    [Fact]
    public void TakeDamage_ReducesCurrentHp()
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);

        goblin.TakeDamage(3);

        Assert.Equal(4, goblin.CurrentHp);
        Assert.Equal(7, goblin.MaxHp);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(int.MaxValue)]
    public void TakeDamage_AtLeastCurrentHp_StopsAtZero(int amount)
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);

        goblin.TakeDamage(amount);

        Assert.Equal(0, goblin.CurrentHp);
    }

    [Fact]
    public void Heal_RestoresCurrentHp()
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);
        goblin.TakeDamage(5);

        goblin.Heal(2);

        Assert.Equal(4, goblin.CurrentHp);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void Heal_UpToOrPastMaxHp_StopsAtMaxHp(int amount)
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);
        goblin.TakeDamage(5);

        goblin.Heal(amount);

        Assert.Equal(7, goblin.CurrentHp);
    }

    [Fact]
    public void TakeDamage_NegativeAmount_Throws()
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);

        Assert.Throws<ArgumentOutOfRangeException>(() => goblin.TakeDamage(-1));
        Assert.Equal(7, goblin.CurrentHp);
    }

    [Fact]
    public void Heal_NegativeAmount_Throws()
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);
        goblin.TakeDamage(5);

        Assert.Throws<ArgumentOutOfRangeException>(() => goblin.Heal(-1));
        Assert.Equal(2, goblin.CurrentHp);
    }

    [Fact]
    public void TakeDamage_WhenHpIsNotTracked_Throws()
    {
        var player = new Combatant("Aria", initiative: 12);

        Assert.Throws<InvalidOperationException>(() => player.TakeDamage(1));
    }

    // Manual edits from the tracker UI

    [Fact]
    public void SetMaxHp_BelowCurrentHp_PullsCurrentHpDown()
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);

        goblin.SetMaxHp(4);

        Assert.Equal(4, goblin.MaxHp);
        Assert.Equal(4, goblin.CurrentHp);
    }

    [Fact]
    public void SetMaxHp_AboveCurrentHp_LeavesCurrentHpAlone()
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);
        goblin.TakeDamage(3);

        goblin.SetMaxHp(20);

        Assert.Equal(20, goblin.MaxHp);
        Assert.Equal(4, goblin.CurrentHp);
    }

    [Fact]
    public void SetMaxHp_OnCombatantWithoutHp_StartsTrackingAtFull()
    {
        var player = new Combatant("Aria", initiative: 12);

        player.SetMaxHp(30);

        Assert.Equal(30, player.CurrentHp);
    }

    [Fact]
    public void SetMaxHp_Negative_Throws()
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);

        Assert.Throws<ArgumentOutOfRangeException>(() => goblin.SetMaxHp(-1));
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(3, 3)]
    [InlineData(99, 7)]
    public void SetCurrentHp_StaysBetweenZeroAndMaxHp(int value, int expected)
    {
        var goblin = new Combatant("Goblin", initiative: 12, maxHp: 7);

        goblin.SetCurrentHp(value);

        Assert.Equal(expected, goblin.CurrentHp);
    }
}
