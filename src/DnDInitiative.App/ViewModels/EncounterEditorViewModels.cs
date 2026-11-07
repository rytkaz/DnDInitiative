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

    private string _statBlockName = "";
    private string _challengeRating = "";
    private int _xpEach;

    public EncounterCreatureViewModel(EncounterCreature creature, StatBlock? statBlock, Action onQuantityChanged)
    {
        ArgumentNullException.ThrowIfNull(creature);

        StatBlockId = creature.StatBlockId;
        ShowStatBlock(statBlock);
        Quantity = creature.Quantity;
        CustomName = creature.CustomName ?? "";

        _onQuantityChanged = onQuantityChanged;
    }

    public Guid StatBlockId { get; }

    public string StatBlockName
    {
        get => _statBlockName;
        private set => SetProperty(ref _statBlockName, value);
    }

    public string ChallengeRating
    {
        get => _challengeRating;
        private set => SetProperty(ref _challengeRating, value);
    }

    public int XpEach
    {
        get => _xpEach;
        private set => SetProperty(ref _xpEach, value);
    }

    /// <summary>
    /// Shows the stat block's current name and rating. Called again when the page is shown,
    /// because the stat block may have been edited on the Stat Blocks page in the meantime.
    /// </summary>
    public void ShowStatBlock(StatBlock? statBlock)
    {
        StatBlockName = statBlock?.Name ?? "(missing stat block)";
        ChallengeRating = statBlock?.ChallengeRating ?? "";
        XpEach = ChallengeRatings.TryGetXp(ChallengeRating, out int xp) ? xp : 0;
    }

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

public enum EncounterSortOrder
{
    Name,
    Modified,
}

/// <summary>One choice in the encounter list's "Sort by" box.</summary>
public sealed record EncounterSortOption(EncounterSortOrder Order, string Label);

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

    /// <summary>When the encounter was last saved, or created if it never was. Not something the user edits.</summary>
    public DateTimeOffset? Modified { get; set; }

    public ObservableCollection<EncounterCreatureViewModel> Creatures { get; } = [];

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    public Encounter ToEncounter()
    {
        return new Encounter(Id, Name.Trim(), Creatures.Select(creature => creature.ToCreature()).ToList(), Modified);
    }
}