using DnDInitiative.Core.Combat;

namespace DnDInitiative.Core.StatBlocks;

public sealed record StatBlock
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string Source { get; init; } = "";

    public string Size { get; init; } = "";

    public string CreatureType { get; init; } = "";

    public string Alignment { get; init; } = "";

    public int ArmorClass { get; init; } = 10;

    public int HitPoints { get; init; } = 1;

    public string HitDice { get; init; } = "";

    public int InitiativeModifier { get; init; }

    public string Speed { get; init; } = "";

    public AbilityScores Abilities { get; init; } = new(10, 10, 10, 10, 10, 10);

    public string SavingThrows { get; init; } = "";

    public string Skills { get; init; } = "";

    public string DamageVulnerabilities { get; init; } = "";

    public string DamageResistances { get; init; } = "";

    public string DamageImmunities { get; init; } = "";

    public string ConditionImmunities { get; init; } = "";

    public string Senses { get; init; } = "";

    public string Languages { get; init; } = "";

    /// <summary>Kept as text because of fractions such as "1/4".</summary>
    public string ChallengeRating { get; init; } = "0";

    public IReadOnlyList<StatBlockFeature> Traits { get; init; } = [];

    public IReadOnlyList<StatBlockFeature> Actions { get; init; } = [];

    public IReadOnlyList<StatBlockFeature> BonusActions { get; init; } = [];

    public IReadOnlyList<StatBlockFeature> Reactions { get; init; } = [];

    public IReadOnlyList<StatBlockFeature> LegendaryActions { get; init; } = [];

    /// <summary>
    /// Creates what the tracker needs to put this creature into combat.
    /// </summary>
    public CombatantBlueprint ToBlueprint(string? displayName = null)
    {
        return new CombatantBlueprint(displayName ?? Name, InitiativeModifier, HitPoints, ArmorClass);
    }
}