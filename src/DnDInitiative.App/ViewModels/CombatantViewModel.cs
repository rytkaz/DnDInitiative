using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.Core.Combat;

namespace DnDInitiative.App.ViewModels;

public sealed partial class CombatantViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isCurrent;

    public CombatantViewModel(Combatant combatant)
    {
        Combatant = combatant;
    }

    public Combatant Combatant { get; }

    public string Name => Combatant.Name;

    public int Initiative => Combatant.Initiative;

    public int? CurrentHp => Combatant.CurrentHp;

    public int? MaxHp => Combatant.MaxHp;

    public int? ArmorClass => Combatant.ArmorClass;
}