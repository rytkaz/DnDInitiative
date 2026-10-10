using DnDInitiative.Core.Spells;
using DnDInitiative.Infrastructure.Import;

namespace DnDInitiative.Infrastructure.Tests.Import;

public class SpellNoteParserTests
{
    private const string Fence = "```";

    private const string DetectMagicNote = $"""
    # Detect Magic

    {Fence}spell
    name: Detect Magic
    level: 1st-level
    school: Divination
    ritual: true
    time: 1 action
    range: Self
    components: V, S, M (note: any [focus](compendium/items/arcane-focus.md) will do)
    duration: Concentration, up to 10 minutes
    source: XPHB p. 262
    {Fence}

    For the duration, you sense the presence of magical effects within 30 feet of yourself.
    ^detect-magic-text

    The spell is blocked by 1 foot of stone. See also [Identify](compendium/spells/identify-xphb.md)
    and the [Invisible](rules/conditions.md#Invisible) condition.
    """;

    [Fact]
    public void Parse_SpellNote_ReadsEveryField()
    {
        Spell? spell = SpellNoteParser.Parse(DetectMagicNote, "detect-magic-xphb");

        Assert.NotNull(spell);
        Assert.Equal("detect-magic-xphb", spell.Key);
        Assert.Equal("Detect Magic", spell.Name);
        Assert.Equal("1st-level", spell.Level);
        Assert.Equal("Divination", spell.School);
        Assert.True(spell.IsRitual);
        Assert.Equal("1 action", spell.CastingTime);
        Assert.Equal("Self", spell.Range);
        Assert.Equal("Concentration, up to 10 minutes", spell.Duration);
        Assert.Equal("XPHB p. 262", spell.Source);
    }

    [Fact]
    public void Parse_ValueContainingAColonAndALink_IsSplitAtTheFirstColonOnlyAndTheLinkBecomesText()
    {
        Spell? spell = SpellNoteParser.Parse(DetectMagicNote, "detect-magic-xphb");

        Assert.Equal("V, S, M (note: any focus will do)", spell?.Components);
    }

    [Fact]
    public void Parse_Text_KeepsParagraphsAndSpellLinksAndDropsObsidianAnchors()
    {
        Spell? spell = SpellNoteParser.Parse(DetectMagicNote, "detect-magic-xphb");

        Assert.Equal(
            "For the duration, you sense the presence of magical effects within 30 feet of yourself.\n\n"
            + "The spell is blocked by 1 foot of stone. See also [Identify](spell:identify-xphb)\n"
            + "and the Invisible condition.",
            spell?.Description.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_WindowsLineEndings_GiveTheSameSpell()
    {
        string unix = DetectMagicNote.Replace("\r\n", "\n", StringComparison.Ordinal);
        string windows = unix.Replace("\n", "\r\n", StringComparison.Ordinal);

        Assert.Equal(SpellNoteParser.Parse(unix, "detect-magic"), SpellNoteParser.Parse(windows, "detect-magic"));
    }

    [Theory]
    [InlineData("# Goblin\n\nA small, mean creature.")]
    [InlineData("")]
    public void Parse_NoteWithoutASpellBlock_ReturnsNull(string markdown)
    {
        Assert.Null(SpellNoteParser.Parse(markdown, "goblin"));
    }

    [Fact]
    public void Parse_SpellBlockWithoutAName_ReturnsNull()
    {
        string note = $"{Fence}spell\nname: \nlevel: cantrip\n{Fence}\n\nSome text.";

        Assert.Null(SpellNoteParser.Parse(note, "nameless"));
    }

    [Fact]
    public void Parse_NoTextAfterTheBlock_GivesAnEmptyDescription()
    {
        string note = $"{Fence}spell\nname: Light\nlevel: cantrip\n{Fence}";

        Assert.Equal("", SpellNoteParser.Parse(note, "light")?.Description);
    }
}
