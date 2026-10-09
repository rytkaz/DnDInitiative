namespace DnDInitiative.Infrastructure.Persistence;

internal sealed class EncounterEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public List<EncounterCreatureEntity> Creatures { get; set; } = [];
}

internal sealed class EncounterCreatureEntity
{
    public int Id { get; set; }

    public Guid EncounterId { get; set; }

    public int Position { get; set; }

    /// <summary>Deliberately not a foreign key: an encounter may outlive a stat block it refers to.</summary>
    public Guid StatBlockId { get; set; }

    public int Quantity { get; set; }

    public string? CustomName { get; set; }
}