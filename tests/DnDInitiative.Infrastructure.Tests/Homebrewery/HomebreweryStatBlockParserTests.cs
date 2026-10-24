using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Homebrewery;

namespace DnDInitiative.Infrastructure.Tests.Homebrewery;

public class HomebreweryStatBlockParserTests
{
    private const string Source = "Test Bestiary";

    // The legacy layout: a block quote, with lore and a second creature around it.
    private const string LegacyDocument = """
    # Marsh Lurkers
    <!-- Art for the lurker
    https://example.com/lurker.png
    -->
    Marsh lurkers hide in reeds. This is lore, not a stat block.

    ## Marsh Lurker Lore
    **Nature DC 12**: They hate salt.

    \page
    ___
    > ## Marsh Lurker
    >*Medium monstrosity (amphibian), neutral evil*
    > ___
    > - **Armor Class** 14 (Natural Armor), 11 While Dry
    > - **Hit Points** 45 (6d8 + 18)
    > - **Speed** 30 ft., swim 40 ft.
    >___
    >|STR|DEX|CON|INT|WIS|CHA|
    >|:---:|:---:|:---:|:---:|:---:|:---:|
    >|16 (+3)|12 (+1)|16 (+3)|5 (-3)|11 (+0)|6 (-2)|
    >___
    > - **Saving Throws** Con +5
    > - **Skills** Stealth +5
    > - **Damage Resistances** Cold
    > - **Condition Immunities** Frightened
    > - **Senses** Darkvision 60 ft., Passive Perception 10
    > - **Languages** Understands Common
    > - **Challenge** 2 (450 XP)
    > ___
    >
    > ***Amphibious.*** The lurker can breathe air and water.
    >
    > ***Reed Cloak.*** While in reeds, the lurker has advantage on Stealth checks.
    >
    > It loses this benefit while it is on fire.
    >
    > ### Actions
    > ***Multiattack.*** The lurker makes two Bite attacks.
    >
    > ***Tail Lash.*** The lurker swings its tail
    at every creature within 10 feet.
    >
    > ***Bite.*** *Melee Weapon Attack:* +5 to hit, reach 5 ft., one target. *Hit:* 7 (1d8 + 3) piercing damage, and the target is [Grappled](https://homebrewery.naturalcrit.com/share/conditions01).
    >
    > ### Reactions
    > ***Slip Away (1/Day.***) When hit by a melee attack, the lurker swims 10 feet.



    ___
    > ## Marsh Lurker Hatchling
    >*Small monstrosity (amphibian), unaligned*
    > ___
    > - **Armor Class** 12
    > - **Hit Points** 9 (2d6 + 2)
    > - **Speed** 20 ft., swim 30 ft.
    >___
    >|STR|DEX|CON|INT|WIS|CHA|
    >|:---:|:---:|:---:|:---:|:---:|:---:|
    >|8 (-1)|14 (+2)|12 (+1)|3 (-4)|10 (+0)|4 (-3)|
    >___
    > - **Senses** Passive Perception 10
    > - **Challenge** 1/8 (25 XP)
    > ___
    > ***Nip.*** *Melee Weapon Attack:* +4 to hit, reach 5 ft., one target. *Hit:* 4 (1d4 + 2) piercing damage.
    """;

    // The V3 layout with the 2014 stat lines, a legendary action preamble and spacer lines.
    private const string V3Document = """
    # Cinder Drakes

    {{monster,frame,wide

     ## Cinder Drake
    *Large dragon (fire), chaotic evil*

    ___

     - **Armor Class** 17
     - **Hit Points** 110 (13d10 + 39)
     - **Speed** 40 ft., fly 80 ft.

    ___

    |STR|DEX|CON|INT|WIS|CHA|
    |:---:|:---:|:---:|:---:|:---:|:---:|
    |19 (+4)|10 (+0)|17 (+3)|12 (+1)|11 (+0)|15 (+2)|

    ___

     - **Saving Throws** Dex +3, Con +6
     - **Damage Immunities** Fire
     - **Senses** Blindsight 30 ft., Passive Perception 13
     - **Languages** Draconic
     - **Challenge** 7 (2,900 XP)

    ___

     ***Smoulder.*** A creature that touches the drake takes 3 (1d6) fire damage.

    :

     ### Actions

    :

    ***Claw.*** *Melee Weapon Attack:* +7 to hit, reach 5 ft., one target. *Hit:* 11 (2d6 + 4) slashing damage.
    \column
    ***Ash Breath (Recharge 5-6).*** Each creature in a 30-foot cone makes a DC 14 Constitution saving throw.
    :
    On a failure it is [Choking](https://homebrewery.naturalcrit.com/share/conditions01).

    ### Bonus Actions
    ***Flare.*** The drake sheds bright light until the start of its next turn.

     ### Legendary Actions

    The drake can take 2 legendary actions, choosing from the options below.

    :

    ***Wing Buffet.*** The drake beats its wings.
    }}

    {{note
    ##### Tactics
    The drake opens with its breath.
    }}
    """;

