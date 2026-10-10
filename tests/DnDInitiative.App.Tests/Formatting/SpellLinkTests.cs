using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.Tests.Formatting;

public class SpellLinkTests
{
    [Theory]
    [InlineData("spell:fireball-xphb", "fireball-xphb")]
    [InlineData("SPELL:Fireball", "Fireball")]
    [InlineData("spell: Detect Magic ", "Detect Magic")]
    public void ToUri_ThenToReference_GivesBackTheSpellReference(string target, string expected)
    {
        Uri? uri = SpellLink.ToUri(target);

        Assert.NotNull(uri);
        Assert.Equal(expected, SpellLink.ToReference(uri));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("spell:")]
    [InlineData("spell:   ")]
    [InlineData("https://example.com/fireball")]
    [InlineData("rules/conditions.md#Prone")]
    public void ToUri_NotALinkToASpell_ReturnsNull(string? target)
    {
        Assert.Null(SpellLink.ToUri(target));
    }

    [Fact]
    public void ToReference_SomeOtherKindOfLink_ReturnsNull()
    {
        Assert.Null(SpellLink.ToReference(new Uri("https://example.com/fireball")));
        Assert.Null(SpellLink.ToReference(new Uri("fireball", UriKind.Relative)));
        Assert.Null(SpellLink.ToReference(null));
    }
}
