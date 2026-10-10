using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.Tests.Formatting;

public class InlineMarkdownTests
{
    [Fact]
    public void Parse_PlainText_IsOneUnstyledSegment()
    {
        AssertSegments("Speed 30 ft.", new TextSegment("Speed 30 ft."));
    }

    [Fact]
    public void Parse_SingleAsterisks_AreItalic()
    {
        AssertSegments(
            "*Melee Attack Roll:* +10 reach 10 ft. *Hit:* 17 damage.",
            new TextSegment("Melee Attack Roll:", IsItalic: true),
            new TextSegment(" +10 reach 10 ft. "),
            new TextSegment("Hit:", IsItalic: true),
            new TextSegment(" 17 damage."));
    }

    [Fact]
    public void Parse_DoubleAndTripleAsterisks_AreBoldAndBoldItalic()
    {
        AssertSegments(
            "**Bold** then ***both***",
            new TextSegment("Bold", IsBold: true),
            new TextSegment(" then "),
            new TextSegment("both", IsBold: true, IsItalic: true));
    }

    [Fact]
    public void Parse_Link_IsASegmentWithItsTarget()
    {
        AssertSegments(
            "Casts [Fireball](spell:fireball-xphb) once.",
            new TextSegment("Casts "),
            new TextSegment("Fireball", LinkTarget: "spell:fireball-xphb"),
            new TextSegment(" once."));
    }

    [Fact]
    public void Parse_LinksAfterABoldLabel_AreSeparateSegments()
    {
        // How a creature's spell list arrives.
        AssertSegments(
            "**At will:** [detect magic](spell:detect-magic), [light](spell:light)",
            new TextSegment("At will:", IsBold: true),
            new TextSegment(" "),
            new TextSegment("detect magic", LinkTarget: "spell:detect-magic"),
            new TextSegment(", "),
            new TextSegment("light", LinkTarget: "spell:light"));
    }

    [Fact]
    public void Parse_LinkInsideEmphasis_KeepsTheEmphasis()
    {
        AssertSegments(
            "*[fireball](spell:fireball)*",
            new TextSegment("fireball", IsItalic: true, LinkTarget: "spell:fireball"));
    }

    [Theory]
    [InlineData("damage [see below]")]
    [InlineData("an empty link []() is not one")]
    [InlineData("(parentheses) and [brackets] apart")]
    public void Parse_BracketsThatAreNotLinks_AreLeftAsText(string text)
    {
        AssertSegments(text, new TextSegment(text));
    }

    [Theory]
    [InlineData("2 * 3 * 4")]
    [InlineData("an unmatched *asterisk")]
    [InlineData("")]
    public void Parse_AsterisksThatAreNotEmphasis_AreLeftAsText(string text)
    {
        string joined = string.Concat(InlineMarkdown.Parse(text).Select(segment => segment.Text));

        Assert.Equal(text, joined);
        Assert.DoesNotContain(InlineMarkdown.Parse(text), segment => segment.IsBold || segment.IsItalic);
    }

    private static void AssertSegments(string text, params TextSegment[] expected)
    {
        Assert.Equal<TextSegment>(expected, InlineMarkdown.Parse(text));
    }
}