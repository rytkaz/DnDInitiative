using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Security.AccessControl;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

public sealed partial class EncounterCreatureViewModel : ObservableObject
{
    // Assigned last in the constructor, so setting the initial values does not report a change.
    private readonly Action? _onQuantityChanged;

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private string _customName = "";

    public EncounterCreatureViewModel(EncounterCreature creature, StatBlock? statBlock, Action onQuantityChanged)
    {
        ArgumentNullException.ThrowIfNull(creature);

        StatBlockId = creature.StatBlockId;
        StatBlockName = statBlock?.Name ?? "(missing stat block)";
        ChallengeRating = statBlock?.ChallengeRating ?? "";
        XpEach = ChallengeRatings.TryGetXp(ChallengeRating, out int xp) ? xp : 0;
        Quantity = creature.Quantity;
        CustomName = creature.CustomName ?? "";

        _onQuantityChanged = onQuantityChanged;
    }

    public Guid StatBlockId { get; }

    public string StatBlockName { get; }

    public string ChallengeRating { get; }

    public int XpEach { get; }

    public EncounterCreature ToCreature()
    {
        return new EncounterCreature(
            StatBlockId,
            Math.Clamp(Quantity, 1, 99),
            string.IsNullOrWhiteSpace(CustomName) ? null : CustomName.Trim());
    }

    partial void OnQuantityChanged(int value)
    {
        _onQuantityChanged?.Invoke();
    }
}

public sealed partial class EncounterEditorViewModel : ObservableValidator
{
    private readonly SavedState<Encounter> _savedState;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "An encounter needs a name.")]
    private string _name = "";

    public bool HasUnsavedChanges => _savedState.HasChanges;

    public EncounterEditorViewModel(Guid id)
    {
        Id = id;
        _savedState = new SavedState<Encounter>(ToEncounter);
    }

    public void MarkSaved()
    {
        _savedState.MarkSaved();
    }

    public Guid Id { get; }

    public ObservableCollection<EncounterCreatureViewModel> Creatures { get; } = [];

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    public Encounter ToEncounter()
    {
        return new Encounter(Id, Name.Trim(), Creatures.Select(creature => creature.ToCreature()).ToList());
    }
}