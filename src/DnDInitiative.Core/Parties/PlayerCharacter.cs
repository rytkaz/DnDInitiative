namespace DnDInitiative.Core.Parties;

public sealed record PlayerCharacter(
    string Name,
    int Level,
    int? MaxHp = null,
    int? ArmorClass = null);