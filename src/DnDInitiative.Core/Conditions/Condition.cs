namespace DnDInitiative.Core.Conditions;

/// <summary>
/// A condition or other status a creature can be in, such as Prone or Concentration.
/// Everything is display text.
/// </summary>
public sealed record Condition
{
    /// <summary>The condition's name, which is also what links to it contain. Compared ignoring case.</summary>
    public required string Name { get; init; }

    public string Source { get; init; } = "";

    /// <summary>The rules text, in Markdown.</summary>
    public string Description { get; init; } = "";
}
