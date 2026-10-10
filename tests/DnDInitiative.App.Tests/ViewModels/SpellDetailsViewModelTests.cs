using DnDInitiative.App.Formatting;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.Tests.ViewModels;

public class SpellDetailsViewModelTests
{
    [Theory]
    [InlineData("3rd-level", "Evocation", false, "3rd-level Evocation")]
    [InlineData("1st-level", "Divination", true, "1st-level Divination (ritual)")]
    [InlineData("cantrip", "Evocation", false, "Evocation cantrip")]
    [InlineData("", "", false, "")]
    public void Summary_PutsLevelAndSchoolInTheBooksOrder(string level, string school, bool isRitual, string expected)
    {
        var viewModel = new SpellDetailsViewModel(new Spell
        {
            Key = "x",
            Name = "X",
            Level = level,
            School = school,
            IsRitual = isRitual,
        });

        Assert.Equal(expected, viewModel.Summary);
    }

    [Fact]
    public void Stats_LeaveOutWhatTheSpellDoesNotHave()
    {
        var viewModel = new SpellDetailsViewModel(new Spell
        {
            Key = "fireball",
            Name = "Fireball",
            CastingTime = "1 action",
            Range = "150 feet",
            Duration = "  ",
        });

        LabeledValue[] expected = [new("Casting Time", "1 action"), new("Range", "150 feet")];
        Assert.Equal<LabeledValue>(expected, viewModel.Stats);
    }

    [Fact]
    public void Blocks_AreTheDescriptionSplitIntoParagraphs()
    {
        var viewModel = new SpellDetailsViewModel(new Spell
        {
            Key = "fireball",
            Name = "Fireball",
            Description = "A bright streak flashes.\n\nEach creature makes a save.",
        });

        Assert.Equal(2, viewModel.Blocks.Count);
        Assert.All(viewModel.Blocks, block => Assert.Equal(MarkdownBlockKind.Paragraph, block.Kind));
    }

    [Fact]
    public void NotFound_NamesWhatTheLinkAskedForAndExplains()
    {
        SpellDetailsViewModel viewModel = SpellDetailsViewModel.NotFound("fireball-xphb");

        Assert.Equal("fireball-xphb", viewModel.Name);
        Assert.Equal("Spell not found", viewModel.Summary);
        Assert.Empty(viewModel.Stats);
        Assert.Contains("Import", Assert.Single(viewModel.Blocks).Text, StringComparison.Ordinal);
    }
}
