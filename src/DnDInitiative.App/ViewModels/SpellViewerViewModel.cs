using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// The spell popup: which spell it shows, and whether it is open. There is one for the whole
/// app, because a spell link can be clicked on any page that shows a stat block.
/// </summary>
public sealed partial class SpellViewerViewModel : ObservableObject
{
    private readonly ISpellRepository _spells;

    // Increased for every spell asked for, so a slow lookup cannot replace a newer one.
    private int _requestVersion;

    [ObservableProperty]
    private SpellDetailsViewModel? _details;

    [ObservableProperty]
    private bool _isOpen;

    public SpellViewerViewModel(ISpellRepository spells)
    {
        _spells = spells;
    }

    /// <param name="reference">What the link said: a spell's key, or its name.</param>
    // Concurrent on purpose: a second link can be clicked while the first lookup is still running.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ShowAsync(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return;
        }

        int version = ++_requestVersion;
        Spell? spell = await _spells.FindAsync(reference);

        if (version != _requestVersion)
        {
            return;
        }

        // A link to a spell that was never imported still opens the popup, to say so.
        // Doing nothing would look like a broken link.
        Details = spell is null ? SpellDetailsViewModel.NotFound(reference.Trim()) : new SpellDetailsViewModel(spell);
        IsOpen = true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }
}
