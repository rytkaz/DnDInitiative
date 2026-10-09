using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

public sealed partial class EncounterBuilderViewModel : PageViewModel
{
    private const int PageSize = 100;

    private int _statBlockListVersion;
    private bool _hasMoreStatBlocks;

    private readonly IEncounterRepository _encounterRepository;
    private readonly IStatBlockRepository _statBlockRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly IMessenger _messenger;
    private readonly IClipboardService _clipboard;
    private readonly IConfirmationService _confirmation;

    // Only the stat blocks that encounters on this page actually use.
    private readonly Dictionary<Guid, StatBlock> _statBlocksById = new();
    private bool _encountersLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedEncounter))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddCreatureCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartEncounterCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyLinkCommand))]
    private EncounterEditorViewModel? _selectedEncounter;

    [ObservableProperty]
    private Party? _selectedParty;

    [ObservableProperty]
    private string _statBlockSearchText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCreatureCommand))]
    private StatBlockSummary? _selectedStatBlock;

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
        IPartyRepository partyRepository,
        IMessenger messenger,
        IClipboardService clipboard,
        IConfirmationService confirmation)
    {
        _encounterRepository = encounterRepository;
        _statBlockRepository = statBlockRepository;
        _partyRepository = partyRepository;
        _messenger = messenger;
        _clipboard = clipboard;
        _confirmation = confirmation;
    }

    public override string Title => "Encounter Builder";

    public ObservableCollection<EncounterEditorViewModel> Encounters { get; } = [];

    public ObservableCollection<Party> Parties { get; } = [];

    public ObservableCollection<StatBlockSummary> FilteredStatBlocks { get; } = [];

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
        RefreshStatBlockListAsync().Forget();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Stat blocks and parties are edited on other pages, so they are
        // re-read every time this page is shown.
        await RefreshStatBlockListAsync();

        Guid? selectedPartyId = SelectedParty?.Id;
        IReadOnlyList<Party> parties = await _partyRepository.GetAllAsync();
        Parties.Clear();
        foreach (Party party in parties)
        {
            Parties.Add(party);
        }

        SelectedParty = Parties.FirstOrDefault(party => party.Id == selectedPartyId) ?? Parties.FirstOrDefault();

        if (_encountersLoaded)
        {
            await CacheStatBlocksAsync(_statBlocksById.Keys.ToList());
        }
        else
        {
            // Encounters are read once, so unsaved edits survive leaving the page.
            _encountersLoaded = true;
            IReadOnlyList<Encounter> encounters = await _encounterRepository.GetAllAsync();
            await CacheStatBlocksAsync(encounters
                .SelectMany(encounter => encounter.Creatures)
                .Select(creature => creature.StatBlockId)
                .Distinct()
                .ToList());

            foreach (Encounter encounter in encounters)
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

        if (!_confirmation.Confirm("Delete party", $"Delete {editor.Name}? This cannot be undone."))
        {
            return;
        }

        await _encounterRepository.DeleteAsync(editor.Id);
        Encounters.Remove(editor);
        SelectedEncounter = Encounters.FirstOrDefault();
        StatusMessage = $"Deleted {editor.Name}.";
    }

    [RelayCommand(CanExecute = nameof(CanAddCreature))]
    private async Task AddCreatureAsync()
    {
        if (SelectedEncounter is not { } encounter || SelectedStatBlock is not { } summary)
        {
            return;
        }

        // Adding the same stat block again raises the count on its row,
        // unless that row was given a custom name and so is its own thing.
        EncounterCreatureViewModel? existing = encounter.Creatures.FirstOrDefault(row =>
            row.StatBlockId == summary.Id && string.IsNullOrWhiteSpace(row.CustomName));

        if (existing is not null)
        {
            existing.Quantity++;
            return;
        }

        // The list only holds summaries, so fetch the full stat block the first time it is used.
        if (!_statBlocksById.ContainsKey(summary.Id))
        {
            StatBlock? statBlock = await _statBlockRepository.GetAsync(summary.Id);
            if (statBlock is null)
            {
                return;
            }

            _statBlocksById[statBlock.Id] = statBlock;
        }

        encounter.Creatures.Add(CreateRow(new EncounterCreature(summary.Id)));
        Recalculate();
    }

    [RelayCommand]
    private void RemoveCreature(EncounterCreatureViewModel row)
    {
        if (SelectedEncounter is { } encounter && encounter.Creatures.Remove(row))
        {
            Recalculate();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEncounter))]
    private void StartEncounter()
    {
        if (SelectedEncounter is not { } editor)
        {
            return;
        }

        Encounter encounter = editor.ToEncounter();
        _messenger.Send(new StartCombatMessage(encounter.Name, encounter.ToBlueprints(_statBlocksById), SelectedParty));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEncounter))]
    private async Task CopyLinkAsync()
    {
        if (SelectedEncounter is not { } editor)
        {
            return;
        }

        if (!editor.Validate())
        {
            StatusMessage = "Give the encounter a name before copying a link.";
            return;
        }

        // The link only holds an Id, so the encounter has to be stored for it to work.
        Encounter encounter = editor.ToEncounter();
        await _encounterRepository.SaveAsync(encounter);

        var link = new EncounterLink(encounter.Id, SelectedParty?.Id);
        string markdown = $"[{encounter.Name}]({link})";

        StatusMessage = _clipboard.TrySetText(markdown)
            ? "Saved, and copied a link for your notes."
            : "Saved, but the clipboard was busy. Try again.";
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

    private async Task CacheStatBlocksAsync(List<Guid> ids)
    {
        foreach (StatBlock statBlock in await _statBlockRepository.GetManyAsync(ids))
        {
            _statBlocksById[statBlock.Id] = statBlock;
        }
    }

    [RelayCommand]
    private async Task LoadMoreStatBlocksAsync()
    {
        if (!_hasMoreStatBlocks)
        {
            return;
        }

        int version = _statBlockListVersion;
        IReadOnlyList<StatBlockSummary> page = await _statBlockRepository.SearchAsync(
            StatBlockSearchText,
            PageSize,
            skip: FilteredStatBlocks.Count);

        if (version == _statBlockListVersion)
        {
            AppendStatBlockPage(page);
        }
    }

    private async Task RefreshStatBlockListAsync()
    {
        int version = ++_statBlockListVersion;
        IReadOnlyList<StatBlockSummary> firstPage = await _statBlockRepository.SearchAsync(StatBlockSearchText, PageSize);

        // A newer refresh started while this one was waiting: let it fill the list.
        if (version != _statBlockListVersion)
        {
            return;
        }

        FilteredStatBlocks.Clear();
        AppendStatBlockPage(firstPage);
    }

    private void AppendStatBlockPage(IReadOnlyList<StatBlockSummary> page)
    {
        foreach (StatBlockSummary summary in page)
        {
            FilteredStatBlocks.Add(summary);
        }

        _hasMoreStatBlocks = page.Count == PageSize;
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