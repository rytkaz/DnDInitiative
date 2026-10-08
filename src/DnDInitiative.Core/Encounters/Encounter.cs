using DnDInitiative.Core.Combat;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Core.Encounters;

/// <summary>
/// One line of an encounter: a stat block, how many, and an optional name to show in combat.
/// </summary>
public sealed record EncounterCreature(Guid StatBlockId, int Quantity = 1, string? CustomName = null);

public sealed record Encounter(Guid Id, string Name, IReadOnlyList<EncounterCreature> Creatures)
{
    /// <summary>
    /// Expands the encounter into one blueprint per creature. Lines whose stat block is missing are skipped.
    /// </summary>
    public IReadOnlyList<CombatantBlueprint> ToBlueprints(IReadOnlyDictionary<Guid, StatBlock> statBlocks)
    {
        ArgumentNullException.ThrowIfNull(statBlocks);

        var blueprints = new List<CombatantBlueprint>();
        foreach (EncounterCreature creature in Creatures)
        {
            if (!statBlocks.TryGetValue(creature.StatBlockId, out StatBlock? statBlock))
            {
                continue;
            }

            string? displayName = string.IsNullOrWhiteSpace(creature.CustomName) ? null : creature.CustomName.Trim();
            CombatantBlueprint blueprint = statBlock.ToBlueprint(displayName);
            for (int i = 0; i < creature.Quantity; i++)
            {
                blueprints.Add(blueprint);
            }
        }

        return blueprints;
    }

    public int TotalXp(IReadOnlyDictionary<Guid, StatBlock> statBlocks)
    {
        ArgumentNullException.ThrowIfNull(statBlocks);

        int total = 0;
        foreach (EncounterCreature creature in Creatures)
        {
            if (statBlocks.TryGetValue(creature.StatBlockId, out StatBlock? statBlock)
                && ChallengeRatings.TryGetXp(statBlock.ChallengeRating, out int xp))
            {
                total += xp * Math.Max(0, creature.Quantity);
            }
        }

        return total;
    }
}