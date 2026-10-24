using DnDInitiative.App.Formatting;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Conditions;

namespace DnDInitiative.App.Tests.ViewModels;

public class ConditionDetailsViewModelTests
{
    [Fact]
    public void Constructor_ShowsTheSourceAndSplitsTheTextIntoBlocks()
    {
        var viewModel = new ConditionDetailsViewModel(new Condition
        {
            Name = "Grappled",
            Source = "XPHB p. 362",
            Description = "While Grappled, you experience the following effects.\n\n- **Speed 0.** Your Speed is 0.\n- **Attacks Affected.** Disadvantage.",
        });

        Assert.Equal("Grappled", viewModel.Name);
        Assert.Equal("Condition", viewModel.Summary);
        Assert.Equal(new LabeledValue("Source", "XPHB p. 362"), Assert.Single(viewModel.Stats));
        MarkdownBlockKind[] expectedKinds = [MarkdownBlockKind.Paragraph, MarkdownBlockKind.Bullet, MarkdownBlockKind.Bullet];
        Assert.Equal(expectedKinds, viewModel.Blocks.Select(block => block.Kind).ToArray());
    }

    [Fact]
    public void Constructor_NoSource_LeavesTheStatsEmpty()
    {
        var viewModel = new ConditionDetailsViewModel(new Condition { Name = "Bleeding", Description = "You bleed." });

        Assert.Empty(viewModel.Stats);
    }

    [Fact]
    public void NotFound_NamesWhatTheLinkAskedForAndExplains()
    {
        ConditionDetailsViewModel viewModel = ConditionDetailsViewModel.NotFound("Petrified");

        Assert.Equal("Petrified", viewModel.Name);
        Assert.Equal("Condition not found", viewModel.Summary);
        Assert.Contains("Import", Assert.Single(viewModel.Blocks).Text, StringComparison.Ordinal);
    }
}
