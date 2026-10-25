using DnDInitiative.App.Services;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Abilities;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.Tests.Fakes;

/// <summary>
/// Gives every spell and ability one stand-in card. The real cards measure WPF text,
/// which needs a UI thread that tests do not have.
/// </summary>
internal sealed class FakeCardFactory : ICardFactory
{
    public IReadOnlyList<CardViewModel> ForSpell(Spell spell)
    {
        return [MissingCardViewModel.ForSpell(spell.Key)];
    }

    public IReadOnlyList<CardViewModel> ForAbility(Ability ability)
    {
        return [MissingCardViewModel.ForAbility(ability.Key)];
    }
}