using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.Core.Combat;

namespace DnDInitiative.App.ViewModels;

public sealed partial class CombatantViewModel : ObservableObject
{
    private readonly Action<Combatant, int> _changeInitiative;

    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DamageCommand))]
    [NotifyCanExecuteChangedFor(nameof(HealCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetHpCommand))]
    private int? _hpChangeAmount;

    public CombatantViewModel(Combatant combatant, Action<Combatant, int> changeInitiative)
    {
        Combatant = combatant;
        _changeInitiative = changeInitiative;
    }

    public Combatant Combatant { get; }

    public string Name
    {
        get => Combatant.Name;
        set
        {
            // A combatant must have a name. Clearing the box is taken as a change of mind.
            if (!string.IsNullOrWhiteSpace(value))
            {
                Combatant.Rename(value);
            }

            // Raised even when nothing changed, so the text box goes back to showing the
            // real name: the old one after a refused edit, or the new one without stray spaces.
            OnPropertyChanged();
        }
    }

    public int Initiative
    {
        get => Combatant.Initiative;
        set
        {
            if (value == Combatant.Initiative)
            {
                return;
            }

            // The tracker owns the order, so the change goes through it.
            _changeInitiative(Combatant, value);
            OnPropertyChanged();
        }
    }

    public int? ArmorClass
    {
        get => Combatant.ArmorClass;
        set => SetProperty(
            Combatant.ArmorClass,
            value,
            Combatant,
            static (combatant, armorClass) => combatant.ArmorClass = armorClass);
    }

    public int? MaxHp
    {
        get => Combatant.MaxHp;
        set
        {
            if (value is < 0 || value == Combatant.MaxHp)
            {
                return;
            }

            Combatant.SetMaxHp(value);
            OnPropertyChanged();
            OnHpChanged();
        }
    }

    public int? CurrentHp => Combatant.CurrentHp;

    public bool TracksHp => Combatant.TracksHp;

    public string HpText => Combatant.TracksHp
        ? string.Create(CultureInfo.InvariantCulture, $"{Combatant.CurrentHp} / {Combatant.MaxHp}")
        : "—";

    private bool CanChangeHp => HpChangeAmount is > 0 && Combatant.TracksHp;

    // Unlike damage and healing, zero is a meaningful value to set.
    private bool CanSetHp => HpChangeAmount is >= 0 && Combatant.TracksHp;

    [RelayCommand(CanExecute = nameof(CanChangeHp))]
    private void Damage()
    {
        if (HpChangeAmount is int amount)
        {
            Combatant.TakeDamage(amount);
            HpChangeAmount = null;
            OnHpChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanChangeHp))]
    private void Heal()
    {
        if (HpChangeAmount is int amount)
        {
            Combatant.Heal(amount);
            HpChangeAmount = null;
            OnHpChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSetHp))]
    private void SetHp()
    {
        if (HpChangeAmount is int amount)
        {
            // The combatant clamps the value to its maximum.
            Combatant.SetCurrentHp(amount);
            HpChangeAmount = null;
            OnHpChanged();
        }
    }

    private void OnHpChanged()
    {
        OnPropertyChanged(nameof(CurrentHp));
        OnPropertyChanged(nameof(HpText));
        OnPropertyChanged(nameof(TracksHp));
        DamageCommand.NotifyCanExecuteChanged();
        HealCommand.NotifyCanExecuteChanged();
        SetHpCommand.NotifyCanExecuteChanged();
    }

    public void RefreshInitiative()
    {
        OnPropertyChanged(nameof(Initiative));
    }
}