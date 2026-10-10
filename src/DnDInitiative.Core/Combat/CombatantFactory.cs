using DnDInitiative.Core.Dice;

namespace DnDInitiative.Core.Combat;

/// <summary>
/// Builds ready-to-fight combatants: names them and rolls their initiative.
/// </summary>
public sealed class CombatantFactory
{
    private readonly InitiativeRoller _initiativeRoller;

    public CombatantFactory(InitiativeRoller initiativeRoller)
    {
        _initiativeRoller = initiativeRoller;
    }

    public IReadOnlyList<Combatant> Create(
        IReadOnlyList<CombatantBlueprint> blueprints,
        CombatantNamer namer,
        RollMode rollMode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(blueprints);
        ArgumentNullException.ThrowIfNull(namer);

        IReadOnlyList<string> names = namer.Assign(blueprints.Select(blueprint => blueprint.Name).ToList());

        var combatants = new List<Combatant>(blueprints.Count);
        for (int i = 0; i < blueprints.Count; i++)
        {
            CombatantBlueprint blueprint = blueprints[i];
            InitiativeRoll roll = _initiativeRoller.Roll(blueprint.InitiativeModifier, rollMode);

            combatants.Add(new Combatant(
                names[i],
                roll.Total,
                blueprint.MaxHp,
                blueprint.ArmorClass,
                blueprint.StatBlockId,
                blueprint.InitiativeModifier,
                blueprint.IsPlayer));
        }

        return combatants;
    }

    /// <summary>Rolls a new initiative total for a combatant that is already in the fight.</summary>
    public int RollInitiative(Combatant combatant, RollMode rollMode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(combatant);
        return _initiativeRoller.Roll(combatant.InitiativeModifier, rollMode).Total;
    }
}