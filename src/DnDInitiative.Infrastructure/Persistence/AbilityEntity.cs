namespace DnDInitiative.Infrastructure.Persistence;

internal sealed class AbilityEntity
{
    /// <summary>The primary key: the name and the source, made into one lower-case word.</summary>
    public string Key { get; set; } = "";

    public string Name { get; set; } = "";

    public string Category { get; set; } = "";

    public int Degree { get; set; }

    public string Prerequisites { get; set; } = "";

    /// <summary>Stored as a JSON array in one column, which queries can still look inside.</summary>
    public List<string> Classes { get; set; } = [];

    public string Source { get; set; } = "";

    public string Description { get; set; } = "";
}
