using DnDInitiative.Core.Conditions;

namespace DnDInitiative.Core.Tests.Conditions;

public class ConditionSourcesTests
{
    [Theory]
    [InlineData("XPHB p. 367")]
    [InlineData("PHB p. 290")]
    [InlineData("Player's Handbook (2024) p. 361")]
    [InlineData("Player’s Handbook p. 290")]
    [InlineData("SRD 5.2")]
    [InlineData("System Reference Document 5.1")]
    [InlineData("xphb")]
    public void IsCoreRules_PlayersHandbookOrSrd_IsTrue(string source)
    {
        Assert.True(ConditionSources.IsCoreRules(source));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Conflux Creatures")]
    [InlineData("DMG p. 12")]
    [InlineData("XDMG")]
    [InlineData("Hazards and Shards")]
    [InlineData("PHBrew")]
    public void IsCoreRules_AnythingElse_IsFalse(string source)
    {
        Assert.False(ConditionSources.IsCoreRules(source));
    }

    [Fact]
    public void IsCoreRules_NoSource_IsFalse()
    {
        Assert.False(ConditionSources.IsCoreRules(null));
    }
}
