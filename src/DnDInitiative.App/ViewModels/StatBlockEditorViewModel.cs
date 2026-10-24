using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

public sealed partial class StatBlockEditorViewModel : ObservableValidator
{
    private readonly Guid _id;
    private readonly SavedState<StatBlock> _savedState;

    private readonly TextFieldViewModel _source;
    private readonly TextFieldViewModel _size;
    private readonly TextFieldViewModel _creatureType;
    private readonly TextFieldViewModel _alignment;
    private readonly TextFieldViewModel _hitDice;
    private readonly TextFieldViewModel _speed;
    private readonly TextFieldViewModel _challengeRating;
    private readonly TextFieldViewModel _savingThrows;
    private readonly TextFieldViewModel _skills;
    private readonly TextFieldViewModel _damageVulnerabilities;
    private readonly TextFieldViewModel _damageResistances;
    private readonly TextFieldViewModel _damageImmunities;
    private readonly TextFieldViewModel _conditionImmunities;
    private readonly TextFieldViewModel _senses;
    private readonly TextFieldViewModel _languages;

    private readonly NumberFieldViewModel _armorClass;
    private readonly NumberFieldViewModel _hitPoints;
    private readonly NumberFieldViewModel _initiativeModifier;
    private readonly NumberFieldViewModel _strength;
    private readonly NumberFieldViewModel _dexterity;
    private readonly NumberFieldViewModel _constitution;
    private readonly NumberFieldViewModel _intelligence;
    private readonly NumberFieldViewModel _wisdom;
    private readonly NumberFieldViewModel _charisma;

