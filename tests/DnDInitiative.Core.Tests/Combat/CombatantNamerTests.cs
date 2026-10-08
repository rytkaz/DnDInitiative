using DnDInitiative.Core.Combat;

namespace DnDInitiative.Core.Tests.CombatTests;

public class CombatantNamerTests
{
    [Fact]
    public void Assign_UniqueNames_LeavesThemUnchanged()
    {
        var namer = new CombatantNamer();

        IReadOnlyList<string> names = Assign(namer, "Goblin", "Orc");

        AssertNames(names, "Goblin", "Orc");
    }

    [Fact]
    public void Assign_DuplicateNames_NumbersThemInOrder()
    {
        var namer = new CombatantNamer();

        IReadOnlyList<string> names = Assign(namer, "Goblin", "Orc", "Goblin", "Goblin");

        AssertNames(names, "Goblin 1", "Orc", "Goblin 2", "Goblin 3");
    }

    [Fact]
    public void Assign_NameIssuedInEarlierBatch_ContinuesNumbering()
    {
        var namer = new CombatantNamer();
        Assign(namer, "Goblin");

        IReadOnlyList<string> names = Assign(namer, "Goblin");

        AssertNames(names, "Goblin 2");
    }

    [Fact]
    public void Assign_NamesDifferingOnlyByCase_AreTreatedAsDuplicates()
    {
        var namer = new CombatantNamer();

        IReadOnlyList<string> names = Assign(namer, "Goblin", "goblin");

        AssertNames(names, "Goblin 1", "goblin 2");
    }

    private static IReadOnlyList<string> Assign(CombatantNamer namer, params string[] baseNames)
    {
        return namer.Assign(baseNames);
    }

    private static void AssertNames(IReadOnlyList<string> actual, params string[] expected)
    {
        Assert.Equal<string>(expected, actual);
    }
}