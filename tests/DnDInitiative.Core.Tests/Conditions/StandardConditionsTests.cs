using DnDInitiative.Core.Conditions;

namespace DnDInitiative.Core.Tests.Conditions;

public class StandardConditionsTests
{
    [Theory]
    [InlineData("Prone")]
    [InlineData("prone")]
    [InlineData(" Grappled ")]
    [InlineData("Exhaustion")]
    [InlineData("Incapacitated")]
    public void IsStandard_OneOfTheFifteen_IsTrue(string name)
    {
        Assert.True(StandardConditions.IsStandard(name));
    }

    [Theory]
    [InlineData("Bleeding")]
    [InlineData("Hidden")]
    [InlineData("Grapple")]
    [InlineData("")]
    public void IsStandard_AnythingElse_IsFalse(string name)
    {
        Assert.False(StandardConditions.IsStandard(name));
    }

    [Fact]
    public void IsStandard_NoName_IsFalse()
    {
        Assert.False(StandardConditions.IsStandard(null));
    }
}
