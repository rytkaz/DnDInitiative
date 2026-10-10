using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Parties;

namespace DnDInitiative.Core.Tests.Parties;

public class PartyTests
{
    [Fact]
    public void ToBlueprints_CopiesNameHpAndArmorClassWithNoInitiativeModifier()
    {
        var party = new Party(
            Guid.NewGuid(),
            "Heroes",
            [new PlayerCharacter("Aria", Level: 3, MaxHp: 24, ArmorClass: 16), new PlayerCharacter("Bren", Level: 3)]);

        IReadOnlyList<CombatantBlueprint> blueprints = party.ToBlueprints();

        Assert.Equal(2, blueprints.Count);
        Assert.Equal(new CombatantBlueprint("Aria", InitiativeModifier: 0, MaxHp: 24, ArmorClass: 16, IsPlayer: true), blueprints[0]);
        Assert.Equal(new CombatantBlueprint("Bren", IsPlayer: true), blueprints[1]);
    }

    [Fact]
    public void ToBlueprints_MarksEveryMemberAsAPlayer()
    {
        var party = new Party(Guid.NewGuid(), "Heroes", [new PlayerCharacter("Aria", Level: 3)]);

        Assert.True(Assert.Single(party.ToBlueprints()).IsPlayer);
    }
}