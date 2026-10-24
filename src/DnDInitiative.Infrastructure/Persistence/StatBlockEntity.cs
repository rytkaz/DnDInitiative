using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Infrastructure.Persistence;

internal sealed class StatBlockEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string Source { get; set; } = "";

    public string Size { get; set; } = "";

    public string CreatureType { get; set; } = "";

    public string Alignment { get; set; } = "";

    public int ArmorClass { get; set; }

    public int HitPoints { get; set; }

    public string HitDice { get; set; } = "";

    public int InitiativeModifier { get; set; }

    public string Speed { get; set; } = "";

    public int Strength { get; set; }

    public int Dexterity { get; set; }

    public int Constitution { get; set; }

    public int Intelligence { get; set; }

    public int Wisdom { get; set; }

    public int Charisma { get; set; }

    public string SavingThrows { get; set; } = "";

    public string Skills { get; set; } = "";

    public string DamageVulnerabilities { get; set; } = "";

    public string DamageResistances { get; set; } = "";

    public string DamageImmunities { get; set; } = "";

    public string ConditionImmunities { get; set; } = "";

    public string Senses { get; set; } = "";

    public string Languages { get; set; } = "";

    public string ChallengeRating { get; set; } = "";

    public StatBlockOrigin Origin { get; set; }

    // Each of these holds a JSON array of { Name, Description } objects.
    public string TraitsJson { get; set; } = "[]";

    public string ActionsJson { get; set; } = "[]";

    public string BonusActionsJson { get; set; } = "[]";

    public string ReactionsJson { get; set; } = "[]";

    public string LegendaryActionsJson { get; set; } = "[]";

    public string MythicActionsJson { get; set; } = "[]";

    public string LairActionsJson { get; set; } = "[]";

    // The text before each of these sections, in Markdown.
    public string LegendaryActionsDescription { get; set; } = "";

    public string MythicActionsDescription { get; set; } = "";

    public string LairActionsDescription { get; set; } = "";
}