    // The V3 layout in the 2024 style: vitals, Mod/Save tables, one immunities line.
    private const string Modern2024Document = """
    {{monster,frame

     ## Glass Sentinel {{bonus **cr 5**}}
    *A towering figure of fused green glass, humming faintly.*

    {{stats

    {{vitals

    **AC**         :: 16 (Natural Armor)
    **HP**         :: 85 (10d10 + 30)
    **Speed**      :: 25 ft.
    \column
    **Initiative** :: --1 (9)
    **Size**       :: Large (9 ft. tall)
    **Type**       :: Construct
    }}
    {{tables
    |   |   |  MOD | SAVE |
    |:--|:-:|:----:|:----:|
    |Str| 18 | +4  | +7 |
    |Int| 3 | -4  | -4 |

    |   |   |  MOD | SAVE |
    |:--|:-:|:----:|:----:|
    |Dex| 8 | -1  | -1 |
    |Wis| 10 | +0  | +0 |

    |   |   |  MOD | SAVE |
    |:--|:-:|:----:|:----:|
    |Con| 16 | +3  | +6 |
    |Cha| 1 | -5  | -5 |
    }}

    **Immunities** :: Poison, Psychic; Charmed, Poisoned
    **Senses**     :: Darkvision 60 ft., Passive Perception 10
    **Drives**     :: Guard the vault
    **Languages**  :: None
    **XP**         :: 1,800 {{bonus **Proficiency Bonus** +3}}

    }}

    ### Traits

    ***Refraction.*** Spells that target only the sentinel are reflected.

    ### Actions

    ***Shard Fist.*** *Melee Attack Roll:* +7, reach 5 ft. *Hit:* 13 (2d8 + 4) Slashing damage.
    :
    | d4 | Effect |
    |:--:|:--|
    | 1-2 | The target is pushed 5 feet. |
    | 3-4 | The target is knocked prone. |

    ### Reactions

    ***Crack.*** When it takes Thunder damage, its AC drops by 2.

    }}
    """;

    [Fact]
    public void Parse_LegacyLayout_ReadsEveryCreatureAndNothingElse()
    {
        IReadOnlyList<StatBlock> statBlocks = HomebreweryStatBlockParser.Parse(LegacyDocument, Source, ConditionLinkRewriter.None);

        string[] expected = ["Marsh Lurker (Test Bestiary)", "Marsh Lurker Hatchling (Test Bestiary)"];
        Assert.Equal(expected, statBlocks.Select(statBlock => statBlock.Name).ToArray());
    }

    [Fact]
    public void Parse_LegacyLayout_ReadsTheStatLines()
    {
        StatBlock lurker = HomebreweryStatBlockParser.Parse(LegacyDocument, Source, ConditionLinkRewriter.None)[0];

        Assert.Equal(Source, lurker.Source);
        Assert.Equal("Medium", lurker.Size);
        Assert.Equal("monstrosity (amphibian)", lurker.CreatureType);
        Assert.Equal("neutral evil", lurker.Alignment);
        Assert.Equal(14, lurker.ArmorClass);
        Assert.Equal(45, lurker.HitPoints);
        Assert.Equal("6d8 + 18", lurker.HitDice);
        Assert.Equal("30 ft., swim 40 ft.", lurker.Speed);
        Assert.Equal(new AbilityScores(16, 12, 16, 5, 11, 6), lurker.Abilities);
        Assert.Equal(1, lurker.InitiativeModifier);
        Assert.Equal("Con +5", lurker.SavingThrows);
        Assert.Equal("Stealth +5", lurker.Skills);
        Assert.Equal("Cold", lurker.DamageResistances);
        Assert.Equal("Frightened", lurker.ConditionImmunities);
        Assert.Equal("Darkvision 60 ft., Passive Perception 10", lurker.Senses);
        Assert.Equal("Understands Common", lurker.Languages);
        Assert.Equal("2", lurker.ChallengeRating);
        Assert.Equal(StatBlockOrigin.Imported, lurker.Origin);
    }

