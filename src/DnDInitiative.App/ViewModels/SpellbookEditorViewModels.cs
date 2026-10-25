using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.Core.Abilities;
using DnDInitiative.Core.Spellbooks;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.ViewModels;

/// <summary>The facts a spell or ability is sorted by, in the library and in a spellbook.</summary>
public interface ISortableEntry
{
    string Name { get; }

    /// <summary>The spell level (0 for a cantrip) or the ability's degree.</summary>
    int Level { get; }

    IReadOnlyList<string> Classes { get; }

    /// <summary>As the library words it, such as "1 bonus action". Empty for an ability.</summary>
    string CastingTime { get; }

    /// <summary>As the library words it, such as "Self (15-foot cone)". Empty for an ability.</summary>
    string Range { get; }

    bool IsRitual { get; }

    bool IsConcentration { get; }
}

/// <summary>What the library and the pages can be sorted by: one for each column of the library.</summary>
public enum LibrarySortOrder
{
    /// <summary>The level column: by level, then by name.</summary>
    LevelThenName,
    Name,
    CastingTime,
    Range,

    /// <summary>Rituals first.</summary>
    Ritual,

    /// <summary>Concentration spells first.</summary>
    Concentration,
}

/// <summary>
/// The heading of one of the library's columns. Clicking it sorts by that column, and clicking
/// it again reverses the order; the arrow shows which column the list is sorted by.
/// </summary>
public sealed partial class LibraryColumnHeader : ObservableObject
{
    private readonly Action<LibrarySortOrder> _sort;

    /// <summary>"▲" or "▼" when the list is sorted by this column, otherwise empty.</summary>
    [ObservableProperty]
    private string _arrow = "";

    public LibraryColumnHeader(LibrarySortOrder order, string label, string toolTip, Action<LibrarySortOrder> sort)
    {
        Order = order;
        Label = label;
        ToolTip = toolTip;
        _sort = sort;
    }

    public LibrarySortOrder Order { get; }

    public string Label { get; }

    public string ToolTip { get; }

    [RelayCommand]
    private void Sort()
    {
        _sort(Order);
    }
}

public enum LibraryKind
{
    Both,
    Spells,
    Abilities,
}

/// <summary>One choice in the library's Spells / Abilities / Both box.</summary>
public sealed record LibraryKindOption(LibraryKind Kind, string Label);

/// <summary>One choice in a filter box. A null value means "any".</summary>
public sealed record FilterOption<T>(T Value, string Label);

/// <summary>One choice in the pages' zoom box.</summary>
public sealed record ZoomOption(double Scale, string Label);

