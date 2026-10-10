using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Core.Tests.StatBlocks;

public class StatBlockIdentityTests
{
    [Fact]
    public void ForImported_IsTheSameEveryTime()
    {
        // Pinned on purpose. If this value ever changes, every synced encounter breaks.
        Assert.Equal(
            Guid.Parse("f5a0ddb2-8732-8f7c-829c-f56400f4c48c"),
            StatBlockIdentity.ForImported("Goblin", "MM"));
    }

    [Fact]
    public void ForImported_IgnoresCaseAndSurroundingSpaces()
    {
        Assert.Equal(
            StatBlockIdentity.ForImported("Goblin", "MM"),
            StatBlockIdentity.ForImported("  goblin ", "mm"));
    }

    [Fact]
    public void ForImported_SameNameFromAnotherSource_IsADifferentCreature()
    {
        Assert.NotEqual(
            StatBlockIdentity.ForImported("Goblin", "MM"),
            StatBlockIdentity.ForImported("Goblin", "XMM"));
    }
}