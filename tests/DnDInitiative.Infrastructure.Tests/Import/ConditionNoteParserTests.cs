using DnDInitiative.Core.Conditions;
using DnDInitiative.Infrastructure.Import;

namespace DnDInitiative.Infrastructure.Tests.Import;

public class ConditionNoteParserTests
{
    // The shape of rules/conditions.md as the converter writes it.
    private const string ConditionsNote = """
    ---
    obsidianUIMode: preview
    tags:
    - compendium/src/5e/xphb
    ---
    # Conditions

    ## Blinded
    _Source: Player's Handbook (2024) p. 361_

    While you have the Blinded condition, you experience the following effects.

    **Can't See.** You can't see.
    ^blinded-effects

    ## Grappled
    _Source: Player's Handbook (2024) p. 362_

    - **Speed 0.** Your Speed is 0.
    - **Movable.** See [Prone](rules/conditions.md#Prone).

    ### Escaping
    An action ends it.

    ## Concentration
    Some statuses have no source line.
    """;

    [Fact]
    public void Parse_ConditionsNote_ReadsEverySectionInOrder()
    {
        IReadOnlyList<Condition> conditions = ConditionNoteParser.Parse(ConditionsNote);

        Assert.Equal(["Blinded", "Grappled", "Concentration"], conditions.Select(condition => condition.Name).ToArray());
    }

    [Fact]
    public void Parse_SourceLine_IsTakenOutOfTheText()
    {
        Condition blinded = ConditionNoteParser.Parse(ConditionsNote)[0];

        Assert.Equal("Player's Handbook (2024) p. 361", blinded.Source);
        Assert.Equal(
            "While you have the Blinded condition, you experience the following effects.\n\n**Can't See.** You can't see.",
            blinded.Description);
    }

    [Fact]
    public void Parse_Text_KeepsSubheadingsAndListsAndTurnsConditionLinksIntoAppLinks()
    {
        Condition grappled = ConditionNoteParser.Parse(ConditionsNote)[1];

        Assert.Contains("- **Movable.** See [Prone](condition:Prone).", grappled.Description, StringComparison.Ordinal);
        Assert.Contains("### Escaping", grappled.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_SectionWithoutASourceLine_HasAnEmptySourceAndKeepsItsFirstLine()
    {
        Condition concentration = ConditionNoteParser.Parse(ConditionsNote)[2];

        Assert.Equal("", concentration.Source);
        Assert.Equal("Some statuses have no source line.", concentration.Description);
    }

    [Fact]
    public void Parse_SectionThatOnlyEmbedsAnotherNote_IsSkipped()
    {
        // What the collected note looks like when rules are split into one note each.
        string note = "# Conditions\n\n## Blinded\n\n![Blinded](blinded.md)\n";

        Assert.Empty(ConditionNoteParser.Parse(note));
    }

    [Fact]
    public void Parse_WindowsLineEndings_GiveTheSameConditions()
    {
        string unix = ConditionsNote.Replace("\r\n", "\n", StringComparison.Ordinal);
        string windows = unix.Replace("\n", "\r\n", StringComparison.Ordinal);

        Assert.Equal(ConditionNoteParser.Parse(unix), ConditionNoteParser.Parse(windows));
    }
}
