using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Infrastructure.Homebrewery;

/// <summary>
/// Reads the monster stat blocks in a Homebrewery document. Three layouts are understood:
/// <list type="bullet">
/// <item>The legacy one, a block quote that starts with "> ## Name".</item>
/// <item>The V3 one, a {{monster,frame ... }} block with "- **Armor Class** 14" lines.</item>
/// <item>The V3 one in the 2024 style, with "**AC** :: 14" lines and the ability scores in
/// small Mod/Save tables.</item>
/// </list>
/// Everything outside the stat blocks (lore, tables, credits) is ignored.
/// </summary>
internal static partial class HomebreweryStatBlockParser
{
    private static readonly string[] s_sizes = ["Tiny", "Small", "Medium", "Large", "Huge", "Gargantuan"];

    private static readonly string[] s_damageTypes =
    [
        "acid", "bludgeoning", "cold", "fire", "force", "lightning", "necrotic",
        "piercing", "poison", "psychic", "radiant", "slashing", "thunder",
    ];

    private static readonly string[] s_abilityNames = ["Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma"];

    // The bold labels of the lines between the name and the traits, in every spelling the layouts use.
    private static readonly Dictionary<string, Stat> s_statLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Armor Class"] = Stat.ArmorClass,
        ["AC"] = Stat.ArmorClass,
        ["Hit Points"] = Stat.HitPoints,
        ["HP"] = Stat.HitPoints,
        ["Speed"] = Stat.Speed,
        ["Initiative"] = Stat.Initiative,
        ["Saving Throws"] = Stat.SavingThrows,
        ["Saves"] = Stat.SavingThrows,
        ["Skills"] = Stat.Skills,
        ["Damage Vulnerabilities"] = Stat.Vulnerabilities,
        ["Vulnerabilities"] = Stat.Vulnerabilities,
        ["Damage Resistances"] = Stat.Resistances,
        ["Resistances"] = Stat.Resistances,
        ["Damage Immunities"] = Stat.DamageImmunities,
        ["Condition Immunities"] = Stat.ConditionImmunities,
        ["Immunities"] = Stat.Immunities,
        ["Senses"] = Stat.Senses,
        ["Languages"] = Stat.Languages,
        ["Challenge"] = Stat.Challenge,
        ["CR"] = Stat.Challenge,
        ["Size"] = Stat.Size,
        ["Type"] = Stat.Type,
        ["Alignment"] = Stat.Alignment,
    };

    private enum Stat
    {
        ArmorClass,
        HitPoints,
        Speed,
        Initiative,
        SavingThrows,
        Skills,
        Vulnerabilities,
        Resistances,
        DamageImmunities,
        ConditionImmunities,
        Immunities,
        Senses,
        Languages,
        Challenge,
        Size,
        Type,
        Alignment,
    }

    private enum Section
    {
        None,
        Traits,
        Actions,
        BonusActions,
        Reactions,
        LegendaryActions,
    }

    /// <summary>Returns every stat block in the document, in the order they appear.</summary>
    /// <param name="markdown">The document's Markdown source.</param>
    /// <param name="source">What to file the stat blocks under.</param>
    /// <param name="links">Turns links into the conditions document into condition links.</param>
    public static IReadOnlyList<StatBlock> Parse(string markdown, string source, ConditionLinkRewriter links)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(links);

        var statBlocks = new List<StatBlock>();
        foreach (List<string> block in FindBlocks(HomebreweryText.Lines(markdown)))
        {
            if (new BlockReader(links).Read(block) is { } statBlock)
            {
                // Named like the 5eTools import names creatures from books other than the
                // Monster Manual, "Goblin (XMM)", so creatures from different sources with the
                // same name can be told apart in the library.
                string sourceName = source.Trim();
                statBlocks.Add(statBlock with { Name = $"{statBlock.Name} ({sourceName})", Source = sourceName });
            }
        }

        return statBlocks;
    }

    /// <summary>Cuts the document into the lines of each stat block, quote markers removed.</summary>
    private static List<List<string>> FindBlocks(IReadOnlyList<string> lines)
    {
        var blocks = new List<List<string>>();
        int index = 0;

        while (index < lines.Count)
        {
            if (MonsterBlockStart().IsMatch(lines[index]))
            {
                // The block ends where its braces balance. Blocks nest ({{stats inside {{monster),
                // and some lines open and close a span ({{bonus ...}}), so the braces are counted.
                var block = new List<string>();
                int depth = 0;
                while (index < lines.Count)
                {
                    string line = lines[index++];
                    block.Add(line);
                    depth += Occurrences(line, "{{") - Occurrences(line, "}}");
                    if (depth <= 0)
                    {
                        break;
                    }
                }

                blocks.Add(block);
            }
            else if (QuotedNameHeading().IsMatch(lines[index]))
            {
                // A legacy block is the run of quoted lines. A stray empty line inside it does
                // not end it, as long as the quote carries on with the same creature.
                var block = new List<string>();
                while (index < lines.Count)
                {
                    string line = lines[index];
                    if (line.TrimStart().StartsWith('>'))
                    {
                        block.Add(QuoteMarker().Replace(line, ""));
                    }
                    else if (block.Count > 0 && IsLazyContinuation(block[^1], line))
                    {
                        // Markdown lets a paragraph inside a quote carry on onto a line without
                        // the ">". It is the same sentence, so it joins the line before it.
                        block[^1] = block[^1].TrimEnd() + " " + line.Trim();
                    }
                    else if (line.Trim().Length > 0 || !QuoteContinues(lines, index))
                    {
                        break;
                    }

                    index++;
                }

                blocks.Add(block);
            }
            else
            {
                index++;
            }
        }

        return blocks;
    }

    /// <summary>
    /// Whether an unquoted line still belongs to the quoted paragraph above it. Only text does:
    /// a heading, rule, table, page break or block of the page's own layout ends the quote.
    /// </summary>
    private static bool IsLazyContinuation(string previous, string line)
    {
        string text = line.Trim();
        string before = previous.Trim();
        return text.Length > 0
            && before.Length > 0
            && !NotParagraphText().IsMatch(text)
            && !NotParagraphText().IsMatch(before);
    }

    private static bool QuoteContinues(IReadOnlyList<string> lines, int index)
    {
        for (int next = index + 1; next < lines.Count; next++)
        {
            if (lines[next].Trim().Length > 0)
            {
                return lines[next].TrimStart().StartsWith('>') && !QuotedNameHeading().IsMatch(lines[next]);
            }
        }

        return false;
    }

    private static int Occurrences(string text, string token)
    {
        int count = 0;
        int position = text.IndexOf(token, StringComparison.Ordinal);
        while (position >= 0)
        {
            count++;
            position = text.IndexOf(token, position + token.Length, StringComparison.Ordinal);
        }

        return count;
    }

    /// <summary>Reads the number at the start of a value, so "33 (6d8 + 6)" gives 33 and "-2 (8)" gives -2.</summary>
    private static int? LeadingNumber(string? text)
    {
        if (text is null || LeadingSignedNumber().Match(text) is not { Success: true } match)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["digits"].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
        {
            return null;
        }

        // Some documents write "--1" or a typographic minus; any minus sign makes it negative.
        return match.Groups["sign"].Value.AsSpan().IndexOfAny('-', '−', '–') >= 0 ? -number : number;
    }

    private static Section SectionFor(string title)
    {
        if (title.Contains("bonus", StringComparison.OrdinalIgnoreCase))
        {
            return Section.BonusActions;
        }

        if (title.Contains("reaction", StringComparison.OrdinalIgnoreCase))
        {
            return Section.Reactions;
        }

        // The app has one list for actions taken outside the creature's turn.
        if (title.Contains("legendary", StringComparison.OrdinalIgnoreCase)
            || title.Contains("mythic", StringComparison.OrdinalIgnoreCase)
            || title.Contains("villain", StringComparison.OrdinalIgnoreCase)
            || title.Contains("lair", StringComparison.OrdinalIgnoreCase))
        {
            return Section.LegendaryActions;
        }

        return title.Contains("action", StringComparison.OrdinalIgnoreCase) ? Section.Actions : Section.Traits;
    }

    private static int? AbilityIndex(string cell)
    {
        string name = cell.Trim();
        if (name.Length < 3)
        {
            return null;
        }

        for (int index = 0; index < s_abilityNames.Length; index++)
        {
            // "Str", "STR" and "Strength" all name the first one.
            if (s_abilityNames[index].StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return null;
    }

    private static bool IsSize(string word)
    {
        return s_sizes.Contains(word, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Collects one stat block's lines into a <see cref="StatBlock"/>.</summary>
    private sealed class BlockReader
    {
        private readonly ConditionLinkRewriter _links;
        private readonly Dictionary<Stat, string> _stats = new();
        private readonly Dictionary<Section, List<FeatureDraft>> _features = new();
        private readonly int?[] _scores = new int?[6];
        private readonly int?[] _modifiers = new int?[6];
        private readonly int?[] _saves = new int?[6];
        private string _name = "";
        private string _challengeRating = "";
        private string _size = "";
        private string _type = "";
        private string _alignment = "";
        private Section _section = Section.None;
        private FeatureDraft? _feature;
        private bool _paragraphEnded;

        public BlockReader(ConditionLinkRewriter links)
        {
            _links = links;
        }

        /// <returns>The stat block, or null if the lines do not describe a creature.</returns>
        public StatBlock? Read(List<string> lines)
        {
            foreach (string line in lines)
            {
                ReadLine(line.Trim());
            }

            // A block without either is a quote or a note that happens to have a "## " heading.
            if (_name.Length == 0 || !(_stats.ContainsKey(Stat.ArmorClass) || _stats.ContainsKey(Stat.HitPoints)))
            {
                return null;
            }

            return Build();
        }

        private void ReadLine(string line)
        {
            if (line.Length == 0 || HomebreweryText.IsLayout(line))
            {
                _paragraphEnded = true;
                return;
            }

            // The name comes first. The challenge rating can sit in a span on the same line,
            // "## Zombie Rotter {{bonus **cr 1/4**}}", so it is read before the spans go.
            if (_name.Length == 0 && NameHeading().Match(line) is { Success: true } heading)
            {
                string text = heading.Groups["text"].Value;
                if (BonusChallengeRating().Match(text) is { Success: true } rating)
                {
                    _challengeRating = rating.Groups["rating"].Value;
                }

                _name = HomebreweryText.CollapseSpaces(Display(HomebreweryText.WithoutInlineBlocks(text)).Replace("*", "", StringComparison.Ordinal));
                return;
            }

            line = HomebreweryText.WithoutInlineBlocks(line).Trim();

            // "{{stats" opens a block whose contents are read; the opening line itself says nothing.
            if (line.StartsWith("{{", StringComparison.Ordinal))
            {
                return;
            }

            line = line.Replace("}}", "", StringComparison.Ordinal).Trim();
            if (line.Length == 0)
            {
                _paragraphEnded = true;
                return;
            }

            if (SectionHeading().Match(line) is { Success: true } section)
            {
                _section = SectionFor(section.Groups["title"].Value);
                _feature = null;
                return;
            }

            if (line.StartsWith('#'))
            {
                return;
            }

            // A table inside a feature, such as a roll for a random effect, belongs to its text.
            if (line.StartsWith('|') && _feature is null)
            {
                ReadTableRow(line);
                return;
            }

            if (_feature is null && _section == Section.None && ItalicLine().Match(line) is { Success: true } italic)
            {
                ReadSizeTypeAndAlignment(italic.Groups["text"].Value);
                return;
            }

            if (StatLine().Match(line) is { Success: true } stat
                && _feature is null
                && s_statLabels.TryGetValue(stat.Groups["label"].Value.Trim(), out Stat field))
            {
                _stats[field] = stat.Groups["value"].Value.Trim();
                return;
            }

            if (FeatureStart().Match(line) is { Success: true } feature && IsFeature(feature))
            {
                StartFeature(feature.Groups["name"].Value, feature.Groups["rest"].Value);
                return;
            }

            ContinueFeature(line);
        }

        // "***Amphibious.*** text" and "**Amphibious.** text" name a feature. Under a section
        // heading such as "### Actions" any bold start does, even without the full stop.
        private bool IsFeature(Match feature)
        {
            return feature.Groups["name"].Value.TrimEnd().EndsWith('.')
                || feature.Groups["rest"].Value.TrimStart().StartsWith('.')
                || _section != Section.None;
        }

        private void StartFeature(string name, string text)
        {
            // Features before the first section heading are traits.
            if (_section == Section.None)
            {
                _section = Section.Traits;
            }

            if (!_features.TryGetValue(_section, out List<FeatureDraft>? list))
            {
                list = [];
                _features[_section] = list;
            }

            // The card adds the full stop after the name itself.
            name = name.Trim().TrimEnd('.').Trim();
            text = text.TrimStart().TrimStart('.').TrimStart();

            // "***Breath (1/Day.***)": the bracket closed after the bold. It belongs to the name.
            if (Occurrences(name, "(") > Occurrences(name, ")") && text.StartsWith(')'))
            {
                name += ")";
                text = text[1..].TrimStart().TrimStart('.');
            }

            _feature = new FeatureDraft(name);
            _feature.Text.Append(text.Trim());
            list.Add(_feature);
            _paragraphEnded = false;
        }

        private void ContinueFeature(string line)
        {
            // Text before the first feature of a section, such as "The dragon can take 3
            // legendary actions...", has nowhere to go in the app's stat block.
            if (_feature is null)
            {
                return;
            }

            StringBuilder text = _feature.Text;
            if (text.Length > 0)
            {
                bool startsOwnLine = line.StartsWith("- ", StringComparison.Ordinal)
                    || line.StartsWith("* ", StringComparison.Ordinal)
                    || line.StartsWith('|');
                text.Append(_paragraphEnded ? "\n\n" : startsOwnLine ? "\n" : " ");
            }

            text.Append(line);
            _paragraphEnded = false;
        }

        private void ReadTableRow(string line)
        {
            string[] cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();

            // The 2024 layout: "|Str| 13 | +1 | +1 |", one ability a row, with its modifier and save.
            if (cells.Length >= 2 && AbilityIndex(cells[0]) is { } ability && LeadingNumber(cells[1]) is { } score)
            {
                _scores[ability] = score;
                _modifiers[ability] = cells.Length > 2 ? LeadingNumber(cells[2]) : null;
                _saves[ability] = cells.Length > 3 ? LeadingNumber(cells[3]) : null;
                return;
            }

            // The older layout: a heading row of STR to CHA, then one row of "10 (+0)" cells.
            if (cells.Length == 6 && cells.All(cell => LeadingNumber(cell) is not null))
            {
                for (int index = 0; index < 6; index++)
                {
                    _scores[index] = LeadingNumber(cells[index]);
                }
            }
        }

        // "*Medium humanoid (any race), any alignment*". The 2024 layout puts a sentence of
        // description in italics here instead, which does not start with a size and is skipped.
        private void ReadSizeTypeAndAlignment(string text)
        {
            string[] words = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2 || !IsSize(words[0]))
            {
                return;
            }

            // "Medium or Small humanoid": the size is the first three words.
            int sizeWords = words.Length > 3
                && string.Equals(words[1], "or", StringComparison.OrdinalIgnoreCase)
                && IsSize(words[2]) ? 3 : 1;

            string rest = string.Join(' ', words.Skip(sizeWords));
            int comma = FirstCommaOutsideBrackets(rest);

            _size = string.Join(' ', words.Take(sizeWords));
            _type = Display(comma < 0 ? rest : rest[..comma]).Trim();
            _alignment = comma < 0 ? "" : Display(rest[(comma + 1)..]).Trim();
        }

        private static int FirstCommaOutsideBrackets(string text)
        {
            int depth = 0;
            for (int index = 0; index < text.Length; index++)
            {
                switch (text[index])
                {
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth--;
                        break;
                    case ',' when depth <= 0:
                        return index;
                }
            }

            return -1;
        }

        private StatBlock Build()
        {
            var abilities = new AbilityScores(
                _scores[0] ?? 10,
                _scores[1] ?? 10,
                _scores[2] ?? 10,
                _scores[3] ?? 10,
                _scores[4] ?? 10,
                _scores[5] ?? 10);

            (string damageImmunities, string conditionImmunities) = Immunities();

            return new StatBlock
            {
                Id = Guid.NewGuid(),
                Name = _name,
                Size = StatText(Stat.Size) is { Length: > 0 } size ? WithoutBrackets(size) : _size,
                CreatureType = StatText(Stat.Type) is { Length: > 0 } type ? type : _type,
                Alignment = StatText(Stat.Alignment) is { Length: > 0 } alignment ? alignment : _alignment,
                ArmorClass = LeadingNumber(StatValue(Stat.ArmorClass)) ?? 10,
                HitPoints = LeadingNumber(StatValue(Stat.HitPoints)) ?? 1,
                HitDice = HitDice(StatValue(Stat.HitPoints)),
                InitiativeModifier = LeadingNumber(StatValue(Stat.Initiative)) ?? AbilityScores.Modifier(abilities.Dexterity),
                Speed = StatText(Stat.Speed),
                Abilities = abilities,
                SavingThrows = StatText(Stat.SavingThrows) is { Length: > 0 } saves ? saves : SavesFromTable(),
                Skills = StatText(Stat.Skills),
                DamageVulnerabilities = StatText(Stat.Vulnerabilities),
                DamageResistances = StatText(Stat.Resistances),
                DamageImmunities = damageImmunities,
                ConditionImmunities = _links.LinkNames(conditionImmunities),
                Senses = StatText(Stat.Senses),
                Languages = StatText(Stat.Languages),
                ChallengeRating = ChallengeRating(),
                Origin = StatBlockOrigin.Imported,
                Traits = Features(Section.Traits),
                Actions = Features(Section.Actions),
                BonusActions = Features(Section.BonusActions),
                Reactions = Features(Section.Reactions),
                LegendaryActions = Features(Section.LegendaryActions),
            };
        }

        private string? StatValue(Stat stat)
        {
            return _stats.GetValueOrDefault(stat);
        }

        private string StatText(Stat stat)
        {
            return _stats.TryGetValue(stat, out string? value) ? Display(value).Trim() : "";
        }

        private string ChallengeRating()
        {
            string rating = _challengeRating.Length > 0
                ? _challengeRating
                : StatText(Stat.Challenge).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

            // Minions from Flee, Mortals! are rated "M4": kept as written, it simply has no XP.
            return rating.Length > 0 ? rating : "0";
        }

        // The 2014 layout lists damage and condition immunities separately. The 2024 one has
        // one line, "Poison; Exhaustion, Poisoned", with damage types before the semicolon.
        private (string Damage, string Conditions) Immunities()
        {
            string damage = StatText(Stat.DamageImmunities);
            string conditions = StatText(Stat.ConditionImmunities);
            string combined = StatText(Stat.Immunities);

            if (combined.Length > 0)
            {
                int semicolon = combined.IndexOf(';', StringComparison.Ordinal);
                if (semicolon >= 0)
                {
                    damage = Join(damage, combined[..semicolon].Trim());
                    conditions = Join(conditions, combined[(semicolon + 1)..].Trim());
                }
                else if (s_damageTypes.Any(type => combined.StartsWith(type, StringComparison.OrdinalIgnoreCase)))
                {
                    damage = Join(damage, combined);
                }
                else
                {
                    conditions = Join(conditions, combined);
                }
            }

            return (damage, conditions);
        }

        private static string Join(string first, string second)
        {
            return first.Length == 0 ? second : second.Length == 0 ? first : $"{first}, {second}";
        }

        // In the 2024 layout a proficient save is one that differs from the plain modifier.
        private string SavesFromTable()
        {
            var saves = new List<string>();
            for (int index = 0; index < 6; index++)
            {
                if (_saves[index] is { } save && _modifiers[index] is { } modifier && save != modifier)
                {
                    saves.Add(s_abilityNames[index] + " " + save.ToString("+0;-0", CultureInfo.InvariantCulture));
                }
            }

            return string.Join(", ", saves);
        }

        // "135 (18d10 + 36)" gives "18d10 + 36".
        private static string HitDice(string? hitPoints)
        {
            return hitPoints is not null && HitDiceInBrackets().Match(hitPoints) is { Success: true } match
                ? match.Groups["dice"].Value.Trim()
                : "";
        }

        // "Medium (6 ft. tall)" gives "Medium".
        private static string WithoutBrackets(string text)
        {
            int bracket = text.IndexOf('(', StringComparison.Ordinal);
            return (bracket < 0 ? text : text[..bracket]).Trim();
        }

        private List<StatBlockFeature> Features(Section section)
        {
            if (!_features.TryGetValue(section, out List<FeatureDraft>? drafts))
            {
                return [];
            }

            return drafts
                .Where(draft => draft.Name.Length > 0)
                .Select(draft => new StatBlockFeature(Display(draft.Name).Trim(), _links.LinkNames(Display(draft.Text.ToString()).Trim())))
                .ToList();
        }

        private string Display(string text)
        {
            return HomebreweryText.Display(text, _links);
        }
    }

    private sealed class FeatureDraft
    {
        public FeatureDraft(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public StringBuilder Text { get; } = new();
    }

    [GeneratedRegex(@"^\s*\{\{\s*monster\b", RegexOptions.IgnoreCase)]
    private static partial Regex MonsterBlockStart();

    // "> ## Name", the first line of a legacy stat block. Exactly two # signs.
    [GeneratedRegex(@"^\s*>\s*##(?!#)\s*\S")]
    private static partial Regex QuotedNameHeading();

    [GeneratedRegex(@"^\s*>[ \t]?")]
    private static partial Regex QuoteMarker();

    // Lines that start something other than running text: headings, list items, tables,
    // rules, page and column breaks, {{ }} blocks and HTML.
    [GeneratedRegex(@"^(?:#|[-*+]\s|\||_{3,}|-{3,}|\\|\{\{|\}\}|<|:+$)")]
    private static partial Regex NotParagraphText();

    [GeneratedRegex(@"^##(?!#)\s*(?<text>.+)$")]
    private static partial Regex NameHeading();

    [GeneratedRegex(@"^###(?!#)\s*(?<title>.+?)\s*$")]
    private static partial Regex SectionHeading();

    // "{{bonus **cr 1/4**}}" or "{{bonus **CR M6**}}".
    [GeneratedRegex(@"\{\{\s*bonus[^}]*?\bcr\s+(?<rating>[^\s*}]+)", RegexOptions.IgnoreCase)]
    private static partial Regex BonusChallengeRating();

    // "*Tiny aberration, lawful evil*" or "_Tiny aberration, lawful evil_", but not "**bold**".
    [GeneratedRegex(@"^(?:\*(?!\*)(?<text>[^*]+)\*|_(?!_)(?<text>[^_]+)_)$")]
    private static partial Regex ItalicLine();

    // "- **Armor Class** 17 (natural armor)" or "**AC** :: 17".
    [GeneratedRegex(@"^(?:[-*]\s+)?\*\*(?<label>[^*]+)\*\*\s*(?:::\s*)?(?<value>.*)$")]
    private static partial Regex StatLine();

    // "***Name.*** text" or "**Name.** text".
    [GeneratedRegex(@"^\*{2,3}(?<name>[^*]+?)\*{2,3}(?<rest>.*)$")]
    private static partial Regex FeatureStart();

    [GeneratedRegex(@"^\s*(?<sign>[+\-−–]*)\s*(?<digits>\d+)")]
    private static partial Regex LeadingSignedNumber();

    [GeneratedRegex(@"\((?<dice>[^)]*\d+d\d+[^)]*)\)")]
    private static partial Regex HitDiceInBrackets();
}
