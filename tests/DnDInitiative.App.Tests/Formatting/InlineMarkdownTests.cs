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