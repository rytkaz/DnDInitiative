namespace DnDInitiative.Infrastructure.Persistence;

internal sealed class PartyEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public List<PlayerCharacterEntity> Members { get; set; } = [];
}

internal sealed class PlayerCharacterEntity
{
    public int Id { get; set; }

    public Guid PartyId { get; set; }

    /// <summary>The member's place in the party, since table rows have no inherent order.</summary>
    public int Position { get; set; }

    public string Name { get; set; } = "";

    public int Level { get; set; }

    public int? MaxHp { get; set; }

    public int? ArmorClass { get; set; }
}