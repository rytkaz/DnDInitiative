using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.Core.Parties;

namespace DnDInitiative.App.ViewModels;

public sealed partial class PlayerCharacterViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "A character needs a name.")]
    private string _name = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 20, ErrorMessage = "Level must be between 1 and 20.")]
    private int _level = 1;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 9999, ErrorMessage = "Max HP must be at least 1.")]
    private int? _maxHp;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 99, ErrorMessage = "AC must be between 0 and 99.")]
    private int? _armorClass;

    public static PlayerCharacterViewModel FromPlayerCharacter(PlayerCharacter character)
    {
        ArgumentNullException.ThrowIfNull(character);

        return new PlayerCharacterViewModel
        {
            Name = character.Name,
            Level = character.Level,
            MaxHp = character.MaxHp,
            ArmorClass = character.ArmorClass,
        };
    }

    public PlayerCharacter ToPlayerCharacter()
    {
        return new PlayerCharacter(Name.Trim(), Level, MaxHp, ArmorClass);
    }

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }
}