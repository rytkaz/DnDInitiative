using DnDInitiative.Core.Combat;

namespace DnDInitiative.Core.Parties;

public sealed record Party(Guid Id, string Name, IReadOnlyList<PlayerCharacter> Members)
{
    public IReadOnlyList<CombatantBlueprint> ToBlueprints()
    {
        return Members
            .Select(member => new CombatantBlueprint(member.Name, MaxHp: member.MaxHp, ArmorClass: member.ArmorClass))
            .ToList();
    }
}