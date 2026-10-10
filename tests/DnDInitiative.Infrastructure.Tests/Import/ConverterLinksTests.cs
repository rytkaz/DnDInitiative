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

    [Theory]
    [InlineData("is [Grappled](https://example.com/rules) (escape DC 17)", "is [Grappled](https://example.com/rules) (escape DC 17)")]
    [InlineData("[Rules](HTTP://example.com/a \"The rules\")", "[Rules](HTTP://example.com/a)")]
    public void Simplify_LinkToAWebPage_IsKeptWithoutItsTitle(string text, string expected)
    {
        Assert.Equal(expected, ConverterLinks.Simplify(text));
    }

    [Theory]
    [InlineData("[Run me](file:///C:/Windows/notepad.exe)", "Run me")]
    [InlineData("[Mail](mailto:hello@example.com)", "Mail")]
    public void Simplify_LinkWithAnotherKindOfAddress_BecomesItsText(string text, string expected)
    {
        Assert.Equal(expected, ConverterLinks.Simplify(text));
    }

    [Fact]
    public void Simplify_EmbeddedImage_IsRemoved()
    {
        Assert.Equal("Before  after", ConverterLinks.Simplify("Before ![token](img/goblin.webp) after"));
    }

    [Theory]
    [InlineData("Available in the <span title='Systems Reference Document (5.2)'>SRD</span> and the Free Rules", "Available in the SRD and the Free Rules")]
    [InlineData("First line<br/>second line", "First linesecond line")]
    [InlineData("<b>[fireball](compendium/spells/fireball.md)</b>", "[fireball](spell:fireball)")]
    public void Simplify_HtmlTags_AreRemovedAndTheirTextKept(string text, string expected)
    {
        Assert.Equal(expected, ConverterLinks.Simplify(text));
    }

    [Theory]
    [InlineData("a range of < 5 ft. and > 2 ft.")]
    [InlineData("if 3 <5 and 7> 2")]
    [InlineData("an unclosed <span")]
    public void Simplify_AngleBracketsThatAreNotTags_AreUnchanged(string text)
    {
        Assert.Equal(text, ConverterLinks.Simplify(text));
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
