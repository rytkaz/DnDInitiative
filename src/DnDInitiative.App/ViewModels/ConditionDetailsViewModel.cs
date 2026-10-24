using DnDInitiative.App.Formatting;
using DnDInitiative.Core.Conditions;

namespace DnDInitiative.App.ViewModels;

/// <summary>A condition prepared for display in the reference popup.</summary>
public sealed class ConditionDetailsViewModel : IReferenceCard
{
    public ConditionDetailsViewModel(Condition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        Name = condition.Name;
        Summary = "Condition";
        Stats = string.IsNullOrWhiteSpace(condition.Source) ? [] : [new LabeledValue("Source", condition.Source.Trim())];
        Blocks = MarkdownBlocks.Parse(condition.Description);
    }

    private ConditionDetailsViewModel(string name, string summary, string explanation)
    {
        Name = name;
        Summary = summary;
        Stats = [];
        Blocks = [new MarkdownBlock(MarkdownBlockKind.Paragraph, explanation)];
    }

    public string Name { get; }

    public string Summary { get; }

    public IReadOnlyList<LabeledValue> Stats { get; }

    public IReadOnlyList<MarkdownBlock> Blocks { get; }

    /// <summary>What to show when a link names a condition that is not in the library.</summary>
    public static ConditionDetailsViewModel NotFound(string name)
    {
        return new ConditionDetailsViewModel(
            name,
            "Condition not found",
            "This condition is not in the library. Conditions are brought in by the 5eTools Import and Homebrewery Import pages.");
    }
}
