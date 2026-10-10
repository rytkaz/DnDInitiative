using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

using DnDInitiative.Core.StatBlocks;

using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Reads stat blocks written in the Fantasy Statblocks YAML format, as produced by ttrpg-convert.
/// </summary>
public static partial class FantasyStatblockParser
{
    private static readonly IDeserializer s_deserializer = new DeserializerBuilder().Build();

    /// <summary>Returns every stat block defined in ```statblock fences in a Markdown note.</summary>
    public static IReadOnlyList<StatBlock> ParseMarkdown(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var statBlocks = new List<StatBlock>();
        foreach (Match match in StatblockFence().Matches(markdown))
        {
            if (ParseYaml(match.Groups["yaml"].Value) is { } statBlock)
            {
                statBlocks.Add(statBlock);
            }
        }

        return statBlocks;
    }

    /// <summary>Returns null when the YAML is malformed or does not define a named creature.</summary>
    public static StatBlock? ParseYaml(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        Dictionary<string, object?>? map;
        try
        {
            map = s_deserializer.Deserialize<Dictionary<string, object?>>(yaml);
        }
        catch (YamlException)
        {
            return null;
        }

        if (map is null)
        {
            return null;
        }

        string name = Text(map, "name");
        if (name.Length == 0)
        {
            return null;
        }

        string type = Text(map, "type");
        string subtype = Text(map, "subtype");
        int[] stats = Integers(map, "stats");
        AbilityScores abilities = stats.Length == 6
            ? new AbilityScores(stats[0], stats[1], stats[2], stats[3], stats[4], stats[5])
            : new AbilityScores(10, 10, 10, 10, 10, 10);
        string challengeRating = Text(map, "cr");

        return new StatBlock
        {
            Id = Guid.NewGuid(),
            Name = name,
            Source = Text(map, "source"),
            Size = Text(map, "size"),
            CreatureType = subtype.Length > 0 ? $"{type} ({subtype})" : type,
            Alignment = Text(map, "alignment"),
            ArmorClass = Integer(map, "ac") ?? 10,
            HitPoints = Integer(map, "hp") ?? 1,
            HitDice = Text(map, "hit_dice"),
            InitiativeModifier = Integer(map, "modifier") ?? AbilityScores.Modifier(abilities.Dexterity),
            Speed = Text(map, "speed"),
            Abilities = abilities,
            SavingThrows = Bonuses(map, "saves"),
            Skills = Bonuses(map, "skillsaves"),
            DamageVulnerabilities = Text(map, "damage_vulnerabilities"),
            DamageResistances = Text(map, "damage_resistances"),
            DamageImmunities = Text(map, "damage_immunities"),
            ConditionImmunities = Text(map, "condition_immunities"),
            Senses = Text(map, "senses"),
            Languages = Text(map, "languages"),
            ChallengeRating = challengeRating.Length > 0 ? challengeRating : "0",
            Traits = Features(map, "traits"),
            Actions = Features(map, "actions"),
            BonusActions = Features(map, "bonus_actions"),
            Reactions = Features(map, "reactions"),
            LegendaryActions = Features(map, "legendary_actions"),
        };
    }

    [GeneratedRegex(@"```statblock[^\n]*\r?\n(?<yaml>.*?)\r?\n```", RegexOptions.Singleline)]
    private static partial Regex StatblockFence();

    private static string Text(Dictionary<string, object?> map, string key)
    {
        return map.TryGetValue(key, out object? value) ? Flatten(value) : "";
    }

    /// <summary>Turns any YAML value into display text, whatever shape it arrived in.</summary>
    private static string Flatten(object? value)
    {
        return value switch
        {
            null => "",
            // Links to spells are kept, in a form the app can follow. Other links become plain text.
            string text => ConverterLinks.Simplify(text).Trim(),
            IDictionary<object, object> entries => string.Join(
                ", ",
                entries.Select(entry => (Flatten(entry.Key) + " " + Flatten(entry.Value)).Trim())),
            IEnumerable<object> items => string.Join(
                ", ",
                items.Select(Flatten).Where(text => text.Length > 0)),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "",
        };
    }

    /// <summary>Reads the number at the start of a value, so "33 (6d8 + 6)" gives 33.</summary>
    private static int? Integer(Dictionary<string, object?> map, string key)
    {
        string text = Text(map, key);

        int end = 0;
        while (end < text.Length && (char.IsAsciiDigit(text[end]) || (end == 0 && text[end] is '+' or '-')))
        {
            end++;
        }

        return int.TryParse(text.AsSpan(0, end), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number)
            ? number
            : null;
    }

    private static int[] Integers(Dictionary<string, object?> map, string key)
    {
        if (!map.TryGetValue(key, out object? value) || value is not IEnumerable<object> items)
        {
            return [];
        }

        return items
            .Select(item => int.TryParse(Flatten(item), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
                ? number
                : 10)
            .ToArray();
    }

    /// <summary>
    /// Saves and skills arrive as a list of small maps, either { dexterity: 4 }
    /// or { name: Stealth, desc: "+6" }. Both become "Dexterity +4, Stealth +6".
    /// </summary>
    private static string Bonuses(Dictionary<string, object?> map, string key)
    {
        if (!map.TryGetValue(key, out object? value) || value is not IEnumerable<object> items)
        {
            return Text(map, key);
        }

        var parts = new List<string>();
        foreach (object item in items)
        {
            if (item is not IDictionary<object, object> entry)
            {
                parts.Add(Flatten(item));
            }
            else if (entry.TryGetValue("name", out object? name))
            {
                string bonus = entry.TryGetValue("desc", out object? desc) ? Signed(Flatten(desc)) : "";
                parts.Add((Flatten(name) + " " + bonus).Trim());
            }
            else
            {
                foreach (KeyValuePair<object, object> pair in entry)
                {
                    parts.Add(Capitalize(Flatten(pair.Key)) + " " + Signed(Flatten(pair.Value)));
                }
            }
        }

        return string.Join(", ", parts.Where(part => part.Length > 0));
    }

    private static List<StatBlockFeature> Features(Dictionary<string, object?> map, string key)
    {
        var features = new List<StatBlockFeature>();
        if (!map.TryGetValue(key, out object? value) || value is not IEnumerable<object> items)
        {
            return features;
        }

        foreach (object item in items)
        {
            if (item is IDictionary<object, object> entry)
            {
                // The usual form: { name: ..., desc: ... }
                string name = entry.TryGetValue("name", out object? nameValue) ? Flatten(nameValue) : "";
                string description = entry.TryGetValue("desc", out object? descValue) ? Flatten(descValue) : "";
                if (name.Length > 0)
                {
                    features.Add(new StatBlockFeature(name, description));
                }
            }
            else if (item is IEnumerable<object> pair)
            {
                // The older form: [name, description]
                List<string> parts = pair.Select(Flatten).ToList();
                if (parts.Count > 0 && parts[0].Length > 0)
                {
                    features.Add(new StatBlockFeature(parts[0], string.Join(" ", parts.Skip(1))));
                }
            }
        }

        return features;
    }

    private static string Signed(string text)
    {
        return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number)
            ? number.ToString("+0;-0", CultureInfo.InvariantCulture)
            : text;
    }

    private static string Capitalize(string text)
    {
        return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
    }
}