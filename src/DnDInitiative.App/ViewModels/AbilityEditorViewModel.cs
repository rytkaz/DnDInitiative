using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.App.Services;
using DnDInitiative.Core.Abilities;

namespace DnDInitiative.App.ViewModels;

/// <summary>Edits an ability the user is writing, or an imported one they are changing.</summary>
public sealed partial class AbilityEditorViewModel : LibraryEditorViewModel
{
    private readonly ICardFactory _cardFactory;
    private readonly SavedState<Ability> _savedState;

    [ObservableProperty]
    private string _category = Ability.MartialExploit;

    [ObservableProperty]
    private int _degree = 1;

    [ObservableProperty]
    private string _prerequisites = "";

    public AbilityEditorViewModel(
        ICardFactory cardFactory,
        Ability ability,
        IEnumerable<string> knownClasses,
        string? originalKey)
        : base(knownClasses, ability?.Classes ?? [], originalKey)
    {
        ArgumentNullException.ThrowIfNull(cardFactory);
        ArgumentNullException.ThrowIfNull(ability);

        _cardFactory = cardFactory;

        Name = ability.Name;
        Source = ability.Source;
        Description = ability.Description;
        Category = ability.Category;
        Degree = Math.Clamp(ability.Degree, 1, 5);
        Prerequisites = ability.Prerequisites;

        _savedState = new SavedState<Ability>(ToAbility);
        Ready();
    }

    /// <summary>The choices in the degree box, 1 to 5.</summary>
    public IReadOnlyList<FilterOption<int>> DegreeOptions { get; } =
    [
        .. Enumerable.Range(1, 5).Select(degree => new FilterOption<int>(degree, string.Create(CultureInfo.InvariantCulture, $"Degree {degree}"))),
    ];

    public override string Heading => IsNew ? "New ability" : "Edit ability";

    public override string Noun => "ability";

    public override bool HasUnsavedChanges => _savedState.HasChanges;

    /// <summary>An ability with the defaults a new one starts from.</summary>
    public static AbilityEditorViewModel CreateNew(ICardFactory cardFactory, IEnumerable<string> knownClasses)
    {
        var ability = new Ability { Key = "", Name = "", Degree = 1, Source = DefaultSource };
        return new AbilityEditorViewModel(cardFactory, ability, knownClasses, originalKey: null);
    }

    public override void MarkSaved()
    {
        _savedState.MarkSaved();
    }

    public Ability ToAbility()
    {
        return new Ability
        {
            Key = Key,
            Name = Name.Trim(),
            Category = Category.Trim().Length > 0 ? Category.Trim() : Ability.MartialExploit,
            Degree = Degree,
            Prerequisites = Prerequisites.Trim(),
            Classes = SelectedClasses(),
            Source = Source.Trim(),
            Description = Description.Trim(),
            IsCustom = true,
        };
    }

    protected override IReadOnlyList<CardViewModel> BuildCards()
    {
        return _cardFactory.ForAbility(ToAbility());
    }
}
