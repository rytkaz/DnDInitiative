using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Import;

namespace DnDInitiative.Infrastructure.Tests.Import;

public class FantasyStatblockParserTests
{
    private const string Fence = "```";

    private const string GoblinNote = $"""
    ---
    aliases:
    - Goblin Warrior
    ---
    # Goblin Warrior

    {Fence}statblock
    name: Goblin Warrior
    size: Small
    type: Fey
    subtype: Goblinoid
    alignment: Chaotic Neutral
    ac: 15
    ac_class: leather armor, shield
    hp: 10
    hit_dice: 3d6
    modifier: 4
    stats: [8, 15, 10, 10, 8, 8]
    speed: 30 ft.
    saves:
      - dexterity: 4
    skillsaves:
      - name: "[Stealth](rules/skills.md#Stealth)"
        desc: "+6"
    senses: darkvision 60 ft., passive Perception 9
    languages: Common, Goblin
    cr: "1/4"
    traits:
      - name: Nimble Escape
        desc: The goblin takes the Disengage or Hide action.
    actions:
      - name: Scimitar
        desc: "Melee attack: +4 to hit. 5 slashing damage."
      - name: Shortbow
        desc: "Ranged attack: +4 to hit. 5 piercing damage."
    source: MM
    {Fence}
    ^statblock
    """;

    [Fact]
    public void ParseMarkdown_NoteWithStatblock_ReadsAllFields()
    {
        StatBlock goblin = Assert.Single(FantasyStatblockParser.ParseMarkdown(GoblinNote));

        Assert.Equal("Goblin Warrior", goblin.Name);
        Assert.Equal("MM", goblin.Source);
        Assert.Equal("Small", goblin.Size);
        Assert.Equal("Fey (Goblinoid)", goblin.CreatureType);
        Assert.Equal(15, goblin.ArmorClass);
        Assert.Equal(10, goblin.HitPoints);
        Assert.Equal("3d6", goblin.HitDice);
        Assert.Equal(4, goblin.InitiativeModifier);
        Assert.Equal(new AbilityScores(8, 15, 10, 10, 8, 8), goblin.Abilities);
        Assert.Equal("Dexterity +4", goblin.SavingThrows);
        Assert.Equal("Stealth +6", goblin.Skills);
        Assert.Equal("1/4", goblin.ChallengeRating);
        Assert.Equal("Nimble Escape", Assert.Single(goblin.Traits).Name);
        Assert.Equal(2, goblin.Actions.Count);
        Assert.Empty(goblin.Reactions);
    }

    [Fact]
    public void ParseYaml_NoInitiativeModifier_FallsBackToDexterityModifier()
    {
        StatBlock? statBlock = FantasyStatblockParser.ParseYaml("""
            name: Scout
            stats: [10, 16, 10, 10, 10, 10]
            """);

        Assert.Equal(3, statBlock?.InitiativeModifier);
    }

    [Fact]
    public void ParseYaml_HitPointsWrittenAsText_ReadsTheLeadingNumber()
    {
        StatBlock? statBlock = FantasyStatblockParser.ParseYaml("""
            name: Ogre
            hp: 59 (7d10 + 21)
            """);

        Assert.Equal(59, statBlock?.HitPoints);
    }

    [Fact]
    public void ParseYaml_SpellLinksInADescription_AreKeptAsSpellLinksAndOtherLinksBecomeText()
    {
        // What the converter writes for a caster: the spell list arrives inside a trait.
        StatBlock? statBlock = FantasyStatblockParser.ParseYaml("""
            name: Mage
            traits:
              - name: Spellcasting
                desc: "The mage casts spells using [Intelligence](rules/abilities.md#Intelligence).\n\n**At will:** [detect magic](compendium/spells/detect-magic.md), [Fireball](compendium/spells/fireball-xphb.md)"
            """);

        Assert.Equal(
            "The mage casts spells using Intelligence.\n\n**At will:** [detect magic](spell:detect-magic), [Fireball](spell:fireball-xphb)",
            Assert.Single(statBlock!.Traits).Description);
    }

    [Fact]
    public void ParseMarkdown_NoteWithoutStatblock_ReturnsNothing()
    {
        Assert.Empty(FantasyStatblockParser.ParseMarkdown("# Fireball\n\nA bright streak flashes..."));
    }

    [Fact]
    public void ParseYaml_BlockThatOnlyReferencesACreature_ReturnsNull()
    {
        Assert.Null(FantasyStatblockParser.ParseYaml("creature: Goblin"));
    }

    [Fact]
    public void ParseYaml_MalformedYaml_ReturnsNull()
    {
        Assert.Null(FantasyStatblockParser.ParseYaml("name: [unclosed"));
    }

    [Fact]
    public void ParseYaml_LegendaryMythicAndLairActions_AreReadWithTheirDescriptions()
    {
        const string yaml = """
        name: Swamp Wyrm
        legendary_description: "Legendary Action Uses: 3. Immediately after another creature's turn, the wyrm can expend a use."
        legendary_actions:
          - name: Tail Swipe
            desc: The wyrm swipes.
        mythic_description: If the wyrm's Mythic trait is active, it can use the options below.
        mythic_actions:
          - name: Rot
            desc: Plants wither.
        lair_actions:
          - name: Fog
            desc: Fog rolls in.
        """;

        StatBlock wyrm = Assert.IsType<StatBlock>(FantasyStatblockParser.ParseYaml(yaml));

        Assert.Equal("Legendary Action Uses: 3. Immediately after another creature's turn, the wyrm can expend a use.", wyrm.LegendaryActionsDescription);
        Assert.Equal("Tail Swipe", Assert.Single(wyrm.LegendaryActions).Name);
        Assert.Equal("If the wyrm's Mythic trait is active, it can use the options below.", wyrm.MythicActionsDescription);
        Assert.Equal("Rot", Assert.Single(wyrm.MythicActions).Name);
        Assert.Equal("Fog", Assert.Single(wyrm.LairActions).Name);
        Assert.Equal("", wyrm.LairActionsDescription);
    }
}
