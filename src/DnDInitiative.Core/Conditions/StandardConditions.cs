namespace DnDInitiative.Core.Conditions;

/// <summary>The conditions every edition's rules define.</summary>
public static class StandardConditions
{
    private static readonly HashSet<string> s_names = new(StringComparer.OrdinalIgnoreCase)
    {
        "Blinded", "Charmed", "Deafened", "Exhaustion", "Frightened", "Grappled", "Incapacitated", "Invisible",
        "Paralyzed", "Petrified", "Poisoned", "Prone", "Restrained", "Stunned", "Unconscious",
    };

    /// <summary>Whether the name is one of the fifteen standard conditions, ignoring case.</summary>
    public static bool IsStandard(string? name)
    {
        return name is not null && s_names.Contains(name.Trim());
    }
}
