using DnDInitiative.Infrastructure.Homebrewery;

namespace DnDInitiative.Infrastructure.Tests.Homebrewery;

public class ConditionLinkRewriterTests
{
    private const string ConditionsLink = "https://homebrewery.naturalcrit.com/share/conditions01";

    private static readonly string[] s_names = ["Bleeding", "Grappled", "Prone", "Limb Damage", "Flanked", "Flanking", "Cursed"];

    private readonly ConditionLinkRewriter _rewriter = new(["conditions01"], s_names);

    [Fact]
    public void Rewrite_LinkIntoTheConditionsDocument_BecomesAConditionLink()
    {
        string text = $"The target is knocked [Prone]({ConditionsLink}).";

        Assert.Equal("The target is knocked [Prone](condition:Prone).", _rewriter.Rewrite(text));
    }

    [Fact]
    public void Rewrite_AnotherFormOfTheName_FindsTheCondition()
    {
        string text = $"The lurker can [Grapple]({ConditionsLink}) it.";

        Assert.Equal("The lurker can [Grapple](condition:Grappled) it.", _rewriter.Rewrite(text));
    }

    [Fact]
    public void Rewrite_PageAnchorOnTheLink_IsIgnored()
    {
        string text = $"[Bleeding]({ConditionsLink}#p3)";

        Assert.Equal("[Bleeding](condition:Bleeding)", _rewriter.Rewrite(text));
    }

    [Fact]
    public void Rewrite_LinkToAnotherDocument_StaysAWebLink()
    {
        const string text = "See the [Marsh Lurker](https://homebrewery.naturalcrit.com/share/creatures02).";

        Assert.Equal(text, _rewriter.Rewrite(text));
    }

    [Fact]
    public void Rewrite_TextMatchingNoCondition_StaysAWebLink()
    {
        string text = $"[Devoured]({ConditionsLink})";

        Assert.Equal(text, _rewriter.Rewrite(text));
    }

    [Fact]
    public void Rewrite_NoConditionsDocument_ChangesNothing()
    {
        string text = $"[Prone]({ConditionsLink})";

        Assert.Equal(text, ConditionLinkRewriter.None.Rewrite(text));
    }

    [Theory]
    [InlineData("prone", "Prone")]
    [InlineData("Grapple", "Grappled")]
    [InlineData("Bleed", "Bleeding")]
    [InlineData("Cursing", "Cursed")]
    [InlineData("Flanking", "Flanking")]
    [InlineData("lose a limb", "Limb Damage")]
    [InlineData("limb damage", "Limb Damage")]
    public void FindName_CloseEnough_ReturnsTheCondition(string linkText, string expected)
    {
        Assert.Equal(expected, ConditionLinkRewriter.FindName(linkText, s_names));
    }

    [Theory]
    [InlineData("Pro")]
    [InlineData("Graceful")]
    [InlineData("")]
    public void FindName_NotCloseEnough_ReturnsNull(string linkText)
    {
        Assert.Null(ConditionLinkRewriter.FindName(linkText, s_names));
    }

    [Fact]
    public void LinkNames_StandardConditionInAnyCase_IsLinked()
    {
        var rewriter = new ConditionLinkRewriter([], ["Prone"]);

        Assert.Equal("Knocked [prone](condition:Prone), or [Prone](condition:Prone).", rewriter.LinkNames("Knocked prone, or Prone."));
    }

    [Fact]
    public void LinkNames_HomebrewCondition_IsLinkedOnlyWhenCapitalised()
    {
        var rewriter = new ConditionLinkRewriter([], ["Hidden"]);

        Assert.Equal("The hidden door makes it [Hidden](condition:Hidden).", rewriter.LinkNames("The hidden door makes it Hidden."));
    }

    [Fact]
    public void LinkNames_HomebrewNameStartingALongerName_IsNotLinked()
    {
        var rewriter = new ConditionLinkRewriter([], ["Burning", "Prone"]);

        Assert.Equal(
            "Its Burning Lash leaves the target [Burning](condition:Burning), and [Prone](condition:Prone) Targets too.",
            rewriter.LinkNames("Its Burning Lash leaves the target Burning, and Prone Targets too."));
    }

    [Fact]
    public void LinkNames_PartOfALongerWord_IsNotLinked()
    {
        var rewriter = new ConditionLinkRewriter([], ["Prone"]);

        Assert.Equal("Pronefall and Proneness.", rewriter.LinkNames("Pronefall and Proneness."));
    }

    [Fact]
    public void LinkNames_LongerNameContainingAShorterOne_LinksTheLongerName()
    {
        var rewriter = new ConditionLinkRewriter([], ["Limb", "Limb Damage"]);

        Assert.Equal("It takes [Limb Damage](condition:Limb Damage).", rewriter.LinkNames("It takes Limb Damage."));
    }

    [Fact]
    public void LinkNames_TextInsideALink_IsLeftAlone()
    {
        var rewriter = new ConditionLinkRewriter([], ["Prone", "Grappled"]);
        const string text = "[Prone](condition:Prone) and [Grappled Foes](https://example.com).";

        Assert.Equal(text, rewriter.LinkNames(text));
    }

    [Fact]
    public void LinkNames_ExceptedCondition_IsNotLinked()
    {
        var rewriter = new ConditionLinkRewriter([], ["Dazed", "Prone"]);

        Assert.Equal("A Dazed creature falls [Prone](condition:Prone).", rewriter.LinkNames("A Dazed creature falls Prone.", except: "Dazed"));
    }

    [Fact]
    public void LinkNames_NoConditionsKnown_ChangesNothing()
    {
        Assert.Equal("Knocked prone.", ConditionLinkRewriter.None.LinkNames("Knocked prone."));
    }
}
