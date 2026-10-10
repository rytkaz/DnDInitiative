using DnDInitiative.Infrastructure.Import;

namespace DnDInitiative.Infrastructure.Tests.Import;

public class HomebrewLinksTests
{
    [Theory]
    [InlineData(
        "the target is {@link Grappled|https://homebrewery.naturalcrit.com/share/uDYpiTxHncTE} (escape DC 17).",
        "the target is [Grappled](https://homebrewery.naturalcrit.com/share/uDYpiTxHncTE) (escape DC 17).")]
    [InlineData(
        "catches {@link Filth Fever|http://example.com/rules?id=3#fever}.",
        "catches [Filth Fever](http://example.com/rules?id=3#fever).")]
    [InlineData(
        "{@link Prone|https://a.example/x} and {@link Dazed|https://a.example/x}",
        "[Prone](https://a.example/x) and [Dazed](https://a.example/x)")]
    public void ToMarkdown_WebLinkTag_BecomesAMarkdownLink(string json, string expected)
    {
        Assert.Equal(expected, HomebrewLinks.ToMarkdown(json));
    }

    [Theory]
    // No address, or not a web address.
    [InlineData("{@link https://discord.gg/5etools}")]
    [InlineData("{@link Mail us|mailto:hello@example.com}")]
    // Another tag inside the text.
    [InlineData("{@link {@bold Barrow Wight}|https://example.com/x}")]
    // Characters that would end a Markdown link early, or split a JSON escape.
    [InlineData("{@link Rules|https://example.com/a_(b)}")]
    [InlineData("{@link Say \\\"hi\\\"|https://example.com/x}")]
    // Other tags are none of this class's business.
    [InlineData("{@damage 2d6} and {@spell fireball|XPHB}")]
    [InlineData("no tags at all")]
    public void ToMarkdown_AnythingElse_IsLeftExactlyAsItWas(string json)
    {
        Assert.Equal(json, HomebrewLinks.ToMarkdown(json));
    }
}
