namespace DnDInitiative.Core.Combat;

/// <summary>
/// Everything needed to put one creature or player into combat.
/// </summary>
public sealed record CombatantBlueprint(
    string Name,
    int InitiativeModifier = 0,
    int? MaxHp = null,
    int? ArmorClass = null,
    Guid? StatBlockId = null,
    bool IsPlayer = false);