    [Fact]
    public void Parse_LegacyLayout_SortsTheFeaturesIntoTheirSections()
    {
        StatBlock lurker = HomebreweryStatBlockParser.Parse(LegacyDocument, Source, ConditionLinkRewriter.None)[0];

        string[] traits = ["Amphibious", "Reed Cloak"];
        string[] actions = ["Multiattack", "Tail Lash", "Bite"];
        string[] reactions = ["Slip Away (1/Day)"];
        Assert.Equal(traits, lurker.Traits.Select(feature => feature.Name).ToArray());
        Assert.Equal(actions, lurker.Actions.Select(feature => feature.Name).ToArray());
        Assert.Equal(reactions, lurker.Reactions.Select(feature => feature.Name).ToArray());
    }

    [Fact]
    public void Parse_FeatureOfSeveralParagraphs_KeepsThemApart()
    {
        StatBlock lurker = HomebreweryStatBlockParser.Parse(LegacyDocument, Source, ConditionLinkRewriter.None)[0];

        Assert.Equal(
            "While in reeds, the lurker has advantage on Stealth checks.\n\nIt loses this benefit while it is on fire.",
            lurker.Traits[1].Description);
    }

    [Fact]
    public void Parse_LegacyLineWithoutItsQuoteMarker_StaysInTheStatBlock()
    {
        StatBlock lurker = HomebreweryStatBlockParser.Parse(LegacyDocument, Source, ConditionLinkRewriter.None)[0];

        // Markdown carries a quoted paragraph on over a line without ">". The features after it
        // used to be lost, because the block ended there.
        Assert.Equal("The lurker swings its tail at every creature within 10 feet.", lurker.Actions[1].Description);
        Assert.Equal("When hit by a melee attack, the lurker swims 10 feet.", Assert.Single(lurker.Reactions).Description);
    }

    [Fact]
    public void Parse_ConditionNamesInTheText_BecomeConditionLinks()
    {
        var links = new ConditionLinkRewriter([], ["Frightened", "Prone", "Bleeding"]);
        const string document = """
        {{monster,frame
        ## Thorn Wolf
        **AC** :: 13
        **HP** :: 22
        **Condition Immunities** :: Frightened
        ### Actions
        ***Thorn Bite.*** The target is knocked prone and starts Bleeding.
        }}
        """;

        StatBlock wolf = Assert.Single(HomebreweryStatBlockParser.Parse(document, Source, links));

        Assert.Equal("[Frightened](condition:Frightened)", wolf.ConditionImmunities);
        Assert.Equal("The target is knocked [prone](condition:Prone) and starts [Bleeding](condition:Bleeding).", Assert.Single(wolf.Actions).Description);
        Assert.Equal("Thorn Bite", wolf.Actions[0].Name);
    }

    [Fact]
    public void Parse_FeaturesWithoutAnActionsHeading_AreTraits()
    {
        StatBlock hatchling = HomebreweryStatBlockParser.Parse(LegacyDocument, Source, ConditionLinkRewriter.None)[1];

        Assert.Equal("Nip", Assert.Single(hatchling.Traits).Name);
        Assert.Equal("1/8", hatchling.ChallengeRating);
    }

