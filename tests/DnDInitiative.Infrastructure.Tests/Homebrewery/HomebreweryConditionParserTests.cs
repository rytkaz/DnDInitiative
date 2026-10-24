using DnDInitiative.Core.Conditions;
using DnDInitiative.Infrastructure.Homebrewery;

namespace DnDInitiative.Infrastructure.Tests.Homebrewery;

public class HomebreweryConditionParserTests
{
    private const string Source = "Test Bestiary";

    // Homebrewery documents often have stray fence lines between sections. Built here because
    // no line of C# source may start with three backticks.
    private const string Fence = "```";

    // The shape of a Homebrewery conditions appendix: a title, an introduction, a table of
    // contents, then one "###" section per condition, with page breaks and stray layout between.
    private const string ConditionsDocument = $"""
    # Appendix Z: Conditions

    Conditions change what a creature can do.

    > **Design Note.** Keep it simple.

    ### Save Ends

    Some conditions let the creature repeat the save.

    <div class='toc'>
    ##### Table Of Contents
    - **[Mired ](#p2)**
    - **[Sparked ](#p2)**
    </div>

    \page

    ### Mired

    A mired creature's speed is halved.

    **Ending the Mired Condition.** Spend an action to wade free.
    {Fence}
    {Fence}

    ### Sparked/Sparking

    A sparked creature sheds dim light.

    ##### Spark Effects
    | d4 <div style="width:28px"></div> | Effect |
    |:----:|:-----|
    | 1-2 | Nothing. |
    | 3-4 | It takes 1 lightning damage. |

    > **Variant: Bright Sparks.**
    >
    > The light is bright instead.

    <!-- Use art here -->
    ### Empty

    \column
    """;

    [Fact]
    public void IsConditionsDocument_TitleEndsWithConditions_IsTrue()
    {
        Assert.True(HomebreweryConditionParser.IsConditionsDocument(ConditionsDocument));
    }

    [Theory]
    [InlineData("# Marsh Lurkers\n\n### Conditions they cause")]
    [InlineData("No title at all")]
    public void IsConditionsDocument_AnyOtherDocument_IsFalse(string markdown)
    {
        Assert.False(HomebreweryConditionParser.IsConditionsDocument(markdown));
    }

    [Fact]
    public void Parse_ReadsEverySectionWithText()
    {
        IReadOnlyList<Condition> conditions = HomebreweryConditionParser.Parse(ConditionsDocument, Source);

        string[] expected = ["Save Ends", "Mired", "Sparked", "Sparking"];
        Assert.Equal(expected, conditions.Select(condition => condition.Name).ToArray());
        Assert.All(conditions, condition => Assert.Equal(Source, condition.Source));
    }

    [Fact]
    public void Parse_TableOfContents_IsNotPartOfTheText()
    {
        Condition saveEnds = HomebreweryConditionParser.Parse(ConditionsDocument, Source)[0];

        Assert.Equal("Some conditions let the creature repeat the save.", saveEnds.Description);
    }

    [Fact]
    public void Parse_LayoutLines_AreLeftOut()
    {
        Condition mired = HomebreweryConditionParser.Parse(ConditionsDocument, Source)[1];

        Assert.Equal(
            "A mired creature's speed is halved.\n\n**Ending the Mired Condition.** Spend an action to wade free.",
            mired.Description);
    }

    [Fact]
    public void Parse_TwoNamesInOneHeading_GiveTwoConditionsWithTheSameText()
    {
        IReadOnlyList<Condition> conditions = HomebreweryConditionParser.Parse(ConditionsDocument, Source);

        Assert.Equal(conditions[2].Description, conditions[3].Description);
    }

    [Fact]
    public void Parse_TablesAndQuotes_AreKeptWithoutTheirHtml()
    {
        Condition sparked = HomebreweryConditionParser.Parse(ConditionsDocument, Source)[2];

        Assert.Contains("##### Spark Effects\n| d4  | Effect |", sparked.Description, StringComparison.Ordinal);
        Assert.Contains("> **Variant: Bright Sparks.**\n\n> The light is bright instead.", sparked.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("<", sparked.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("art here", sparked.Description, StringComparison.Ordinal);
    }
}
