using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Abilities;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.Services;

/// <summary>
/// Lays spells and abilities out as cards. Behind an interface because the layout measures
/// real WPF text, which needs a UI thread that view model tests do not have.
/// </summary>
public interface ICardFactory
{
    /// <summary>The spell's card, followed by the cards its description continues onto.</summary>
    IReadOnlyList<CardViewModel> ForSpell(Spell spell);

    /// <summary>The ability's card, followed by the cards its description continues onto.</summary>
    IReadOnlyList<CardViewModel> ForAbility(Ability ability);
}

public sealed class CardFactory : ICardFactory
{
    public IReadOnlyList<CardViewModel> ForSpell(Spell spell)
    {
        return SpellCardViewModel.CreateCards(spell);
    }

    public IReadOnlyList<CardViewModel> ForAbility(Ability ability)
    {
        return AbilityCardViewModel.CreateCards(ability);
    }
}