public static partial class SpellbookSorting
{
    /// <summary>
    /// Puts spells and abilities in the chosen order, or the reverse of it. Ties are always broken
    /// by level and then by name, from the lowest. Those without a casting time or range, such as
    /// abilities, go last when sorted by it, in either direction.
    /// </summary>
    public static IEnumerable<T> Sort<T>(IEnumerable<T> entries, LibrarySortOrder order, bool descending = false)
        where T : ISortableEntry
    {
        // A sort on nothing, so every order below can start with ThenBy.
        IOrderedEnumerable<T> sorted = entries.OrderBy(_ => 0);

        sorted = order switch
        {
            LibrarySortOrder.Name => Then(sorted, entry => entry.Name, descending, StringComparer.OrdinalIgnoreCase),
            LibrarySortOrder.CastingTime => Then(
                sorted.ThenBy(entry => entry.CastingTime.Length == 0),
                entry => CastingTimeRank(entry.CastingTime),
                descending)
                .ThenBy(entry => entry.CastingTime, StringComparer.OrdinalIgnoreCase),
            LibrarySortOrder.Range => Then(
                sorted.ThenBy(entry => entry.Range.Length == 0),
                entry => RangeRank(entry.Range),
                descending)
                .ThenBy(entry => entry.Range, StringComparer.OrdinalIgnoreCase),
            LibrarySortOrder.Ritual => Then(sorted, entry => !entry.IsRitual, descending),
            LibrarySortOrder.Concentration => Then(sorted, entry => !entry.IsConcentration, descending),
            _ => Then(sorted, entry => entry.Level, descending),
        };

        return sorted
            .ThenBy(entry => entry.Level)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A casting time as a number of seconds, so "1 action" comes before "1 minute" and "10 minutes"
    /// before "1 hour". A bonus action and a reaction come just after an action.
    /// </summary>
    public static double CastingTimeRank(string castingTime)
    {
        ArgumentNullException.ThrowIfNull(castingTime);

        Match match = AmountAndUnit().Match(castingTime);
        if (!match.Success)
        {
            return double.MaxValue;
        }

        double amount = double.Parse(match.Groups["amount"].Value, CultureInfo.InvariantCulture);
        double seconds = match.Groups["unit"].Value.ToUpperInvariant() switch
        {
            "ACTION" => 6,
            "BONUS ACTION" => 6.1,
            "REACTION" => 6.2,
            "ROUND" => 6.3,
            "MINUTE" => 60,
            "HOUR" => 3600,
            "DAY" => 86400,
            _ => double.MaxValue,
        };
        return amount * seconds;
    }

    /// <summary>
    /// A range in feet, so "Self" and "Touch" come before "5 feet", and "1 mile" after "500 feet".
    /// A range in words, such as "Sight" or "Unlimited", comes after every distance.
    /// </summary>
    public static double RangeRank(string range)
    {
        ArgumentNullException.ThrowIfNull(range);

        string text = range.Trim();
        if (text.StartsWith("Self", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (text.StartsWith("Touch", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        Match match = Distance().Match(text);
        if (match.Success)
        {
            double amount = double.Parse(match.Groups["amount"].Value, CultureInfo.InvariantCulture);
            return 2 + (match.Groups["unit"].Value.StartsWith("mi", StringComparison.OrdinalIgnoreCase) ? amount * 5280 : amount);
        }

        if (text.StartsWith("Sight", StringComparison.OrdinalIgnoreCase))
        {
            return 1e9;
        }

        return text.StartsWith("Unlimited", StringComparison.OrdinalIgnoreCase) ? 1e10 : double.MaxValue;
    }

    private static IOrderedEnumerable<T> Then<T, TKey>(
        IOrderedEnumerable<T> sorted,
        Func<T, TKey> key,
        bool descending,
        IComparer<TKey>? comparer = null)
    {
        return descending ? sorted.ThenByDescending(key, comparer) : sorted.ThenBy(key, comparer);
    }

    // "1 action", "1 bonus action", "10 minutes", "24 hours"
    [GeneratedRegex(@"^\s*(?<amount>\d+)\s+(?<unit>action|bonus action|reaction|round|minute|hour|day)s?\b", RegexOptions.IgnoreCase)]
    private static partial Regex AmountAndUnit();

    // "30 feet", "1 mile", "500 ft."
    [GeneratedRegex(@"^(?<amount>\d+)\s*-?\s*(?<unit>feet|foot|ft|miles?|mi)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Distance();
}

/// <summary>One row in the Spellbook page's library: a spell or an ability that can be added.</summary>
public sealed partial class LibraryEntryViewModel : ObservableObject, ISortableEntry
{
    /// <summary>Whether the open spellbook already has it, so the list can mark it.</summary>
    [ObservableProperty]
    private bool _isInSpellbook;

    private LibraryEntryViewModel(SpellbookEntry entry, string name, int level, IReadOnlyList<string> classes, string source)
    {
        Entry = entry;
        Name = name;
        Level = level;
        Classes = classes;
        Source = source;
    }

    public SpellbookEntry Entry { get; }

    public string Name { get; }

    public int Level { get; }

    public IReadOnlyList<string> Classes { get; }

    public string CastingTime { get; private init; } = "";

    public string Range { get; private init; } = "";

    public bool IsRitual { get; private init; }

    public bool IsConcentration { get; private init; }

    /// <summary>The level column: "Cantrip", "3" for a spell's level, "D2" for an exploit's degree.</summary>
    public string LevelLabel { get; private init; } = "";

    /// <summary>The casting time column, shortened: "Action", "Bonus", "Reaction", "10 min".</summary>
    public string ShortCastingTime => LibraryText.ShortCastingTime(CastingTime);

    /// <summary>The range column, shortened: "Self (15 ft cone)", "120 ft", "1 mi".</summary>
    public string ShortRange => LibraryText.ShortRange(Range);

    /// <summary>Every class that can learn it, in alphabetical order, for the tooltip.</summary>
    public string ClassList => string.Join(", ", Classes);

    /// <summary>"R" in the ritual column, or nothing.</summary>
    public string RitualMark => IsRitual ? "R" : "";

    /// <summary>"C" in the concentration column, or nothing.</summary>
    public string ConcentrationMark => IsConcentration ? "C" : "";

    /// <summary>The longer description in the tooltip, such as "Level 3 · Evocation · concentration".</summary>
    public string Detail { get; private init; } = "";

    public string Source { get; }

    /// <summary>True for an entry the user wrote or changed, which is kept in the campaign folder.</summary>
    public bool IsCustom { get; private init; }

    /// <summary>The row's tooltip: the detail, the casting time and range in full, the classes, then the book.</summary>
    public string ToolTip => string.Join("\n", new[]
    {
        Detail,
        CastingTime.Length > 0 ? $"Casting time: {CastingTime}" : "",
        Range.Length > 0 ? $"Range: {Range}" : "",
        ClassList,
        Source,
    }.Where(line => line.Length > 0));

    public static LibraryEntryViewModel ForSpell(SpellSummary spell)
    {
        ArgumentNullException.ThrowIfNull(spell);

        string level = spell.LevelNumber == 0
            ? "Cantrip"
            : string.Create(CultureInfo.InvariantCulture, $"Level {spell.LevelNumber}");
        string detail = string.Join(" · ", new[]
        {
            level,
            spell.School,
            spell.IsRitual ? "ritual" : "",
            spell.IsConcentration ? "concentration" : "",
            spell.IsCustom ? "custom" : "",
        }.Where(part => part.Length > 0));

        return new LibraryEntryViewModel(
            new SpellbookEntry(SpellbookEntryKind.Spell, spell.Key),
            spell.Name,
            spell.LevelNumber,
            spell.Classes,
            spell.Source)
        {
            CastingTime = spell.CastingTime.Trim(),
            Range = spell.Range.Trim(),
            IsRitual = spell.IsRitual,
            IsConcentration = spell.IsConcentration,
            IsCustom = spell.IsCustom,
            LevelLabel = spell.LevelNumber == 0 ? "Cantrip" : spell.LevelNumber.ToString(CultureInfo.InvariantCulture),
            Detail = detail,
        };
    }

    public static LibraryEntryViewModel ForAbility(AbilitySummary ability)
    {
        ArgumentNullException.ThrowIfNull(ability);

        string detail = ability.Degree > 0
            ? string.Create(CultureInfo.InvariantCulture, $"Degree {ability.Degree} · {ability.Category}")
            : ability.Category;
        if (ability.IsCustom)
        {
            detail += " · custom";
        }

        return new LibraryEntryViewModel(
            new SpellbookEntry(SpellbookEntryKind.Ability, ability.Key),
            ability.Name,
            ability.Degree,
            ability.Classes,
            ability.Source)
        {
            IsCustom = ability.IsCustom,
            LevelLabel = ability.Degree > 0 ? string.Create(CultureInfo.InvariantCulture, $"D{ability.Degree}") : "",
            Detail = detail,
        };
    }
}

/// <summary>Shortens a spell's casting time and range to fit the library's narrow columns.</summary>
public static partial class LibraryText
{
    public static string ShortCastingTime(string castingTime)
    {
        ArgumentNullException.ThrowIfNull(castingTime);

        string text = castingTime.Trim();

        // "1 reaction, which you take when..." keeps only the part before the comma; the tooltip has the rest.
        int comma = text.IndexOf(',', StringComparison.Ordinal);
        if (comma > 0)
        {
            text = text[..comma];
        }

        return text.ToUpperInvariant() switch
        {
            "1 ACTION" => "Action",
            "1 BONUS ACTION" => "Bonus",
            "1 REACTION" => "Reaction",
            _ => Units().Replace(text, match => match.Value.ToUpperInvariant() switch
            {
                "MINUTE" or "MINUTES" => "min",
                "HOUR" or "HOURS" => "hr",
                _ => match.Value,
            }),
        };
    }

    public static string ShortRange(string range)
    {
        ArgumentNullException.ThrowIfNull(range);

        string text = Feet().Replace(range.Trim(), " ft");
        return Miles().Replace(text, " mi");
    }

    [GeneratedRegex(@"\b(minutes?|hours?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Units();

    // "30 feet", "15-foot"
    [GeneratedRegex(@"\s*-?\s*\b(feet|foot)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Feet();

    [GeneratedRegex(@"\s*\b(miles?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Miles();
}

/// <summary>
/// One spell or ability in the open spellbook. What it is called and how it sorts are only
/// known once it was looked up in the library; until then its key stands in for the name.
/// </summary>
public sealed class SpellbookEntryViewModel : ISortableEntry
{
    public SpellbookEntryViewModel(SpellbookEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Entry = entry;
        Name = entry.Key;
    }

    public SpellbookEntry Entry { get; }

    public string Name { get; private set; }

    public int Level { get; private set; }

    public IReadOnlyList<string> Classes { get; private set; } = [];

    public string CastingTime { get; private set; } = "";

    public string Range { get; private set; } = "";

    public bool IsRitual { get; private set; }

    public bool IsConcentration { get; private set; }

    /// <summary>One card, or more when the description continues. Empty until looked up.</summary>
    public IReadOnlyList<CardViewModel> Cards { get; private set; } = [];

    /// <summary>Fills in what the library says about the entry, once it has been looked up.</summary>
    public void Apply(ISortableEntry facts, IReadOnlyList<CardViewModel> cards)
    {
        ArgumentNullException.ThrowIfNull(facts);

        Name = facts.Name;
        Level = facts.Level;
        Classes = facts.Classes;
        CastingTime = facts.CastingTime;
        Range = facts.Range;
        IsRitual = facts.IsRitual;
        IsConcentration = facts.IsConcentration;
        Cards = cards;
    }
}

/// <summary>One card on a printed page, with the spellbook entry it belongs to.</summary>
public sealed partial class SpellbookCardViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public SpellbookCardViewModel(SpellbookEntryViewModel entry, CardViewModel card)
    {
        Entry = entry;
        Card = card;
    }

    /// <summary>What removing or dragging this card acts on. The cards an entry continues onto share it.</summary>
    public SpellbookEntryViewModel Entry { get; }

    public CardViewModel Card { get; }
}

/// <summary>One A4 page of cards, as previewed and as printed.</summary>
public sealed class SpellbookPageViewModel
{
    public SpellbookPageViewModel(int number, IReadOnlyList<SpellbookCardViewModel> cards)
    {
        Number = number;
        Cards = cards;
    }

    /// <summary>Counted from 1.</summary>
    public int Number { get; }

    /// <summary>Up to a page's worth, in reading order. A short last page leaves the rest empty.</summary>
    public IReadOnlyList<SpellbookCardViewModel> Cards { get; }
}

public sealed partial class SpellbookEditorViewModel : ObservableValidator
{
    private readonly SavedState<Spellbook> _savedState;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "A spellbook needs a name.")]
    private string _name = "";

    public SpellbookEditorViewModel(Guid id)
    {
        Id = id;
        _savedState = new SavedState<Spellbook>(ToSpellbook);
    }

    public Guid Id { get; }

    /// <summary>When the spellbook was last saved, or created if it never was. Not something the user edits.</summary>
    public DateTimeOffset? Modified { get; set; }

    /// <summary>In the order the cards are printed.</summary>
    public ObservableCollection<SpellbookEntryViewModel> Entries { get; } = [];

    public bool HasUnsavedChanges => _savedState.HasChanges;

    public bool Contains(SpellbookEntry entry)
    {
        return Entries.Any(row => row.Entry == entry);
    }

    public void MarkSaved()
    {
        _savedState.MarkSaved();
    }

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    public Spellbook ToSpellbook()
    {
        return new Spellbook(Id, Name.Trim(), Entries.Select(row => row.Entry).ToList(), Modified);
    }

    public static SpellbookEditorViewModel FromSpellbook(Spellbook spellbook)
    {
        ArgumentNullException.ThrowIfNull(spellbook);

        var editor = new SpellbookEditorViewModel(spellbook.Id) { Name = spellbook.Name, Modified = spellbook.Modified };
        foreach (SpellbookEntry entry in spellbook.Entries)
        {
            editor.Entries.Add(new SpellbookEntryViewModel(entry));
        }

        editor.MarkSaved();
        return editor;
    }
}