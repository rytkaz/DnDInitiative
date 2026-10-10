using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.Tests.Formatting;

public class MarkdownBlocksTests
{
    [Fact]
    public void Parse_BlankLines_SeparateParagraphsAndWrappedLinesAreJoined()
    {
        AssertBlocks(
            "A bright streak flashes\nfrom your finger.\n\nEach creature makes a save.",
            Paragraph("A bright streak flashes from your finger."),
            Paragraph("Each creature makes a save."));
    }

    [Fact]
    public void Parse_HeadingsAndBullets_AreTheirOwnBlocks()
    {
        AssertBlocks(
            "Choose one:\n- *Fire.* It burns.\n- **Cold.** It freezes.\n## At Higher Levels\nMore damage.",
            Paragraph("Choose one:"),
            Bullet("*Fire.* It burns."),
            Bullet("**Cold.** It freezes."),
            Heading("At Higher Levels"),
            Paragraph("More damage."));
    }

    [Fact]
    public void Parse_LineStartingWithEmphasis_IsNotAListItem()
    {
        AssertBlocks(
            "*Using a Higher-Level Spell Slot.* The damage increases.",
            Paragraph("*Using a Higher-Level Spell Slot.* The damage increases."));
    }

    [Fact]
    public void Parse_Callout_ShowsItsTitleAsAHeadingAndItsTextAsParagraphs()
    {
        AssertBlocks(
            "> [!note] Summoned Spirit\n> It obeys you.\n>\n> It vanishes at 0 hit points.",
            Heading("Summoned Spirit"),
            Paragraph("It obeys you."),
            Paragraph("It vanishes at 0 hit points."));
    }

    [Fact]
    public void Parse_Table_ShowsEachRowOnALineWithoutTheRuleUnderTheHeading()
    {
        AssertBlocks(
            "| d8 | Damage Type |\n|:---:|---|\n| 1 | Acid |\n| 2 | Cold |",
            Paragraph("d8  |  Damage Type"),
            Paragraph("1  |  Acid"),
            Paragraph("2  |  Cold"));
    }

    [Fact]
    public void Parse_DividerLine_IsDropped()
    {
        AssertBlocks("Above.\n---\nBelow.", Paragraph("Above."), Paragraph("Below."));
    }

    [Fact]
    public void Parse_NumberedItems_KeepTheirNumbersOnSeparateLines()
    {
        AssertBlocks(
            "1. First\n2. Second",
            Paragraph("1. First"),
            Paragraph("2. Second"));
    }

    [Fact]
    public void Parse_WindowsLineEndings_AreHandled()
    {
        AssertBlocks("One.\r\n\r\nTwo.", Paragraph("One."), Paragraph("Two."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n \n")]
    public void Parse_NoText_GivesNoBlocks(string? text)
    {
        Assert.Empty(MarkdownBlocks.Parse(text));
    }

    private static MarkdownBlock Paragraph(string text) => new(MarkdownBlockKind.Paragraph, text);

    private static MarkdownBlock Heading(string text) => new(MarkdownBlockKind.Heading, text);

    private static MarkdownBlock Bullet(string text) => new(MarkdownBlockKind.Bullet, text);

    private static void AssertBlocks(string text, params MarkdownBlock[] expected)
    {
        Assert.Equal<MarkdownBlock>(expected, MarkdownBlocks.Parse(text));
    }
}
