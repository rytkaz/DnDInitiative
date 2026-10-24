using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.Tests.Formatting;

public class ReferenceLinkTests
{
    [Theory]
    [InlineData("spell:fireball-xphb", "fireball-xphb")]
    [InlineData("SPELL:Fireball", "Fireball")]
    [InlineData("spell: Detect Magic ", "Detect Magic")]
    public void Spell_ToUriThenToReference_GivesBackTheReference(string target, string expected)
    {
        Assert.Equal(expected, ReferenceLink.Spell.ToReference(ReferenceLink.Spell.ToUri(target)));
    }

    [Theory]
    [InlineData("condition:Prone", "Prone")]
    [InlineData("Condition: Grappled ", "Grappled")]
    [InlineData("condition:Hiding Spot", "Hiding Spot")]
    public void Condition_ToUriThenToReference_GivesBackTheReference(string target, string expected)
    {
        Assert.Equal(expected, ReferenceLink.Condition.ToReference(ReferenceLink.Condition.ToUri(target)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("condition:")]
    [InlineData("spell:prone")]
    [InlineData("https://example.com/prone")]
    [InlineData("rules/conditions.md#Prone")]
    public void Condition_ToUri_NotALinkToACondition_ReturnsNull(string? target)
    {
        Assert.Null(ReferenceLink.Condition.ToUri(target));
    }

    [Fact]
    public void ToReference_LinkOfTheOtherKind_ReturnsNull()
    {
        Uri? spell = ReferenceLink.Spell.ToUri("spell:fireball");

        Assert.Null(ReferenceLink.Condition.ToReference(spell));
        Assert.Null(ReferenceLink.Spell.ToReference(new Uri("https://example.com/fireball")));
        Assert.Null(ReferenceLink.Spell.ToReference(null));
    }
}
