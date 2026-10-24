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

    [Fact]
    public void Sections_LegendaryDescription_ComesFirstWithoutALabel()
    {
        var viewModel = new StatBlockDetailsViewModel(new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Adult Black Dragon",
            LegendaryActionsDescription = "Legendary Action Uses: 3. Immediately after another creature's turn, the dragon can expend a use.\n\nIt regains all uses at the start of its turns.",
            LegendaryActions = [new StatBlockFeature("Pounce", "The dragon moves.")],
        });

        FeatureSection legendary = Assert.Single(viewModel.Sections);
        Assert.Equal("Legendary Actions", legendary.Title);
        Assert.Equal(
            new LabeledValue("", "Legendary Action Uses: 3. Immediately after another creature's turn, the dragon can expend a use.\nIt regains all uses at the start of its turns."),
            legendary.Entries[0]);
        Assert.Equal("Pounce.", legendary.Entries[1].Label);
    }

    [Fact]
    public void Sections_MythicAndLairActions_FollowTheLegendaryActions()
    {
        var viewModel = new StatBlockDetailsViewModel(new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Bog Tyrant",
            LairActions = [new StatBlockFeature("Swamp Gas", "A cloud rises.")],
            MythicActions = [new StatBlockFeature("Mire", "Difficult terrain.")],
            LegendaryActions = [new StatBlockFeature("Stomp", "The ground shakes.")],
        });

        string[] expected = ["Legendary Actions", "Mythic Actions", "Lair Actions"];
        Assert.Equal(expected, viewModel.Sections.Select(section => section.Title).ToArray());
    }

    [Fact]
    public void Sections_DescriptionWithoutActions_IsStillShown()
    {
        var viewModel = new StatBlockDetailsViewModel(new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = "Bog Tyrant",
            LairActionsDescription = "The swamp itself fights for the tyrant.",
        });

        FeatureSection lair = Assert.Single(viewModel.Sections);
        Assert.Equal("Lair Actions", lair.Title);
        Assert.Equal("The swamp itself fights for the tyrant.", Assert.Single(lair.Entries).Value);
    }
}
