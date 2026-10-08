using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;
using DnDInitiative.Core.Parties;

namespace DnDInitiative.App.ViewModels;

public sealed partial class InitiativeTrackerViewModel : PageViewModel, IRecipient<StartCombatMessage>
{
    private const int MaxQuantity = 50;

    private readonly CombatantFactory _combatantFactory;
    private readonly IPartyRepository _partyRepository;
    private InitiativeTracker _tracker = new();
    private CombatantNamer _namer = new();

    [ObservableProperty]
    private string _combatName = "";

    [ObservableProperty]
    private int _round = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
    private CombatantViewModel? _selectedCombatant;

    [ObservableProperty]
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

    public InitiativeTrackerViewModel(
        CombatantFactory combatantFactory,
        IPartyRepository partyRepository,
        IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(messenger);

        _combatantFactory = combatantFactory;
        _partyRepository = partyRepository;
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
        StartCombat(name: "", creatures: []);
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
}