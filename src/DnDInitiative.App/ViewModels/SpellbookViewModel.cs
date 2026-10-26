using System.Collections.ObjectModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.App.Formatting;
using DnDInitiative.App.Services;
using DnDInitiative.Core.Abilities;
using DnDInitiative.Core.Library;
using DnDInitiative.Core.Spellbooks;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// Puts spells and abilities together into spellbooks and lays them out as cards on A4 pages
/// to print. Saved spellbooks are on the left, the open one's pages in the middle, and the
/// library to choose from on the right.
/// </summary>
public sealed partial class SpellbookViewModel : PageViewModel
{
    // The library is read in pages of this size until the last one comes back short. The whole
    // list is wanted, because sorting by name or class cannot be done a page at a time.
    private const int LibraryPageSize = 500;

    // Sorts a spell or ability that is not in the library after everything that is.
    private const int MissingLevel = int.MaxValue;

    private readonly ISpellbookRepository _spellbooks;
    private readonly ISpellRepository _spells;
    private readonly IAbilityRepository _abilities;
    private readonly ICustomSpellRepository _customSpells;
    private readonly ICustomAbilityRepository _customAbilities;
    private readonly ICardFactory _cardFactory;
    private readonly ISpellbookPrinter _printer;
    private readonly IConfirmationService _confirmation;
    private readonly IUiStateStore _uiState;
    private readonly TimeProvider _time;

    // Every entry looked up so far, with its cards, so a change to the spellbook does not lay
    // every card out again. Emptied each time the page is shown, in case something was imported.
    private readonly Dictionary<SpellbookEntry, ResolvedEntry> _resolved = new();

    // Increased for every refresh, so a slow result cannot overwrite a newer one.
    private int _libraryVersion;
    private int _pagesVersion;
    private int _previewVersion;

    private bool _spellbooksLoaded;