    [Fact]
    public void Parse_LinkIntoTheConditionsDocument_BecomesAConditionLink()
    {
        var links = new ConditionLinkRewriter(["conditions01"], ["Grappled", "Choking"]);

        StatBlock lurker = HomebreweryStatBlockParser.Parse(LegacyDocument, Source, links)[0];

        Assert.EndsWith("the target is [Grappled](condition:Grappled).", lurker.Actions[2].Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_V3Layout_ReadsTheStatLines()
    {
        StatBlock drake = Assert.Single(HomebreweryStatBlockParser.Parse(V3Document, Source, ConditionLinkRewriter.None));

        Assert.Equal("Cinder Drake (Test Bestiary)", drake.Name);
        Assert.Equal("Large", drake.Size);
        Assert.Equal("dragon (fire)", drake.CreatureType);
        Assert.Equal(17, drake.ArmorClass);
        Assert.Equal(110, drake.HitPoints);
        Assert.Equal(new AbilityScores(19, 10, 17, 12, 11, 15), drake.Abilities);
        Assert.Equal("Fire", drake.DamageImmunities);
        Assert.Equal("7", drake.ChallengeRating);
    }

    [Fact]
    public void Parse_V3Layout_LeavesOutLayoutAndThePreamble()
    {
        StatBlock drake = Assert.Single(HomebreweryStatBlockParser.Parse(V3Document, Source, ConditionLinkRewriter.None));

        string[] actions = ["Claw", "Ash Breath (Recharge 5-6)"];
        Assert.Equal(actions, drake.Actions.Select(feature => feature.Name).ToArray());
        Assert.Equal(
            "Each creature in a 30-foot cone makes a DC 14 Constitution saving throw.\n\nOn a failure it is [Choking](https://homebrewery.naturalcrit.com/share/conditions01).",
            drake.Actions[1].Description);
        Assert.Equal("Flare", Assert.Single(drake.BonusActions).Name);
        StatBlockFeature legendary = Assert.Single(drake.LegendaryActions);
        Assert.Equal("Wing Buffet", legendary.Name);
        Assert.Equal("The drake beats its wings.", legendary.Description);
    }

    [Fact]
    public void Parse_2024Layout_ReadsVitalsAndTables()
    {
        StatBlock sentinel = Assert.Single(HomebreweryStatBlockParser.Parse(Modern2024Document, Source, ConditionLinkRewriter.None));

        Assert.Equal("Glass Sentinel (Test Bestiary)", sentinel.Name);
        Assert.Equal("5", sentinel.ChallengeRating);
        Assert.Equal(16, sentinel.ArmorClass);
        Assert.Equal(85, sentinel.HitPoints);
        Assert.Equal("10d10 + 30", sentinel.HitDice);
        Assert.Equal(-1, sentinel.InitiativeModifier);
        Assert.Equal("Large", sentinel.Size);
        Assert.Equal("Construct", sentinel.CreatureType);
        Assert.Equal(new AbilityScores(18, 8, 16, 3, 10, 1), sentinel.Abilities);
        Assert.Equal("Strength +7, Constitution +6", sentinel.SavingThrows);
        Assert.Equal("Poison, Psychic", sentinel.DamageImmunities);
        Assert.Equal("Charmed, Poisoned", sentinel.ConditionImmunities);
        Assert.Equal("None", sentinel.Languages);
    }

    [Fact]
    public void Parse_2024Layout_ReadsTheFeaturesAfterTheStats()
    {
        StatBlock sentinel = Assert.Single(HomebreweryStatBlockParser.Parse(Modern2024Document, Source, ConditionLinkRewriter.None));

        Assert.Equal("Refraction", Assert.Single(sentinel.Traits).Name);
        Assert.Equal("Crack", Assert.Single(sentinel.Reactions).Name);
        StatBlockFeature fist = Assert.Single(sentinel.Actions);
        Assert.StartsWith("*Melee Attack Roll:* +7, reach 5 ft.", fist.Description, StringComparison.Ordinal);
        Assert.Contains("| 3-4 | The target is knocked prone. |", fist.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_NameWithLayoutSpans_KeepsOnlyTheWords()
    {
        const string document = """
        {{monster,frame
        ## Cult Fanatic of the {{width:20% }}Ember Throne {{bonus **cr M3**}}
        **AC** :: 13
        **HP** :: 9
        }}
        """;

        StatBlock fanatic = Assert.Single(HomebreweryStatBlockParser.Parse(document, Source, ConditionLinkRewriter.None));

        Assert.Equal("Cult Fanatic of the Ember Throne (Test Bestiary)", fanatic.Name);
        Assert.Equal("M3", fanatic.ChallengeRating);
    }

    [Fact]
    public void Parse_QuoteWithAHeadingButNoStats_IsNotACreature()
    {
        const string document = """
        > ## A Note on Lurkers
        > They are best used in swamps.
        """;

        Assert.Empty(HomebreweryStatBlockParser.Parse(document, Source, ConditionLinkRewriter.None));
    }

    [Fact]
    public void Parse_DocumentWithoutStatBlocks_ReturnsNothing()
    {
        Assert.Empty(HomebreweryStatBlockParser.Parse("# Encounter Tables\n\n| d6 | Encounter |", Source, ConditionLinkRewriter.None));
    }
}
