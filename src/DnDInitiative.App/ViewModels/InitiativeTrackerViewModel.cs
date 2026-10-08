using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;

namespace DnDInitiative.App.ViewModels;

public sealed partial class InitiativeTrackerViewModel : PageViewModel
{
    private const int MaxQuantity = 50;

    private readonly CombatantFactory _combatantFactory;
    private InitiativeTracker _tracker = new();
    private CombatantNamer _namer = new();

    [ObservableProperty]
    private int _round = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
    private CombatantViewModel? _selectedCombatant;

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

    public InitiativeTrackerViewModel(CombatantFactory combatantFactory)
    {
        _combatantFactory = combatantFactory;
    }

    public override string Title => "Initiative Tracker";

    public ObservableCollection<CombatantViewModel> Combatants { get; } = [];

    private bool HasSelectedCombatant => SelectedCombatant is not null;

    private bool CanAddCreature => !string.IsNullOrWhiteSpace(NewName) && NewMaxHp is null or >= 0;

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
        RollMode rollMode = RollWithDisadvantage ? RollMode.Disadvantage : RollMode.Normal;

        foreach (Combatant combatant in _combatantFactory.Create(blueprints, _namer, rollMode))
        {
            _tracker.Add(combatant);
        }

        SyncOrder();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCombatant))]
    private void RemoveSelected()
    {
        if (SelectedCombatant is { } selected)
        {
            _tracker.Remove(selected.Combatant);
            SyncOrder();
        }
    }

    [RelayCommand]
    private void NewCombat()
    {
        _tracker = new InitiativeTracker();
        _namer = new CombatantNamer();
        SyncOrder();
    }

    /// <summary>Rebuilds the on-screen list after combatants were added, removed or moved.</summary>
    private void SyncOrder()
    {
        Dictionary<Combatant, CombatantViewModel> existing = Combatants.ToDictionary(viewModel => viewModel.Combatant);

        Combatants.Clear();
        foreach (Combatant combatant in _tracker.Combatants)
        {
            Combatants.Add(existing.TryGetValue(combatant, out CombatantViewModel? viewModel)
                ? viewModel
                : new CombatantViewModel(combatant));
        }

        SyncTurn();
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
}