    // Whether removing the selected custom entry brings back an imported one, so the button can say "Revert".
    private bool _removeIsRevert;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSpellbook))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddToSpellbookCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleInSpellbookCommand))]
    [NotifyCanExecuteChangedFor(nameof(SortPagesCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    [NotifyPropertyChangedFor(nameof(ShowPages))]
    private SpellbookEditorViewModel? _selectedSpellbook;

    /// <summary>The spell or ability being written or changed. It takes the place of the pages while it is open.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEditor))]
    [NotifyPropertyChangedFor(nameof(ShowPages))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditorCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloseEditorCommand))]
    private LibraryEditorViewModel? _editor;

    /// <summary>"Delete" for an entry the user wrote, "Revert" for an imported one they changed.</summary>
    [ObservableProperty]
    private string _removeLabel = "Delete";

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private LibraryKindOption _selectedKind;

    [ObservableProperty]
    private IReadOnlyList<FilterOption<string?>> _classOptions = [AnyClass];

    [ObservableProperty]
    private FilterOption<string?>? _selectedClass = AnyClass;

    [ObservableProperty]
    private FilterOption<int?>? _selectedLevel;

    [ObservableProperty]
    private FilterOption<bool?>? _selectedConcentration;

    [ObservableProperty]
    private bool _ritualsOnly;

    // Set by clicking a column's heading; see SortLibraryBy.
    private LibrarySortOrder _sortOrder;
    private bool _sortDescending;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToSpellbookCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditEntryCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateEntryCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCustomCommand))]
    private LibraryEntryViewModel? _selectedLibraryEntry;

    /// <summary>The cards of the library row clicked last, so it can be read before it is added.</summary>
    [ObservableProperty]
    private IReadOnlyList<CardViewModel> _previewCards = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCardCommand))]
    private SpellbookCardViewModel? _selectedCard;

    [ObservableProperty]
    private ZoomOption _selectedZoom;

    /// <summary>A note above the library list when something about the library needs explaining, or empty.</summary>
    [ObservableProperty]
    private string _libraryHint = "";

    /// <summary>How many cards and pages the open spellbook makes.</summary>
    [ObservableProperty]
    private string _summary = "";

    [ObservableProperty]
    private string _statusMessage = "";

    public SpellbookViewModel(
        ISpellbookRepository spellbooks,
        ISpellRepository spells,
        IAbilityRepository abilities,
        ICustomSpellRepository customSpells,
        ICustomAbilityRepository customAbilities,
        ICardFactory cardFactory,
        ISpellbookPrinter printer,
        IConfirmationService confirmation,
        IUiStateStore uiState,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(uiState);

        _spellbooks = spellbooks;
        _spells = spells;
        _abilities = abilities;
        _customSpells = customSpells;
        _customAbilities = customAbilities;
        _cardFactory = cardFactory;
        _printer = printer;
        _confirmation = confirmation;
        _uiState = uiState;
        _time = timeProvider;

        SpellbookListWidth = new PanelSize(uiState, "Spellbook.SpellbookList", defaultValue: 200, minimum: 140, maximum: 480);
        LibraryWidth = new PanelSize(uiState, "Spellbook.Library", defaultValue: 480, minimum: 340, maximum: 1000);
        PreviewHeight = new PanelSize(uiState, "Spellbook.Preview", defaultValue: 360, minimum: 120, maximum: 900);
        EditorFormWidth = new PanelSize(uiState, "Spellbook.EditorForm", defaultValue: 440, minimum: 300, maximum: 900);

        // To the fields, so restoring the choices does not save them again.
        UiState saved = uiState.Load();
        _sortOrder = Enum.TryParse(saved.SpellbookLibrarySort, out LibrarySortOrder order) && Enum.IsDefined(order)
            ? order
            : LibrarySortOrder.LevelThenName;
        _sortDescending = saved.SpellbookLibrarySortDescending;

        LevelColumn = new(LibrarySortOrder.LevelThenName, "Lvl", "Spell level, or an exploit's degree", SortLibraryBy);
        NameColumn = new(LibrarySortOrder.Name, "Name", "Name", SortLibraryBy);
        CastingTimeColumn = new(LibrarySortOrder.CastingTime, "Time", "Casting time", SortLibraryBy);
        RangeColumn = new(LibrarySortOrder.Range, "Range", "Range", SortLibraryBy);
        RitualColumn = new(LibrarySortOrder.Ritual, "R", "Ritual", SortLibraryBy);
        ConcentrationColumn = new(LibrarySortOrder.Concentration, "C", "Concentration", SortLibraryBy);
        ShowSortArrow();
        _selectedZoom = ZoomOptions.FirstOrDefault(option => option.Scale.Equals(saved.SpellbookZoom))
            ?? ZoomOptions[1];
        _selectedKind = KindOptions[0];
        _selectedLevel = LevelOptions[0];
        _selectedConcentration = ConcentrationOptions[0];
    }

    public override string Title => "Spellbook";

    public ObservableCollection<SpellbookEditorViewModel> Spellbooks { get; } = [];

    public ObservableCollection<LibraryEntryViewModel> Library { get; } = [];

    /// <summary>The open spellbook's cards, a page's worth at a time.</summary>
    public ObservableCollection<SpellbookPageViewModel> Pages { get; } = [];

    public IReadOnlyList<LibraryKindOption> KindOptions { get; } =
    [
        new(LibraryKind.Both, "Spells and abilities"),
        new(LibraryKind.Spells, "Spells"),
        new(LibraryKind.Abilities, "Abilities"),
    ];

    /// <summary>Spell levels and ability degrees share the box: "3" finds 3rd-level spells and 3rd-degree exploits.</summary>
    public IReadOnlyList<FilterOption<int?>> LevelOptions { get; } =
    [
        new(null, "Any level"),
        new(0, "Cantrip"),
        .. Enumerable.Range(1, 9).Select(level => new FilterOption<int?>(level, string.Create(CultureInfo.InvariantCulture, $"Level {level}"))),
    ];

    public IReadOnlyList<FilterOption<bool?>> ConcentrationOptions { get; } =
    [
        new(null, "Any duration"),
        new(true, "Concentration"),
        new(false, "No concentration"),
    ];

    // The headings of the library's columns, which sort it when clicked.
    public LibraryColumnHeader LevelColumn { get; }

    public LibraryColumnHeader NameColumn { get; }

    public LibraryColumnHeader CastingTimeColumn { get; }

    public LibraryColumnHeader RangeColumn { get; }

    public LibraryColumnHeader RitualColumn { get; }

    public LibraryColumnHeader ConcentrationColumn { get; }

    public IReadOnlyList<ZoomOption> ZoomOptions { get; } =
    [
        new(0.5, "50%"),
        new(0.75, "75%"),
        new(1, "100%"),
        new(1.25, "125%"),
    ];

    public bool HasSelectedSpellbook => SelectedSpellbook is not null;

    public bool HasEditor => Editor is not null;

    /// <summary>Whether the middle of the page shows the open spellbook's pages, rather than the editor.</summary>
    public bool ShowPages => HasSelectedSpellbook && !HasEditor;

    public override IReadOnlyList<string> UnsavedChanges
    {
        get
        {
            List<string> unsaved = Spellbooks
                .Where(spellbook => spellbook.HasUnsavedChanges)
                .Select(spellbook => $"Spellbook: {spellbook.Name}")
                .ToList();

            if (Editor is { HasUnsavedChanges: true } editor)
            {
                unsaved.Add(DescribeEditor(editor));
            }

            return unsaved;
        }
    }

    /// <summary>Width of the editor's form, with the card it draws taking the rest.</summary>
    public PanelSize EditorFormWidth { get; }

    /// <summary>Width of the list of saved spellbooks, on the left.</summary>
    public PanelSize SpellbookListWidth { get; }

    /// <summary>Width of the library, on the right.</summary>
    public PanelSize LibraryWidth { get; }

    /// <summary>Height of the card preview under the library list.</summary>
    public PanelSize PreviewHeight { get; }

    private static FilterOption<string?> AnyClass { get; } = new(null, "Any class");

    private bool CanAddToSpellbook => SelectedSpellbook is not null && SelectedLibraryEntry is not null;

    private bool HasCards => SelectedSpellbook is { Entries.Count: > 0 };

    private bool HasSelectedCard => SelectedCard is not null;

    public override async Task ReloadCampaignDataAsync()
    {
        Spellbooks.Clear();
        SelectedSpellbook = null;
        Editor = null;
        StatusMessage = "";
        _spellbooksLoaded = false;
        await LoadAsync();
    }

    public override async Task<IReadOnlyList<string>> SaveAllAsync()
    {
        var failures = new List<string>();

        // A copy of the list, because storing a spellbook puts the list back in order.
        foreach (SpellbookEditorViewModel editor in Spellbooks.Where(spellbook => spellbook.HasUnsavedChanges).ToList())
        {
            if (editor.Validate())
            {
                await StoreAsync(editor);
            }
            else
            {
                failures.Add("Spellbook: (no name) needs a name");
            }
        }

        if (Editor is { HasUnsavedChanges: true } openEditor && await StoreEditorAsync(openEditor) is { } problem)
        {
            failures.Add($"{DescribeEditor(openEditor)}: {problem}");
        }

        return failures;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        // The library may have been imported into since the page was last shown, so it is
        // read again, and every card is looked up again.
        _resolved.Clear();
        await LoadClassOptionsAsync();
        await RefreshLibraryAsync();

        if (!_spellbooksLoaded)
        {
            // Spellbooks are read once, so unsaved edits survive leaving the page.
            _spellbooksLoaded = true;
            foreach (Spellbook spellbook in await _spellbooks.GetAllAsync())
            {
                Spellbooks.Add(SpellbookEditorViewModel.FromSpellbook(spellbook));
            }

            SortSpellbooks();
            SelectedSpellbook = Spellbooks.FirstOrDefault();
        }

        await RebuildPagesAsync();
    }

    partial void OnSelectedSpellbookChanged(SpellbookEditorViewModel? value)
    {
        SelectedCard = null;
        MarkLibraryEntries();
        RebuildPagesAsync().Forget();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshLibraryAsync().Forget();
    }

    partial void OnSelectedKindChanged(LibraryKindOption value)
    {
        RefreshLibraryAsync().Forget();
    }

    partial void OnSelectedClassChanged(FilterOption<string?>? value)
    {
        RefreshLibraryAsync().Forget();
    }

    partial void OnSelectedLevelChanged(FilterOption<int?>? value)
    {
        RefreshLibraryAsync().Forget();
    }

    partial void OnSelectedConcentrationChanged(FilterOption<bool?>? value)
    {
        RefreshLibraryAsync().Forget();
    }

    partial void OnRitualsOnlyChanged(bool value)
    {
        RefreshLibraryAsync().Forget();
    }

    /// <summary>
    /// Sorts the library by a column, from the lowest; for a column it is already sorted by,
    /// reverses the order instead. Sort pages then uses the same order.
    /// </summary>
    public void SortLibraryBy(LibrarySortOrder order)
    {
        _sortDescending = order == _sortOrder && !_sortDescending;
        _sortOrder = order;
        ShowSortArrow();

        _uiState.Save(_uiState.Load() with
        {
            SpellbookLibrarySort = order.ToString(),
            SpellbookLibrarySortDescending = _sortDescending,
        });

        // Only the order changed, so the rows already loaded are sorted again rather than read again.
        List<LibraryEntryViewModel> sorted = SpellbookSorting.Sort(Library, _sortOrder, _sortDescending).ToList();
        Library.Clear();
        foreach (LibraryEntryViewModel row in sorted)
        {
            Library.Add(row);
        }
    }

    private void ShowSortArrow()
    {
        foreach (LibraryColumnHeader column in new[]
        {
            LevelColumn, NameColumn, CastingTimeColumn, RangeColumn, RitualColumn, ConcentrationColumn,
        })
        {
            column.Arrow = column.Order != _sortOrder ? "" : _sortDescending ? "▼" : "▲";
        }
    }

    partial void OnSelectedZoomChanged(ZoomOption value)
    {
        _uiState.Save(_uiState.Load() with { SpellbookZoom = value.Scale });
    }

    // The list clears its selection when it is refilled. That should not blank the preview.
    partial void OnSelectedLibraryEntryChanged(LibraryEntryViewModel? value)
    {
        if (value is not null)
        {
            ShowPreviewAsync(value.Entry).Forget();
        }

        UpdateRemoveLabelAsync(value).Forget();
    }

    private bool HasSelectedLibraryEntry => SelectedLibraryEntry is not null;

    private bool CanRemoveCustom => SelectedLibraryEntry is { IsCustom: true };

    [RelayCommand]
    private void NewSpell()
    {
        OpenEditor(SpellEditorViewModel.CreateNew(_cardFactory, KnownClasses()));
    }

    [RelayCommand]
    private void NewAbility()
    {
        OpenEditor(AbilityEditorViewModel.CreateNew(_cardFactory, KnownClasses()));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLibraryEntry))]
    private async Task EditEntryAsync()
    {
        if (SelectedLibraryEntry is { } row && await CreateEditorAsync(row.Entry, duplicate: false) is { } editor)
        {
            OpenEditor(editor);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLibraryEntry))]
    private async Task DuplicateEntryAsync()
    {
        if (SelectedLibraryEntry is { } row && await CreateEditorAsync(row.Entry, duplicate: true) is { } editor)
        {
            OpenEditor(editor);
        }
    }

    [RelayCommand(CanExecute = nameof(HasEditor))]
    private async Task SaveEditorAsync()
    {
        if (Editor is not { } editor)
        {
            return;
        }

        StatusMessage = await StoreEditorAsync(editor) ?? $"Saved {editor.Name.Trim()}.";
    }

    [RelayCommand(CanExecute = nameof(HasEditor))]
    private void CloseEditor()
    {
        if (ConfirmLeaveEditor())
        {
            Editor = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemoveCustom))]
    private async Task RemoveCustomAsync()
    {
        if (SelectedLibraryEntry is not { IsCustom: true } row)
        {
            return;
        }

        bool revert = _removeIsRevert;
        bool confirmed = revert
            ? _confirmation.Confirm("Revert to imported", $"Throw away your changes to {row.Name} and use the imported version again?")
            : _confirmation.Confirm("Delete from the library", $"Delete {row.Name}? A spellbook that has it will show a missing card.");
        if (!confirmed)
        {
            return;
        }

        if (row.Entry.Kind == SpellbookEntryKind.Spell)
        {
            await _customSpells.DeleteAsync(row.Entry.Key);
        }
        else
        {
            await _customAbilities.DeleteAsync(row.Entry.Key);
        }

        _resolved.Remove(row.Entry);
        if (Editor is { OriginalKey: { } openKey } && string.Equals(openKey, row.Entry.Key, StringComparison.OrdinalIgnoreCase) && IsKind(Editor, row.Entry.Kind))
        {
            Editor = null;
        }

        await LoadClassOptionsAsync();
        await RefreshLibraryAsync();
        await RebuildPagesAsync();
        StatusMessage = revert ? $"Reverted {row.Name}." : $"Deleted {row.Name}.";
    }

    private List<string> KnownClasses()
    {
        return ClassOptions.Select(option => option.Value).OfType<string>().ToList();
    }

    private static bool IsKind(LibraryEditorViewModel editor, SpellbookEntryKind kind)
    {
        return kind == SpellbookEntryKind.Spell ? editor is SpellEditorViewModel : editor is AbilityEditorViewModel;
    }

    private static string DescribeEditor(LibraryEditorViewModel editor)
    {
        string name = editor.Name.Trim().Length > 0 ? editor.Name.Trim() : "(no name)";
        return editor is SpellEditorViewModel ? $"Spell: {name}" : $"Ability: {name}";
    }

    // The editor holds unsaved text, so replacing it asks first.
    private void OpenEditor(LibraryEditorViewModel editor)
    {
        if (ConfirmLeaveEditor())
        {
            StatusMessage = "";
            Editor = editor;
        }
    }

    private bool ConfirmLeaveEditor()
    {
        return Editor is not { HasUnsavedChanges: true } open
            || _confirmation.Confirm("Discard changes", $"Discard the unsaved changes to {DescribeEditor(open)}?");
    }

    private async Task UpdateRemoveLabelAsync(LibraryEntryViewModel? row)
    {
        bool revert = false;
        if (row is { IsCustom: true })
        {
            revert = row.Entry.Kind == SpellbookEntryKind.Spell
                ? await _customSpells.ExistsInLibraryAsync(row.Entry.Key)
                : await _customAbilities.ExistsInLibraryAsync(row.Entry.Key);
        }

        // Another row was selected while this one was being looked up.
        if (ReferenceEquals(row, SelectedLibraryEntry))
        {
            _removeIsRevert = revert;
            RemoveLabel = revert ? "Revert" : "Delete";
        }
    }

    private async Task<LibraryEditorViewModel?> CreateEditorAsync(SpellbookEntry entry, bool duplicate)
    {
        IEnumerable<string> classes = KnownClasses();

        if (entry.Kind == SpellbookEntryKind.Ability)
        {
            if (await _abilities.FindAsync(entry.Key) is not { } ability)
            {
                StatusMessage = "That ability is no longer in the library.";
                return null;
            }

            if (duplicate)
            {
                return new AbilityEditorViewModel(
                    _cardFactory,
                    ability with { Name = $"{ability.Name} (copy)", Source = LibraryEditorViewModel.DefaultSource },
                    classes,
                    originalKey: null);
            }

            return new AbilityEditorViewModel(_cardFactory, ability, classes, ability.Key)
            {
                Note = NoteForEdit(ability.IsCustom, await _customAbilities.ExistsInLibraryAsync(ability.Key), "ability"),
            };
        }

        if (await _spells.FindAsync(entry.Key) is not { } spell)
        {
            StatusMessage = "That spell is no longer in the library.";
            return null;
        }

        if (duplicate)
        {
            return new SpellEditorViewModel(
                _cardFactory,
                spell with { Name = $"{spell.Name} (copy)", Source = LibraryEditorViewModel.DefaultSource },
                classes,
                originalKey: null);
        }

        return new SpellEditorViewModel(_cardFactory, spell, classes, spell.Key)
        {
            Note = NoteForEdit(spell.IsCustom, await _customSpells.ExistsInLibraryAsync(spell.Key), "spell"),
        };
    }

    private static string NoteForEdit(bool isCustom, bool existsInLibrary, string noun)
    {
        return (isCustom, existsInLibrary) switch
        {
            (false, _) => $"This {noun} was imported. Saving keeps your version in the campaign, and importing again will not replace it. Revert brings the imported one back.",
            (true, true) => $"Your version replaces the imported {noun}. Revert brings the imported one back.",
            _ => "",
        };
    }

    /// <returns>What is wrong, or null if the entry was stored.</returns>
    private async Task<string?> StoreEditorAsync(LibraryEditorViewModel editor)
    {
        if (!editor.Validate() || !editor.CanSave)
        {
            return $"Give the {editor.Noun} a name before saving.";
        }

        string key = editor.Key;
        SpellbookEntryKind kind = editor is SpellEditorViewModel ? SpellbookEntryKind.Spell : SpellbookEntryKind.Ability;

        if (editor.IsNew && await KeyIsTakenAsync(kind, key))
        {
            return $"There is already a {editor.Noun} with this name and source. Change one of them.";
        }

        switch (editor)
        {
            case SpellEditorViewModel spell:
                await _customSpells.SaveAsync(spell.ToSpell());
                break;
            case AbilityEditorViewModel ability:
                await _customAbilities.SaveAsync(ability.ToAbility());
                break;
        }

        editor.AdoptKey(key);
        editor.MarkSaved();
        editor.Note = "";

        // Cards that were laid out from the old text are drawn again.
        _resolved.Remove(new SpellbookEntry(kind, key));
        await LoadClassOptionsAsync();
        await RefreshLibraryAsync();
        await RebuildPagesAsync();
        return null;
    }

    private async Task<bool> KeyIsTakenAsync(SpellbookEntryKind kind, string key)
    {
        return kind == SpellbookEntryKind.Spell
            ? await _customSpells.FindCustomAsync(key) is not null || await _customSpells.ExistsInLibraryAsync(key)
            : await _customAbilities.FindCustomAsync(key) is not null || await _customAbilities.ExistsInLibraryAsync(key);
    }

    partial void OnSelectedCardChanged(SpellbookCardViewModel? oldValue, SpellbookCardViewModel? newValue)
    {
        oldValue?.IsSelected = false;
        newValue?.IsSelected = true;
    }

    [RelayCommand]
    private void NewSpellbook()
    {
        var editor = new SpellbookEditorViewModel(Guid.NewGuid()) { Name = "New spellbook", Modified = _time.GetUtcNow() };
        Spellbooks.Add(editor);
        SortSpellbooks();
        SelectedSpellbook = editor;
        StatusMessage = "";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSpellbook))]
    private async Task CopyAsync()
    {
        if (SelectedSpellbook is not { } editor)
        {
            return;
        }

        // A copy of what is on screen, including edits that have not been saved yet.
        Spellbook source = editor.ToSpellbook();
        string sourceName = source.Name.Length > 0 ? source.Name : "Spellbook";
        Spellbook copy = source with { Id = Guid.NewGuid(), Name = $"{sourceName} (copy)", Modified = _time.GetUtcNow() };

        // Stored straight away, like a copied encounter, so the copy cannot be lost by forgetting to save it.
        await _spellbooks.SaveAsync(copy);

        SpellbookEditorViewModel copyEditor = SpellbookEditorViewModel.FromSpellbook(copy);
        Spellbooks.Add(copyEditor);
        SortSpellbooks();
        SelectedSpellbook = copyEditor;
        StatusMessage = $"Copied {sourceName}.";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSpellbook))]
    private async Task SaveAsync()
    {
        if (SelectedSpellbook is not { } editor)
        {
            return;
        }

        if (!editor.Validate())
        {
            StatusMessage = "Give the spellbook a name before saving.";
            return;
        }

        await StoreAsync(editor);
        StatusMessage = $"Saved {editor.Name}.";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSpellbook))]
    private async Task DeleteAsync()
    {
        if (SelectedSpellbook is not { } editor)
        {
            return;
        }

        if (!_confirmation.Confirm("Delete spellbook", $"Delete {editor.Name}? This cannot be undone."))
        {
            return;
        }

        await _spellbooks.DeleteAsync(editor.Id);
        Spellbooks.Remove(editor);
        SelectedSpellbook = Spellbooks.FirstOrDefault();
        StatusMessage = $"Deleted {editor.Name}.";
    }

    [RelayCommand(CanExecute = nameof(CanAddToSpellbook))]
    private void AddToSpellbook()
    {
        if (SelectedLibraryEntry is { } row)
        {
            AddEntry(row, target: null, after: false);
        }
    }

    /// <summary>
    /// The button at the start of a library row: adds the row to the end of the open spellbook,
    /// or, when it is already there, takes it out again.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanToggleInSpellbook))]
    private void ToggleInSpellbook(LibraryEntryViewModel? row)
    {
        if (row is null || SelectedSpellbook is not { } editor)
        {
            return;
        }

        if (editor.Entries.FirstOrDefault(entry => entry.Entry == row.Entry) is { } existing)
        {
            RemoveEntry(existing);
            StatusMessage = $"Removed {row.Name}.";
        }
        else
        {
            AddEntry(row, target: null, after: false);
        }
    }

    private bool CanToggleInSpellbook(LibraryEntryViewModel? row)
    {
        return row is not null && SelectedSpellbook is not null;
    }

    /// <summary>
    /// Adds a library row to the open spellbook, next to a card already there, or at the end
    /// when there is no target. Called by a row's add button, a double-click, and a drop onto the pages.
    /// </summary>
    public void AddEntry(LibraryEntryViewModel row, SpellbookEntryViewModel? target, bool after)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (SelectedSpellbook is not { } editor)
        {
            return;
        }

        // A second card of the same spell is never wanted, and would make removing one ambiguous.
        if (editor.Contains(row.Entry))
        {
            StatusMessage = $"{row.Name} is already in the spellbook.";
            return;
        }

        int index = target is null ? editor.Entries.Count : editor.Entries.IndexOf(target) + (after ? 1 : 0);
        editor.Entries.Insert(Math.Clamp(index, 0, editor.Entries.Count), new SpellbookEntryViewModel(row.Entry));
        StatusMessage = "";
        EntriesChanged();
    }

    /// <summary>Moves a card's entry to just before or after another one. Called when a card is dragged onto another.</summary>
    public void MoveEntry(SpellbookEntryViewModel entry, SpellbookEntryViewModel? target, bool after)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (SelectedSpellbook is not { } editor || ReferenceEquals(entry, target) || !editor.Entries.Remove(entry))
        {
            return;
        }

        int index = target is null ? editor.Entries.Count : editor.Entries.IndexOf(target) + (after ? 1 : 0);
        editor.Entries.Insert(Math.Clamp(index, 0, editor.Entries.Count), entry);
        EntriesChanged();
    }

    [RelayCommand]
    private void RemoveEntry(SpellbookEntryViewModel? entry)
    {
        if (entry is not null && SelectedSpellbook is { } editor && editor.Entries.Remove(entry))
        {
            EntriesChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCard))]
    private void RemoveSelectedCard()
    {
        RemoveEntry(SelectedCard?.Entry);
    }

    /// <summary>Puts the spellbook's cards in the library's sort order, once. Dragging changes it again afterwards.</summary>
    [RelayCommand(CanExecute = nameof(HasCards))]
    private void SortPages()
    {
        if (SelectedSpellbook is not { } editor)
        {
            return;
        }

        List<SpellbookEntryViewModel> sorted = SpellbookSorting.Sort(editor.Entries, _sortOrder, _sortDescending).ToList();
        editor.Entries.Clear();
        foreach (SpellbookEntryViewModel entry in sorted)
        {
            editor.Entries.Add(entry);
        }

        EntriesChanged();
    }

    [RelayCommand(CanExecute = nameof(HasCards))]
    private void Print()
    {
        if (SelectedSpellbook is not { } editor)
        {
            return;
        }

        string title = editor.Name.Trim().Length > 0 ? editor.Name.Trim() : "Spellbook";
        if (_printer.Print(title, [.. Pages]))
        {
            StatusMessage = $"Sent {title} to the printer.";
        }
    }

    private void EntriesChanged()
    {
        SortPagesCommand.NotifyCanExecuteChanged();
        PrintCommand.NotifyCanExecuteChanged();
        MarkLibraryEntries();
        RebuildPagesAsync().Forget();
    }

    // Every save goes through here, so every save is dated and the list is put back in order.
    private async Task StoreAsync(SpellbookEditorViewModel editor)
    {
        editor.Modified = _time.GetUtcNow();
        await _spellbooks.SaveAsync(editor.ToSpellbook());
        editor.MarkSaved();
        SortSpellbooks();
    }

    private void SortSpellbooks()
    {
        List<SpellbookEditorViewModel> target = Spellbooks
            .OrderBy(spellbook => spellbook.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(spellbook => spellbook.Id)
            .ToList();

        // Moved one at a time, because a list box drops its selection when the selected row is removed.
        for (int index = 0; index < target.Count; index++)
        {
            int current = Spellbooks.IndexOf(target[index]);
            if (current != index)
            {
                Spellbooks.Move(current, index);
            }
        }
    }

    private async Task LoadClassOptionsAsync()
    {
        IReadOnlyList<string> spellClasses = await _spells.GetClassesAsync();
        IReadOnlyList<string> abilityClasses = await _abilities.GetClassesAsync();

        string? selected = SelectedClass?.Value;
        ClassOptions =
        [
            AnyClass,
            .. spellClasses
                .Concat(abilityClasses)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(name => new FilterOption<string?>(name, name)),
        ];
        SelectedClass = ClassOptions.FirstOrDefault(option => string.Equals(option.Value, selected, StringComparison.OrdinalIgnoreCase))
            ?? AnyClass;

        bool hasSpells = (await _spells.SearchAsync(new SpellQuery(), take: 1)).Count > 0;
        bool hasAbilities = (await _abilities.SearchAsync(new AbilityQuery(), take: 1)).Count > 0;
        LibraryHint = (hasSpells, hasAbilities, spellClasses.Count) switch
        {
            (false, false, _) => "The library is empty. Spells come from Import → 5eTools, and martial exploits from Import → Martial Exploits (GM Binder).",
            (true, _, 0) => "Your spells were imported before classes were recorded, so the class filter does not find them yet. Import 5eTools again to add their classes.",
            _ => "",
        };
    }

    private async Task RefreshLibraryAsync()
    {
        int version = ++_libraryVersion;

        string? className = SelectedClass?.Value;
        int? level = SelectedLevel?.Value;
        bool? concentration = SelectedConcentration?.Value;
        LibraryKind kind = SelectedKind.Kind;

        var rows = new List<LibraryEntryViewModel>();

        if (kind != LibraryKind.Abilities)
        {
            var query = new SpellQuery
            {
                Text = SearchText,
                Class = className,
                Level = level,
                IsRitual = RitualsOnly ? true : null,
                IsConcentration = concentration,
            };
            rows.AddRange((await LoadAllAsync((skip, take) => _spells.SearchAsync(query, skip, take))).Select(LibraryEntryViewModel.ForSpell));
        }

        // An ability is never a ritual, never needs concentration, and has no degree 0. Asked
        // for any of those, the library has no abilities to show; "no concentration" does fit them.
        if (kind != LibraryKind.Spells && !RitualsOnly && concentration != true && level != 0)
        {
            var query = new AbilityQuery { Text = SearchText, Class = className, Degree = level };
            rows.AddRange((await LoadAllAsync((skip, take) => _abilities.SearchAsync(query, skip, take))).Select(LibraryEntryViewModel.ForAbility));
        }

        // A newer refresh started while this one was waiting: let it fill the list.
        if (version != _libraryVersion)
        {
            return;
        }

        Library.Clear();
        foreach (LibraryEntryViewModel row in SpellbookSorting.Sort(rows, _sortOrder, _sortDescending))
        {
            Library.Add(row);
        }

        MarkLibraryEntries();
    }

    private static async Task<List<T>> LoadAllAsync<T>(Func<int, int, Task<IReadOnlyList<T>>> search)
    {
        var all = new List<T>();
        IReadOnlyList<T> page;
        do
        {
            page = await search(all.Count, LibraryPageSize);
            all.AddRange(page);
        }
        while (page.Count == LibraryPageSize);

        return all;
    }

    private void MarkLibraryEntries()
    {
        var inSpellbook = SelectedSpellbook?.Entries.Select(row => row.Entry).ToHashSet() ?? [];
        foreach (LibraryEntryViewModel row in Library)
        {
            row.IsInSpellbook = inSpellbook.Contains(row.Entry);
        }
    }

    private async Task ShowPreviewAsync(SpellbookEntry entry)
    {
        int version = ++_previewVersion;
        ResolvedEntry resolved = await ResolveAsync(entry);

        // Another row was clicked while this one was loading: drop the stale result.
        if (version == _previewVersion)
        {
            PreviewCards = resolved.Cards;
        }
    }

    private async Task RebuildPagesAsync()
    {
        int version = ++_pagesVersion;

        if (SelectedSpellbook is not { } editor)
        {
            Pages.Clear();
            Summary = "";
            return;
        }

        foreach (SpellbookEntryViewModel entry in editor.Entries.ToList())
        {
            ResolvedEntry resolved = await ResolveAsync(entry.Entry);
            entry.Apply(resolved, resolved.Cards);
        }

        // The spellbook changed again, or another was chosen, while cards were looked up.
        if (version != _pagesVersion)
        {
            return;
        }

        SpellbookEntryViewModel? selectedEntry = SelectedCard?.Entry;
        List<SpellbookCardViewModel> cards = editor.Entries
            .SelectMany(entry => entry.Cards.Select(card => new SpellbookCardViewModel(entry, card)))
            .ToList();

        Pages.Clear();
        foreach (SpellbookCardViewModel[] chunk in cards.Chunk(SheetLayout.CardsPerPage))
        {
            Pages.Add(new SpellbookPageViewModel(Pages.Count + 1, chunk));
        }

        // The cards are new objects, so the selection moves to the new first card of the same entry.
        SelectedCard = selectedEntry is null ? null : cards.FirstOrDefault(card => ReferenceEquals(card.Entry, selectedEntry));

        Summary = editor.Entries.Count == 0
            ? "Empty. Add spells and abilities from the library on the right, or drag them onto the pages."
            : string.Create(CultureInfo.InvariantCulture, $"{Plural(editor.Entries.Count, "entry", "entries")} on {Plural(cards.Count, "card", "cards")}, {Plural(Pages.Count, "page", "pages")}");
    }

    private static string Plural(int count, string one, string many)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{count} {(count == 1 ? one : many)}");
    }

    private async Task<ResolvedEntry> ResolveAsync(SpellbookEntry entry)
    {
        if (_resolved.TryGetValue(entry, out ResolvedEntry? cached))
        {
            return cached;
        }

        ResolvedEntry resolved;
        if (entry.Kind == SpellbookEntryKind.Ability)
        {
            resolved = await _abilities.FindAsync(entry.Key) is { } ability
                ? new ResolvedEntry(ability.Name, ability.Degree, ability.Classes, _cardFactory.ForAbility(ability))
                : Missing(entry.Key, MissingCardViewModel.ForAbility(entry.Key));
        }
        else
        {
            resolved = await _spells.FindAsync(entry.Key) is { } spell
                ? new ResolvedEntry(spell.Name, spell.LevelNumber, spell.Classes, _cardFactory.ForSpell(spell))
                {
                    CastingTime = spell.CastingTime.Trim(),
                    Range = spell.Range.Trim(),
                    IsRitual = spell.IsRitual,
                    IsConcentration = spell.IsConcentration,
                }
                : Missing(entry.Key, MissingCardViewModel.ForSpell(entry.Key));
        }

        _resolved[entry] = resolved;
        return resolved;
    }

    private static ResolvedEntry Missing(string key, CardViewModel card)
    {
        return new ResolvedEntry(key, MissingLevel, [], [card]);
    }

    /// <summary>What a spellbook entry turned out to be in the library, and its cards.</summary>
    private sealed record ResolvedEntry(string Name, int Level, IReadOnlyList<string> Classes, IReadOnlyList<CardViewModel> Cards)
        : ISortableEntry
    {
        public string CastingTime { get; init; } = "";

        public string Range { get; init; } = "";

        public bool IsRitual { get; init; }

        public bool IsConcentration { get; init; }
    }
}