using DnDInitiative.Core.Combat;

namespace DnDInitiative.Core.Tests.Combat;

public class CombatantRenameTests
{
    [Fact]
    public void Rename_ChangesTheNameAndNothingElse()
    {
        var statBlockId = Guid.NewGuid();
        var combatant = new Combatant("Goblin 2", initiative: 12, maxHp: 10, armorClass: 15, statBlockId: statBlockId);
        Guid id = combatant.Id;
        combatant.TakeDamage(4);

        combatant.Rename("  Goblin (burning) ");

        Assert.Equal("Goblin (burning)", combatant.Name);
        Assert.Equal("Goblin (burning)", combatant.ToString());
        Assert.Equal(id, combatant.Id);
        Assert.Equal(12, combatant.Initiative);
        Assert.Equal(6, combatant.CurrentHp);
        Assert.Equal(statBlockId, combatant.StatBlockId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_ToNothing_IsRefused(string name)
    {
        var combatant = new Combatant("Goblin", initiative: 12);

        Assert.Throws<ArgumentException>(() => combatant.Rename(name));
        Assert.Equal("Goblin", combatant.Name);
    }
}
