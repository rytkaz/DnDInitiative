using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

public sealed partial class InitiativeTrackerViewModel : PageViewModel, IRecipient<StartCombatMessage>
{
    private const int MaxQuantity = 50;
    private const int LibraryPageSize = 50;

    private readonly CombatantFactory _combatantFactory;
    private readonly IPartyRepository _partyRepository;
    private readonly IStatBlockRepository _statBlockRepository;
    private readonly IConfirmationService _confirmation;
    private InitiativeTracker _tracker = new();
    private CombatantNamer _namer = new();

    private int _libraryVersion;
    private bool _hasMoreLibraryResults;

    // Increased for every stat block shown, so a slow load cannot overwrite a newer one.
    private int _statBlockDetailsVersion;

    public ObservableCollection<StatBlockSummary> LibraryResults { get; } = [];

    private bool HasSelectedLibraryStatBlock => SelectedLibraryStatBlock is not null;

    [ObservableProperty]
    private bool _isLibraryExpanded = true;

    [ObservableProperty]
    private string _librarySearchText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddFromLibraryCommand))]
    private StatBlockSummary? _selectedLibraryStatBlock;

    [ObservableProperty]
    private string _combatName = "";

    [ObservableProperty]
    private int _round = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
    private CombatantViewModel? _selectedCombatant;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearPartyCommand))]
    private Party? _selectedParty;

    [ObservableProperty]
    private bool _rollWithDisadvantage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCreatureCommand))]
    private string _newName = "";

    [ObservableProperty]
    private int _newQuantity = 1;

    [ObservableProperty]
    private int _newInitiativeModifier;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCreatureCommand))]
    private int? _newMaxHp;

    [ObservableProperty]
    private int? _newArmorClass;

    [ObservableProperty]
    private StatBlockDetailsViewModel? _statBlockDetails;

    [ObservableProperty]
    private bool _showStatBlockPanel = true;

    private bool HasSelectedParty => SelectedParty is not null;

    public InitiativeTrackerViewModel(
        CombatantFactory combatantFactory,
        IPartyRepository partyRepository,
        IStatBlockRepository statBlockRepository,
        IMessenger messenger,
        IConfirmationService confirmation)
    {
        ArgumentNullException.ThrowIfNull(messenger);

        _combatantFactory = combatantFactory;
        _partyRepository = partyRepository;
        _statBlockRepository = statBlockRepository;
        _confirmation = confirmation;
        messenger.Register<StartCombatMessage>(this);
    }

    public override string Title => "Initiative Tracker";

    public ObservableCollection<CombatantViewModel> Combatants { get; } = [];

    public ObservableCollection<Party> Parties { get; } = [];

    private bool HasSelectedCombatant => SelectedCombatant is not null;

    private bool CanAddCreature => !string.IsNullOrWhiteSpace(NewName) && NewMaxHp is null or >= 0;

    [RelayCommand]
    private async Task LoadPartiesAsync()
    {
        // Reloaded every time the page is shown, so parties edited on the
        // Party Setup page are up to date. The selection is kept by Id.
        Guid? selectedId = SelectedParty?.Id;
        IReadOnlyList<Party> parties = await _partyRepository.GetAllAsync();

        Parties.Clear();
        foreach (Party party in parties)
        {
            Parties.Add(party);
        }

        SelectedParty = Parties.FirstOrDefault(party => party.Id == selectedId);
    }

    [RelayCommand]
    private void Next()
    {
        _tracker.Next();
        SyncTurn();
    }

    [RelayCommand]
    private void Previous()
    {
        _tracker.Previous();
        SyncTurn();
    }

    [RelayCommand(CanExecute = nameof(CanAddCreature))]
    private void AddCreature()
    {
        var blueprint = new CombatantBlueprint(NewName.Trim(), NewInitiativeModifier, NewMaxHp, NewArmorClass);
        List<CombatantBlueprint> blueprints = Enumerable
            .Repeat(blueprint, Math.Clamp(NewQuantity, 1, MaxQuantity))
            .ToList();

        AddCombatants(blueprints, RollWithDisadvantage ? RollMode.Disadvantage : RollMode.Normal);
        SyncOrder();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCombatant))]
    private void RemoveSelected()
    {
        if (SelectedCombatant is { } selected)
        {
            _tracker.Remove(selected.Combatant);
            SelectedCombatant = null;
            SyncOrder();
        }
    }

    private void AddCombatants(IReadOnlyList<CombatantBlueprint> blueprints, RollMode rollMode)
    {
        foreach (Combatant combatant in _combatantFactory.Create(blueprints, _namer, rollMode))
        {
            _tracker.Add(combatant);
        }
    }

    private void ChangeInitiative(Combatant combatant, int initiative)
    {
        _tracker.SetInitiative(combatant, initiative);
        SyncOrder();
    }

    /// <summary>
    /// Makes the on-screen rows match the tracker's order, changing as little as possible.
    /// </summary>
    private void SyncOrder()
    {
        IReadOnlyList<Combatant> order = _tracker.Combatants;

        for (int i = Combatants.Count - 1; i >= 0; i--)
        {
            if (!order.Contains(Combatants[i].Combatant))
            {
                Combatants.RemoveAt(i);
            }
        }

        for (int i = 0; i < order.Count; i++)
        {
            int rowIndex = IndexOfRow(order[i]);
            if (rowIndex < 0)
            {
                Combatants.Insert(i, new CombatantViewModel(order[i], ChangeInitiative));
            }
            else if (rowIndex != i)
            {
                Combatants.Move(rowIndex, i);
            }
        }

        SyncTurn();
    }

    private int IndexOfRow(Combatant combatant)
    {
        for (int i = 0; i < Combatants.Count; i++)
        {
            if (ReferenceEquals(Combatants[i].Combatant, combatant))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Updates the turn marker and round after anything that can change them.</summary>
    private void SyncTurn()
    {
        foreach (CombatantViewModel viewModel in Combatants)
        {
            viewModel.IsCurrent = ReferenceEquals(viewModel.Combatant, _tracker.CurrentCombatant);
        }

        Round = _tracker.Round;
    }

    /// <summary>Moves a row to the position currently held by another row.</summary>
    public void MoveCombatant(CombatantViewModel row, CombatantViewModel target)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(target);

        int fromIndex = Combatants.IndexOf(row);
        int toIndex = Combatants.IndexOf(target);
        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
        {
            return;
        }

        _tracker.Move(fromIndex, toIndex);
        SyncOrder();
    }

    public void Receive(StartCombatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Asked before anything changes, so declining leaves the party selection alone too.
        if (!ConfirmReplacingCombat())
        {
            return;
        }

        // No party in the message means "keep whichever party is selected".
        if (message.Party is not null)
        {
            SelectedParty = message.Party;
        }

        StartCombat(message.Name, message.Creatures);
    }

    [RelayCommand]
    private void NewCombat()
    {
        if (ConfirmReplacingCombat())
        {
            StartCombat(name: "", creatures: []);
        }
    }

    /// <summary>An empty tracker has nothing to lose, so it is replaced without asking.</summary>
    private bool ConfirmReplacingCombat()
    {
        return _tracker.Combatants.Count == 0
            || _confirmation.Confirm(
                "Start a new combat",
                "This ends the combat in progress and clears its combatants. Continue?");
    }

    private void StartCombat(string name, IReadOnlyList<CombatantBlueprint> creatures)
    {
        _tracker = new InitiativeTracker();
        _namer = new CombatantNamer();
        CombatName = name;

        if (SelectedParty is { } party)
        {
            AddCombatants(party.ToBlueprints(), RollMode.Normal);
        }

        AddCombatants(creatures, RollWithDisadvantage ? RollMode.Disadvantage : RollMode.Normal);
        SyncOrder();
    }

    partial void OnSelectedCombatantChanged(CombatantViewModel? value)
    {
        ShowStatBlockAsync(value?.Combatant.StatBlockId).Forget();
    }

    partial void OnSelectedLibraryStatBlockChanged(StatBlockSummary? value)
    {
        // The list clears its selection whenever it is refilled. That should not blank the panel.
        if (value is not null)
        {
            ShowStatBlockAsync(value.Id).Forget();
        }
    }

    private async Task ShowStatBlockAsync(Guid? statBlockId)
    {
        int version = ++_statBlockDetailsVersion;

        if (statBlockId is not { } id)
        {
            StatBlockDetails = null;
            return;
        }

        StatBlock? statBlock = await _statBlockRepository.GetAsync(id);

        // Something else was selected while this one was loading: drop the stale result.
        if (version != _statBlockDetailsVersion)
        {
            return;
        }

        StatBlockDetails = statBlock is null ? null : new StatBlockDetailsViewModel(statBlock);
    }

    partial void OnLibrarySearchTextChanged(string value)
    {
        RefreshLibraryAsync().Forget();
    }

    [RelayCommand]
    private Task LoadLibraryAsync()
    {
        return RefreshLibraryAsync();
    }

    [RelayCommand]
    private async Task LoadMoreLibraryResultsAsync()
    {
        if (!_hasMoreLibraryResults)
        {
            return;
        }

        int version = _libraryVersion;
        IReadOnlyList<StatBlockSummary> page = await _statBlockRepository.SearchAsync(
            LibrarySearchText,
            LibraryPageSize,
            skip: LibraryResults.Count);

        if (version == _libraryVersion)
        {
            AppendLibraryPage(page);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLibraryStatBlock))]
    private async Task AddFromLibraryAsync()
    {
        if (SelectedLibraryStatBlock is not { } summary)
        {
            return;
        }

        StatBlock? statBlock = await _statBlockRepository.GetAsync(summary.Id);
        if (statBlock is null)
        {
            return;
        }

        List<CombatantBlueprint> blueprints = Enumerable
            .Repeat(statBlock.ToBlueprint(), Math.Clamp(NewQuantity, 1, MaxQuantity))
            .ToList();

        AddCombatants(blueprints, RollWithDisadvantage ? RollMode.Disadvantage : RollMode.Normal);
        SyncOrder();
    }

    private async Task RefreshLibraryAsync()
    {
        int version = ++_libraryVersion;
        IReadOnlyList<StatBlockSummary> firstPage = await _statBlockRepository.SearchAsync(LibrarySearchText, LibraryPageSize);

        // A newer refresh started while this one was waiting: let it fill the list.
        if (version != _libraryVersion)
        {
            return;
        }

        LibraryResults.Clear();
        AppendLibraryPage(firstPage);
    }

    private void AppendLibraryPage(IReadOnlyList<StatBlockSummary> page)
    {
        foreach (StatBlockSummary summary in page)
        {
            LibraryResults.Add(summary);
        }

        _hasMoreLibraryResults = page.Count == LibraryPageSize;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedParty))]
    private void ClearParty()
    {
        SelectedParty = null;
    }
}