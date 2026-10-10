using DnDInitiative.Infrastructure.Campaigns;

namespace DnDInitiative.Infrastructure.Tests.Campaigns;

public class FileNamesTests
{
    [Theory]
    [InlineData("Goblin Ambush", "goblin-ambush")]
    [InlineData("  The Dragon's Lair!  ", "the-dragon-s-lair")]
    [InlineData("a / b \\ c : d", "a-b-c-d")]
    [InlineData("Žalgiris", "žalgiris")]
    [InlineData("???", "unnamed")]
    [InlineData("", "unnamed")]
    [InlineData("CON", "con-item")]
    public void Slug_MakesASafeReadableName(string name, string expected)
    {
        Assert.Equal(expected, FileNames.Slug(name));
    }

    [Fact]
    public void Slug_VeryLongName_IsCutShort()
    {
        Assert.Equal(60, FileNames.Slug(new string('a', 200)).Length);
    }

    [Fact]
    public void UniqueFileName_NameIsFree_UsesIt()
    {
        Assert.Equal("heroes.json", FileNames.UniqueFileName("heroes", new HashSet<string>()));
    }

    [Fact]
    public void UniqueFileName_NameIsTaken_AddsTheNextFreeNumber()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Heroes.json", "heroes-2.json" };

        Assert.Equal("heroes-3.json", FileNames.UniqueFileName("heroes", taken));
    }
}