    private readonly FeatureListEditorViewModel _traits;
    private readonly FeatureListEditorViewModel _actions;
    private readonly FeatureListEditorViewModel _bonusActions;
    private readonly FeatureListEditorViewModel _reactions;
    private readonly FeatureListEditorViewModel _legendaryActions;
    private readonly FeatureListEditorViewModel _mythicActions;
    private readonly FeatureListEditorViewModel _lairActions;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "A stat block needs a name.")]
    private string _name = "";

    public bool HasUnsavedChanges => _savedState.HasChanges;

    public StatBlockEditorViewModel(StatBlock statBlock)
    {
        ArgumentNullException.ThrowIfNull(statBlock);

        _id = statBlock.Id;
        Name = statBlock.Name;

        _source = new TextFieldViewModel("Source", statBlock.Source);
        _size = new TextFieldViewModel("Size", statBlock.Size);
        _creatureType = new TextFieldViewModel("Creature type", statBlock.CreatureType);
        _alignment = new TextFieldViewModel("Alignment", statBlock.Alignment);
        _hitDice = new TextFieldViewModel("Hit dice", statBlock.HitDice);
        _speed = new TextFieldViewModel("Speed", statBlock.Speed);
        _challengeRating = new TextFieldViewModel("Challenge rating", statBlock.ChallengeRating);
        _savingThrows = new TextFieldViewModel("Saving throws", statBlock.SavingThrows);
        _skills = new TextFieldViewModel("Skills", statBlock.Skills);
        _damageVulnerabilities = new TextFieldViewModel("Damage vulnerabilities", statBlock.DamageVulnerabilities);
        _damageResistances = new TextFieldViewModel("Damage resistances", statBlock.DamageResistances);
        _damageImmunities = new TextFieldViewModel("Damage immunities", statBlock.DamageImmunities);
        _conditionImmunities = new TextFieldViewModel("Condition immunities", statBlock.ConditionImmunities);
        _senses = new TextFieldViewModel("Senses", statBlock.Senses);
        _languages = new TextFieldViewModel("Languages", statBlock.Languages);

        _armorClass = new NumberFieldViewModel("Armor Class", statBlock.ArmorClass, 0, 99);
        _hitPoints = new NumberFieldViewModel("Hit Points", statBlock.HitPoints, 1, 9999);
        _initiativeModifier = new NumberFieldViewModel("Initiative", statBlock.InitiativeModifier, -20, 30);

        AbilityScores abilities = statBlock.Abilities;
        _strength = new NumberFieldViewModel("STR", abilities.Strength, 1, 30);
        _dexterity = new NumberFieldViewModel("DEX", abilities.Dexterity, 1, 30);
        _constitution = new NumberFieldViewModel("CON", abilities.Constitution, 1, 30);
        _intelligence = new NumberFieldViewModel("INT", abilities.Intelligence, 1, 30);
        _wisdom = new NumberFieldViewModel("WIS", abilities.Wisdom, 1, 30);
        _charisma = new NumberFieldViewModel("CHA", abilities.Charisma, 1, 30);

        _traits = new FeatureListEditorViewModel("Traits", statBlock.Traits);
        _actions = new FeatureListEditorViewModel("Actions", statBlock.Actions);
        _bonusActions = new FeatureListEditorViewModel("Bonus Actions", statBlock.BonusActions);
        _reactions = new FeatureListEditorViewModel("Reactions", statBlock.Reactions);
        _legendaryActions = new FeatureListEditorViewModel("Legendary Actions", statBlock.LegendaryActions, statBlock.LegendaryActionsDescription);
        _mythicActions = new FeatureListEditorViewModel("Mythic Actions", statBlock.MythicActions, statBlock.MythicActionsDescription);
        _lairActions = new FeatureListEditorViewModel("Lair Actions", statBlock.LairActions, statBlock.LairActionsDescription);

        NumberFields =
        [
            _armorClass, _hitPoints, _initiativeModifier,
            _strength, _dexterity, _constitution, _intelligence, _wisdom, _charisma,
        ];
        TextFields =
        [
            _size, _creatureType, _alignment, _hitDice, _speed, _challengeRating,
            _savingThrows, _skills, _damageVulnerabilities, _damageResistances,
            _damageImmunities, _conditionImmunities, _senses, _languages, _source,
        ];
        FeatureLists = [_traits, _actions, _bonusActions, _reactions, _legendaryActions, _mythicActions, _lairActions];

        _savedState = new SavedState<StatBlock>(ToStatBlock);
        _savedState.MarkSaved();
    }

    public IReadOnlyList<NumberFieldViewModel> NumberFields { get; }

    public IReadOnlyList<TextFieldViewModel> TextFields { get; }

    public IReadOnlyList<FeatureListEditorViewModel> FeatureLists { get; }

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    public StatBlock ToStatBlock()
    {
        return new StatBlock
        {
            Id = _id,
            Name = Name.Trim(),
            Source = _source.Value.Trim(),
            Size = _size.Value.Trim(),
            CreatureType = _creatureType.Value.Trim(),
            Alignment = _alignment.Value.Trim(),
            ArmorClass = _armorClass.ClampedValue,
            HitPoints = _hitPoints.ClampedValue,
            HitDice = _hitDice.Value.Trim(),
            InitiativeModifier = _initiativeModifier.ClampedValue,
            Speed = _speed.Value.Trim(),
            Abilities = new AbilityScores(
                _strength.ClampedValue,
                _dexterity.ClampedValue,
                _constitution.ClampedValue,
                _intelligence.ClampedValue,
                _wisdom.ClampedValue,
                _charisma.ClampedValue),
            SavingThrows = _savingThrows.Value.Trim(),
            Skills = _skills.Value.Trim(),
            DamageVulnerabilities = _damageVulnerabilities.Value.Trim(),
            DamageResistances = _damageResistances.Value.Trim(),
            DamageImmunities = _damageImmunities.Value.Trim(),
            ConditionImmunities = _conditionImmunities.Value.Trim(),
            Senses = _senses.Value.Trim(),
            Languages = _languages.Value.Trim(),
            ChallengeRating = _challengeRating.Value.Trim(),
            Traits = _traits.ToFeatures(),
            Actions = _actions.ToFeatures(),
            BonusActions = _bonusActions.ToFeatures(),
            Reactions = _reactions.ToFeatures(),
            LegendaryActionsDescription = _legendaryActions.Description.Trim(),
            LegendaryActions = _legendaryActions.ToFeatures(),
            MythicActionsDescription = _mythicActions.Description.Trim(),
            MythicActions = _mythicActions.ToFeatures(),
            LairActionsDescription = _lairActions.Description.Trim(),
            LairActions = _lairActions.ToFeatures(),
        };
    }
}