using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.App.Services;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.ViewModels;

/// <summary>Edits a spell the user is writing, or an imported one they are changing.</summary>
public sealed partial class SpellEditorViewModel : LibraryEditorViewModel
{
    private readonly ICardFactory _cardFactory;
    private readonly SavedState<Spell> _savedState;

    [ObservableProperty]
    private int _level = 1;

    [ObservableProperty]
    private string _school = "";

    [ObservableProperty]
    private string _castingTime = "";

    [ObservableProperty]
    private string _range = "";

    [ObservableProperty]
    private string _duration = "";

    [ObservableProperty]
    private bool _hasVerbal;

    [ObservableProperty]
    private bool _hasSomatic;

    [ObservableProperty]
    private bool _hasMaterial;

    /// <summary>What the material component is, in the brackets after the M. Only used when M is ticked.</summary>
    [ObservableProperty]
    private string _materialText = "";

    [ObservableProperty]
    private bool _isConcentration;

    [ObservableProperty]
    private bool _isRitual;

    public SpellEditorViewModel(
        ICardFactory cardFactory,
        Spell spell,
        IEnumerable<string> knownClasses,
        string? originalKey)
        : base(knownClasses, spell?.Classes ?? [], originalKey)
    {
        ArgumentNullException.ThrowIfNull(cardFactory);
        ArgumentNullException.ThrowIfNull(spell);

        _cardFactory = cardFactory;

        Name = spell.Name;
        Source = spell.Source;
        Description = spell.Description;
        Level = Math.Clamp(spell.LevelNumber, 0, 9);
        School = spell.School;
        CastingTime = spell.CastingTime;
        Range = spell.Range;
        Duration = spell.Duration;
        IsConcentration = spell.IsConcentration;
        IsRitual = spell.IsRitual;
        (HasVerbal, HasSomatic, HasMaterial, MaterialText) = ParseComponents(spell.Components);

        _savedState = new SavedState<Spell>(ToSpell);
        Ready();
    }

    /// <summary>The choices in the level box: cantrips, then 1 to 9.</summary>
    public IReadOnlyList<FilterOption<int>> LevelOptions { get; } =
    [
        new(0, "Cantrip"),
        .. Enumerable.Range(1, 9).Select(level => new FilterOption<int>(level, string.Create(CultureInfo.InvariantCulture, $"Level {level}"))),
    ];

    public IReadOnlyList<string> Schools { get; } =
    [
        "Abjuration", "Conjuration", "Divination", "Enchantment", "Evocation", "Illusion", "Necromancy", "Transmutation",
    ];

    public override string Heading => IsNew ? "New spell" : "Edit spell";

    public override string Noun => "spell";

    public override bool HasUnsavedChanges => _savedState.HasChanges;

    /// <summary>A spell with the defaults a new one starts from.</summary>
    public static SpellEditorViewModel CreateNew(ICardFactory cardFactory, IEnumerable<string> knownClasses)
    {
        var spell = new Spell
        {
            Key = "",
            Name = "",
            LevelNumber = 1,
            CastingTime = "1 action",
            Range = "Self",
            Duration = "Instantaneous",
            Components = "V, S",
            Source = DefaultSource,
        };

        return new SpellEditorViewModel(cardFactory, spell, knownClasses, originalKey: null);
    }

    public override void MarkSaved()
    {
        _savedState.MarkSaved();
    }

    public Spell ToSpell()
    {
        return new Spell
        {
            Key = Key,
            Name = Name.Trim(),
            Level = LevelText(Level),
            LevelNumber = Level,
            School = School.Trim(),
            IsRitual = IsRitual,
            CastingTime = CastingTime.Trim(),
            Range = Range.Trim(),
            Components = FormatComponents(HasVerbal, HasSomatic, HasMaterial, MaterialText.Trim()),
            Duration = Duration.Trim(),
            IsConcentration = IsConcentration,
            Classes = SelectedClasses(),
            Source = Source.Trim(),
            Description = Description.Trim(),
            IsCustom = true,
        };
    }

    /// <summary>"cantrip", "1st-level", "2nd-level", as the importer words them.</summary>
    public static string LevelText(int level)
    {
        return level switch
        {
            <= 0 => "cantrip",
            1 => "1st-level",
            2 => "2nd-level",
            3 => "3rd-level",
            _ => string.Create(CultureInfo.InvariantCulture, $"{level}th-level"),
        };
    }

    /// <summary>Reads "V, S, M (a pinch of salt)" into its ticks and the material text.</summary>
    public static (bool Verbal, bool Somatic, bool Material, string MaterialText) ParseComponents(string components)
    {
        ArgumentNullException.ThrowIfNull(components);

        int open = components.IndexOf('(', StringComparison.Ordinal);
        int close = components.LastIndexOf(')');
        string letters = open >= 0 ? components[..open] : components;
        string material = open >= 0 && close > open ? components[(open + 1)..close].Trim() : "";

        string[] parts = letters.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return (
            parts.Contains("V", StringComparer.OrdinalIgnoreCase),
            parts.Contains("S", StringComparer.OrdinalIgnoreCase),
            parts.Contains("M", StringComparer.OrdinalIgnoreCase),
            material);
    }

    /// <summary>The reverse of <see cref="ParseComponents"/>: "V, S, M (a pinch of salt)".</summary>
    public static string FormatComponents(bool verbal, bool somatic, bool material, string materialText)
    {
        ArgumentNullException.ThrowIfNull(materialText);

        var parts = new List<string>(3);
        if (verbal)
        {
            parts.Add("V");
        }

        if (somatic)
        {
            parts.Add("S");
        }

        if (material)
        {
            parts.Add(materialText.Length > 0 ? $"M ({materialText})" : "M");
        }

        return string.Join(", ", parts);
    }

    protected override IReadOnlyList<CardViewModel> BuildCards()
    {
        return _cardFactory.ForSpell(ToSpell());
    }
}
