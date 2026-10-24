using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// What the reference popup shows: a spell, a condition, or a note that one was not found.
/// Laid out by <see cref="Views.ReferenceCardView"/>.
/// </summary>
public interface IReferenceCard
{
    string Name { get; }

    /// <summary>The italic line under the name, such as "3rd-level Evocation" or "Condition".</summary>
    string Summary { get; }

    /// <summary>Labelled values between the two rules, such as "Range 150 feet".</summary>
    IReadOnlyList<LabeledValue> Stats { get; }

    /// <summary>The rules text.</summary>
    IReadOnlyList<MarkdownBlock> Blocks { get; }
}
