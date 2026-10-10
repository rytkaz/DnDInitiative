using DnDInitiative.Infrastructure.Import;

namespace DnDInitiative.Infrastructure.Tests.Import;

public class ConverterLinksTests
{
    [Theory]
    [InlineData("[fireball](compendium/spells/fireball-xphb.md)", "[fireball](spell:fireball-xphb)")]
    [InlineData("[Fireball](spells/Fireball.md)", "[Fireball](spell:fireball)")]
    [InlineData("[fireball](compendium/spells/fireball.md#At Higher Levels)", "[fireball](spell:fireball)")]
    [InlineData("[fireball](compendium/spells/fireball.md \"Fireball\")", "[fireball](spell:fireball)")]
    public void Simplify_LinkToASpellNote_BecomesALinkToTheSpellsKey(string text, string expected)
    {
        Assert.Equal(expected, ConverterLinks.Simplify(text));
    }

    [Theory]
    [InlineData("[Stealth](rules/skills.md#Stealth)", "Stealth")]
    [InlineData("[Prone](rules/conditions.md#Prone) and [Goblin](compendium/bestiary/fey/goblin.md)", "Prone and Goblin")]
    // A folder that merely ends in "spells", and a list of spells, are not spell notes.
    [InlineData("[Wizard](compendium/lists/wizard-spells/index.md)", "Wizard")]
    [InlineData("[Spells](compendium/spells.md)", "Spells")]
    public void Simplify_LinkToAnythingElse_BecomesItsText(string text, string expected)
    {
        Assert.Equal(expected, ConverterLinks.Simplify(text));
    }

    [Fact]
    public void Simplify_EmbeddedImage_IsRemoved()
    {
        Assert.Equal("Before  after", ConverterLinks.Simplify("Before ![token](img/goblin.webp) after"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Speed 30 ft.")]
    [InlineData("*Hit:* 7 (1d6 + 4) damage [see below]")]
    public void Simplify_TextWithoutLinks_IsUnchanged(string text)
    {
        Assert.Equal(text, ConverterLinks.Simplify(text));
    }
}
