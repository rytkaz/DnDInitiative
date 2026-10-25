using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.Core.Library;

namespace DnDInitiative.App.ViewModels;

/// <summary>One class in the editor's tick list.</summary>
public sealed partial class ClassChoiceViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public ClassChoiceViewModel(string name, bool isSelected)
    {
        Name = name;
        _isSelected = isSelected;
    }

    public string Name { get; }
}

/// <summary>
/// What the spell and ability editors share: a name and source that make up the key, the
/// classes, the Markdown description, and the card that is redrawn as any of them change.
/// </summary>
public abstract partial class LibraryEditorViewModel : ObservableValidator
{
    /// <summary>What a new entry's source is until the user changes it.</summary>
    public const string DefaultSource = "Homebrew";

    private static readonly HashSet<string> NotPreviewTriggers =
    [
        nameof(PreviewCards), nameof(CanSave), nameof(HasErrors), nameof(Note), nameof(OriginalKey), nameof(IsNew),
    ];

    private bool _ready;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [Required(ErrorMessage = "A name is required.")]
    private string _name = "";

    [ObservableProperty]
    private string _source = DefaultSource;

    [ObservableProperty]
    private string _description = "";

    /// <summary>Classes that are not in the tick list, separated by commas.</summary>
    [ObservableProperty]
    private string _otherClasses = "";

    /// <summary>The card as it would be printed, redrawn after every change, so a long text is seen spilling onto a second card.</summary>
    [ObservableProperty]
    private IReadOnlyList<CardViewModel> _previewCards = [];

    /// <summary>A line under the heading about what saving will do, or empty.</summary>
    [ObservableProperty]
    private string _note = "";

    /// <summary>The key of the entry being edited, or null for one that has never been saved.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    private string? _originalKey;

    protected LibraryEditorViewModel(IEnumerable<string> knownClasses, IEnumerable<string> classes, string? originalKey)
    {
        ArgumentNullException.ThrowIfNull(knownClasses);
        ArgumentNullException.ThrowIfNull(classes);

        _originalKey = originalKey;

        string[] chosen = [.. classes];
        IEnumerable<string> listed = knownClasses
            .Concat(chosen)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);
        foreach (string name in listed)
        {
            var choice = new ClassChoiceViewModel(name, chosen.Contains(name, StringComparer.OrdinalIgnoreCase));
            choice.PropertyChanged += (_, _) => RefreshPreview();
            ClassChoices.Add(choice);
        }
    }

    public abstract string Heading { get; }

    public abstract bool HasUnsavedChanges { get; }

    /// <summary>The kind of entry, for the messages about it: "spell" or "ability".</summary>
    public abstract string Noun { get; }

    public ObservableCollection<ClassChoiceViewModel> ClassChoices { get; } = [];

    public bool IsNew => OriginalKey is null;

    /// <summary>The key the entry is saved under: kept once saved, otherwise made from the name and the source.</summary>
    public string Key => OriginalKey ?? EntryKeys.For(Name, Source);

    public bool CanSave => Name.Trim().Length > 0 && !HasErrors;

    /// <summary>Called after the entry was stored without closing the editor.</summary>
    public abstract void MarkSaved();

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    /// <summary>Remembers the key the entry was stored under, so saving again replaces it instead of adding another.</summary>
    public void AdoptKey(string key)
    {
        OriginalKey = key;
    }

    protected abstract IReadOnlyList<CardViewModel> BuildCards();

    /// <summary>
    /// Ends the setup in a subclass's constructor. Until then the card is not drawn, so filling
    /// in the fields does not lay it out once for every field.
    /// </summary>
    protected void Ready()
    {
        _ready = true;
        MarkSaved();
        RefreshPreview();
    }

    protected IReadOnlyList<string> SelectedClasses()
    {
        return
        [
            .. ClassChoices
                .Where(choice => choice.IsSelected)
                .Select(choice => choice.Name)
                .Concat(OtherClasses.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase),
        ];
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_ready && e.PropertyName is { } property && !NotPreviewTriggers.Contains(property))
        {
            RefreshPreview();
        }
    }

    private void RefreshPreview()
    {
        if (_ready)
        {
            PreviewCards = BuildCards();
        }
    }
}
