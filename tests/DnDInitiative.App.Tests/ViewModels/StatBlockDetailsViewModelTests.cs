using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.ViewModels;

public class StatBlockDetailsViewModelTests
{
    [Fact]
    public void Sections_FeatureWithSeveralParagraphs_StartsEachOnANewLineWithoutAnEmptyLineBetween()
    {
        // The shape the converter gives an action that has a second paragraph.
        var viewModel = new StatBlockDetailsViewModel(new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Ratfolk Fardrop Jezzailer",
            Actions =
            [
                new StatBlockFeature(
                    "Crudebore Jezzail",
                    "*Ranged Attack Roll:* +6 range 40/120 ft. *Hit:* 10 piercing damage.\n\nOn a roll of 1, the jezzail misfires."),
            ],
        });

        LabeledValue action = Assert.Single(Assert.Single(viewModel.Sections).Entries);
        Assert.Equal("Crudebore Jezzail.", action.Label);
        Assert.Equal(
            "*Ranged Attack Roll:* +6 range 40/120 ft. *Hit:* 10 piercing damage.\nOn a roll of 1, the jezzail misfires.",
            action.Value);
    }

    [Theory]
    [InlineData("One.\r\n\r\nTwo.", "One.\nTwo.")]
    [InlineData("One.\n \n\n  Two.  ", "One.\nTwo.")]
    [InlineData("One.\nTwo.", "One.\nTwo.")]
    [InlineData("Just one paragraph.", "Just one paragraph.")]
    [InlineData("", "")]
    public void Sections_FeatureDescription_HasNoEmptyLinesWhateverTheLineEndings(string description, string expected)
    {
        var viewModel = new StatBlockDetailsViewModel(new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Goblin",
            Traits = [new StatBlockFeature("Nimble Escape", description)],
        });

        Assert.Equal(expected, Assert.Single(Assert.Single(viewModel.Sections).Entries).Value);
    }

    [Fact]
    public void Sections_OnlyIncludeTheKindsOfFeatureTheCreatureHas()
    {
        var viewModel = new StatBlockDetailsViewModel(new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Goblin",
            Traits = [new StatBlockFeature("Nimble Escape", "Disengage or Hide.")],
            Reactions = [new StatBlockFeature("Redirect Attack", "Swaps places.")],
        });

        string[] expected = ["Traits", "Reactions"];
        Assert.Equal(expected, viewModel.Sections.Select(section => section.Title).ToArray());
    }
}
