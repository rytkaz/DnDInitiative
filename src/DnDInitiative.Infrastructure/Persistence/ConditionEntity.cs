namespace DnDInitiative.Infrastructure.Persistence;

internal sealed class ConditionEntity
{
    /// <summary>The primary key. Links name the condition they point at.</summary>
    public string Name { get; set; } = "";

    public string Source { get; set; } = "";

    public string Description { get; set; } = "";
}
