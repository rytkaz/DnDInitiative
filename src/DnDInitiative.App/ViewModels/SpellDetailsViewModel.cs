using DnDInitiative.App.Formatting;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// A spell prepared for display: text is formatted and empty parts are left out.
/// </summary>
public sealed class SpellDetailsViewModel : IReferenceCard
{
    public SpellDetailsViewModel(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);

        Name = spell.Name;
        Summary = Summarize(spell);

        var stats = new List<LabeledValue>();
        AddIfPresent(stats, "Casting Time", spell.CastingTime);
        AddIfPresent(stats, "Range", spell.Range);
        AddIfPresent(stats, "Components", spell.Components);
        AddIfPresent(stats, "Duration", spell.Duration);
        AddIfPresent(stats, "Source", spell.Source);
        Stats = stats;

        Blocks = MarkdownBlocks.Parse(spell.Description);
    }

    private SpellDetailsViewModel(string name, string summary, string explanation)
    {
        Name = name;
        Summary = summary;
        Stats = [];
        Blocks = [new MarkdownBlock(MarkdownBlockKind.Paragraph, explanation)];
    }

    public string Name { get; }

    /// <summary>The line under the name, such as "3rd-level Evocation (ritual)".</summary>
    public string Summary { get; }

    public IReadOnlyList<LabeledValue> Stats { get; }

    public IReadOnlyList<MarkdownBlock> Blocks { get; }

    /// <summary>What to show when a link names a spell that is not in the library.</summary>
    public static SpellDetailsViewModel NotFound(string reference)
    {
        return new SpellDetailsViewModel(
            reference,
            "Spell not found",
            "This spell is not in the library. Spells are brought in by Convert and import on the 5eTools Import page.");
    }

    private static string Summarize(Spell spell)
    {
        string level = spell.Level.Trim();
        string school = spell.School.Trim();

        // The books write "Evocation cantrip" but "3rd-level Evocation".
        string text = level.Equals("cantrip", StringComparison.OrdinalIgnoreCase)
            ? $"{school} {level}".Trim()
            : $"{level} {school}".Trim();

        return spell.IsRitual ? $"{text} (ritual)".Trim() : text;
    }

    private static void AddIfPresent(List<LabeledValue> target, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            target.Add(new LabeledValue(label, value.Trim()));
        }
    }
}
