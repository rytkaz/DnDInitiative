using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

public sealed partial class EncounterBuilderViewModel : PageViewModel
{
    private readonly IEncounterRepository _encounterRepository;
    private readonly IStatBlockRepository _statBlockRepository;
    private readonly IPartyRepository _partyRepository;

    private List<StatBlock> _statBlocks = [];
    private Dictionary<Guid, StatBlock> _statBlocksById = new();
    private bool _encountersLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedEncounter))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddCreatureCommand))]
    private EncounterEditorViewModel? _selectedEncounter;

    [ObservableProperty]
    private Party? _selectedParty;

    [ObservableProperty]
    private string _statBlockSearchText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCreatureCommand))]
    private StatBlock? _selectedStatBlock;

    [ObservableProperty]
    private XpBudget _budget = new(0, 0, 0);

    [ObservableProperty]
    private int _totalXp;

    [ObservableProperty]
    private string _difficultyText = "";

    [ObservableProperty]
    private string _statusMessage = "";

    public EncounterBuilderViewModel(
        IEncounterRepository encounterRepository,
        IStatBlockRepository statBlockRepository,
        IPartyRepository partyRepository)
    {
        _encounterRepository = encounterRepository;
        _statBlockRepository = statBlockRepository;
        _partyRepository = partyRepository;
    }

    public override string Title => "Encounter Builder";

    public ObservableCollection<EncounterEditorViewModel> Encounters { get; } = [];

    public ObservableCollection<Party> Parties { get; } = [];

    public ObservableCollection<StatBlock> FilteredStatBlocks { get; } = [];

    public bool HasSelectedEncounter => SelectedEncounter is not null;

    private bool CanAddCreature => SelectedEncounter is not null && SelectedStatBlock is not null;

    partial void OnSelectedEncounterChanged(EncounterEditorViewModel? value)
    {
        Recalculate();
    }

    partial void OnSelectedPartyChanged(Party? value)
    {
        Recalculate();
    }

    partial void OnStatBlockSearchTextChanged(string value)
    {
        ApplyStatBlockFilter();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Stat blocks and parties are edited on other pages, so they are
        // re-read every time this page is shown.
        IReadOnlyList<StatBlock> statBlocks = await _statBlockRepository.GetAllAsync();
        _statBlocks = statBlocks.OrderBy(statBlock => statBlock.Name, StringComparer.OrdinalIgnoreCase).ToList();
        _statBlocksById = _statBlocks.ToDictionary(statBlock => statBlock.Id);
        ApplyStatBlockFilter();

        Guid? selectedPartyId = SelectedParty?.Id;
        IReadOnlyList<Party> parties = await _partyRepository.GetAllAsync();
        Parties.Clear();
        foreach (Party party in parties)
        {
            Parties.Add(party);
        }

        SelectedParty = Parties.FirstOrDefault(party => party.Id == selectedPartyId) ?? Parties.FirstOrDefault();

        // Encounters are read once, so unsaved edits survive leaving the page.
        if (!_encountersLoaded)
        {
            _encountersLoaded = true;
            foreach (Encounter encounter in await _encounterRepository.GetAllAsync())
            {
                Encounters.Add(CreateEditor(encounter));
            }

            SelectedEncounter = Encounters.FirstOrDefault();
        }

        Recalculate();
    }

    [RelayCommand]
    private void NewEncounter()
    {
        var editor = new EncounterEditorViewModel(Guid.NewGuid()) { Name = "New encounter" };
        Encounters.Add(editor);
        SelectedEncounter = editor;
        StatusMessage = "";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEncounter))]
    private async Task SaveAsync()
    {
        if (SelectedEncounter is not { } editor)
        {
            return;
        }

        if (!editor.Validate())
        {
            StatusMessage = "Give the encounter a name before saving.";
            return;
        }

        await _encounterRepository.SaveAsync(editor.ToEncounter());
        StatusMessage = $"Saved {editor.Name}.";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEncounter))]
    private async Task DeleteAsync()
    {
        if (SelectedEncounter is not { } editor)
        {
            return;
        }

        await _encounterRepository.DeleteAsync(editor.Id);
        Encounters.Remove(editor);
        SelectedEncounter = Encounters.FirstOrDefault();
        StatusMessage = $"Deleted {editor.Name}.";
    }

    [RelayCommand(CanExecute = nameof(CanAddCreature))]
    private void AddCreature()
    {
        if (SelectedEncounter is not { } encounter || SelectedStatBlock is not { } statBlock)
        {
            return;
        }

        // Adding the same stat block again raises the count on its row,
        // unless that row was given a custom name and so is its own thing.
        EncounterCreatureViewModel? existing = encounter.Creatures.FirstOrDefault(row =>
            row.StatBlockId == statBlock.Id && string.IsNullOrWhiteSpace(row.CustomName));

        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            encounter.Creatures.Add(CreateRow(new EncounterCreature(statBlock.Id)));
            Recalculate();
        }
    }

    [RelayCommand]
    private void RemoveCreature(EncounterCreatureViewModel row)
    {
        if (SelectedEncounter is { } encounter && encounter.Creatures.Remove(row))
        {
            Recalculate();
        }
    }

    private EncounterEditorViewModel CreateEditor(Encounter encounter)
    {
        var editor = new EncounterEditorViewModel(encounter.Id) { Name = encounter.Name };
        foreach (EncounterCreature creature in encounter.Creatures)
        {
            editor.Creatures.Add(CreateRow(creature));
        }

        return editor;
    }

    private EncounterCreatureViewModel CreateRow(EncounterCreature creature)
    {
        _statBlocksById.TryGetValue(creature.StatBlockId, out StatBlock? statBlock);
        return new EncounterCreatureViewModel(creature, statBlock, Recalculate);
    }

    private void ApplyStatBlockFilter()
    {
        string search = StatBlockSearchText.Trim();

        FilteredStatBlocks.Clear();
        foreach (StatBlock statBlock in _statBlocks)
        {
            if (search.Length == 0 || statBlock.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                FilteredStatBlocks.Add(statBlock);
            }
        }
    }

    private void Recalculate()
    {
        Budget = SelectedParty is { } party
            ? XpBudget.ForParty(party.Members.Select(member => member.Level))
            : new XpBudget(0, 0, 0);
        TotalXp = SelectedEncounter?.ToEncounter().TotalXp(_statBlocksById) ?? 0;
        DifficultyText = DescribeDifficulty();
    }

    private string DescribeDifficulty()
    {
        if (SelectedEncounter is null)
        {
            return "";
        }

        if (SelectedParty is null || SelectedParty.Members.Count == 0)
        {
            return "Select a party with members to see the difficulty.";
        }

        return Budget.Classify(TotalXp) switch
        {
            EncounterDifficulty.Low => "Low",
            EncounterDifficulty.Moderate => "Moderate",
            EncounterDifficulty.High => "High",
            EncounterDifficulty.AboveHigh => "Above High",
            _ => "Add creatures to see the difficulty.",
        };
    }
}