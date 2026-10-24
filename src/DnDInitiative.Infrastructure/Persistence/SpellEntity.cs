namespace DnDInitiative.Infrastructure.Persistence;

internal sealed class SpellEntity
{
    /// <summary>The primary key. Text, not a Guid: it is what links in stat blocks contain.</summary>
    public string Key { get; set; } = "";

    public string Name { get; set; } = "";

    public string Level { get; set; } = "";

    public int LevelNumber { get; set; }

    public string School { get; set; } = "";

    public bool IsRitual { get; set; }

    public string CastingTime { get; set; } = "";

    public string Range { get; set; } = "";

    public string Components { get; set; } = "";

    public string Duration { get; set; } = "";

    public bool IsConcentration { get; set; }

    /// <summary>Stored as a JSON array in one column, which queries can still look inside.</summary>
    public List<string> Classes { get; set; } = [];

    public string Source { get; set; } = "";

    public string Description { get; set; } = "";
}
